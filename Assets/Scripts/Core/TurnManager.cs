using UnityEngine;
using System;
using Cards;
using Core;
using Data;

/// <summary>
/// Конечный автомат хода.
/// Управляет фазами: Добор → Генерация ресурсов → Основная → Боевая → Конец.
/// Валидирует действия игрока (розыгрыш карт) по правилам.
/// </summary>
public class TurnManager : MonoBehaviour
{
    // ─────────── Ссылки ───────────
    [Header("Менеджеры")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private ResourceManager resourceManager;

    // ─────────── Текущее состояние ───────────
    public GamePhase CurrentPhase { get; private set; }
    public TurnAction CurrentTurnAction { get; private set; }

    // ─────────── События ───────────
    /// <summary>Фаза хода изменилась.</summary>
    public event Action<GamePhase> OnPhaseChanged;

    /// <summary>Ход начался. Аргумент — номер хода текущего игрока.</summary>
    public event Action<int> OnTurnStarted;

    /// <summary>Ход завершён (игрок уже переключён). UI должен показать экран передачи.</summary>
    public event Action OnTurnEnded;

    /// <summary>Карта ресурса успешно выложена на поле.</summary>
    public event Action<ResourceCardData> OnResourceCardPlayed;

    /// <summary>Юнит успешно выложен на поле.</summary>
    public event Action<UnitCardData, RuntimeUnitCard> OnUnitCardPlayed;

    // ═══════════════════════════════════════════

    void Awake()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (resourceManager == null) resourceManager = FindObjectOfType<ResourceManager>();
    }

    // ═══════════════════════════════════════════
    //               НАЧАЛО ХОДА
    // ═══════════════════════════════════════════

    /// <summary>
    /// Начать новый ход для текущего активного игрока.
    /// Вызывается: GameManager при старте и UI после экрана передачи хода.
    /// </summary>
    public void StartNewTurn()
    {
        if (gameManager.IsGameOver) return;

        gameManager.IncrementCurrentPlayerTurn();
        int turn = gameManager.CurrentPlayerTurn;
        CurrentTurnAction = TurnAction.None;

        Debug.Log($"\n╔══════════════════════════════════════╗");
        Debug.Log($"║  {gameManager.ActivePlayer.playerName} — ХОД {turn}");
        Debug.Log($"╚══════════════════════════════════════╝");

        // Сбрасываем флаги атаки юнитов
        ResetUnitAttackFlags();

        // 1) Добор карт
        ExecuteDrawPhase(turn);

        // 2) Генерация ресурсов
        ExecuteResourcePhase();

        // 3) Ожидаем действий игрока
        SetPhase(GamePhase.MainPhase);
    }

    // ═══════════════════════════════════════════
    //              ФАЗА ДОБОРА
    // ═══════════════════════════════════════════

    private void ExecuteDrawPhase(int turn)
    {
        SetPhase(GamePhase.DrawPhase);
        PlayerState player = gameManager.ActivePlayer;

        switch (turn)
        {
            case 1:
                Debug.Log("  Добор: стартовая рука уже выдана");
                break;

            case 2:
                DrawToHand(player, deckManager.DrawRandomResourceCard(player), "карта ресурса");
                DrawToHand(player, deckManager.DrawRandomUnitCard(turn), "карта юнита");
                break;

            default:
                DrawToHand(player, deckManager.DrawRandomUnitCard(turn), "карта юнита");

                if (turn % 4 == 0)
                {
                    DrawToHand(player, deckManager.DrawRandomResourceCard(player),
                        "карта ресурса (бонус: ход кратен 4)");
                }
                break;
        }
    }

    private void DrawToHand(PlayerState player, ScriptableObject card, string label)
    {
        if (card == null) return;
        player.hand.Add(card);

        string name = card switch
        {
            ResourceCardData r => r.cardName,
            UnitCardData u => u.cardName,
            _ => "???"
        };
        Debug.Log($"    + {name} ({label})");
    }

    // ═══════════════════════════════════════════
    //          ФАЗА ГЕНЕРАЦИИ РЕСУРСОВ
    // ═══════════════════════════════════════════

    private void ExecuteResourcePhase()
    {
        SetPhase(GamePhase.ResourcePhase);
        resourceManager.GenerateResourcesForPlayer(gameManager.ActivePlayer);
    }

    // ═══════════════════════════════════════════
    //      ОСНОВНАЯ ФАЗА — РОЗЫГРЫШ КАРТ
    // ═══════════════════════════════════════════

    /// <summary>
    /// Попытка разыграть карту ресурса из руки.
    /// </summary>
    public bool TryPlayResourceCard(ResourceCardData card)
    {
        // ── Общие проверки ──
        if (!ValidateMainPhase()) return false;

        PlayerState player = gameManager.ActivePlayer;

        if (!player.hand.Contains(card))
        {
            Debug.LogWarning("  ✘ Карта не найдена в руке");
            return false;
        }

        // ── Проверка правил по ходам ──
        int turn = gameManager.CurrentPlayerTurn;

        // На любом ходе: нельзя положить больше 1 ресурса
        if (CurrentTurnAction == TurnAction.PlayedResource)
        {
            Debug.LogWarning("  ✘ Уже выложена карта ресурса в этом ходу");
            return false;
        }

        // Ход 3+: правило XOR — нельзя ресурс если уже выложены юниты
        if (turn >= 3 && CurrentTurnAction == TurnAction.PlayedUnits)
        {
            Debug.LogWarning("  ✘ Нельзя: уже выложены юниты (правило XOR)");
            return false;
        }

        // ── Выкладываем ──
        player.hand.Remove(card);

        // Проверяем, есть ли на поле карта того же типа
        RuntimeResourceCard existing = player.FindResourceOnField(card.resourceType);

        if (existing != null)
        {
            // Объединяем: увеличиваем генерацию существующей карты
            existing.Merge();
            Debug.Log($"  ▶ Объединение ресурса: {card.cardName} → " +
                      $"{existing.data.cardName} (генерация ×{existing.currentGeneration})");
        }
        else
        {
            // Новый тип — создаём рантайм-обёртку
            RuntimeResourceCard runtimeRes = new RuntimeResourceCard(card);
            player.resourceField.Add(runtimeRes);
            Debug.Log($"  ▶ Выложен ресурс: {card.cardName} " +
                      $"({card.resourceType}, генерация ×{runtimeRes.currentGeneration})");
        }

        CurrentTurnAction = TurnAction.PlayedResource;
        OnResourceCardPlayed?.Invoke(card);
        return true;
    }

    /// <summary>
    /// Попытка разыграть карту юнита из руки.
    /// </summary>
    public bool TryPlayUnitCard(UnitCardData card)
    {
        // ── Общие проверки ──
        if (!ValidateMainPhase()) return false;

        PlayerState player = gameManager.ActivePlayer;
        int turn = gameManager.CurrentPlayerTurn;

        // Ходы 1-2: юниты запрещены
        if (turn <= 2)
        {
            Debug.LogWarning("  ✘ Юнитов нельзя выкладывать на ходах 1-2");
            return false;
        }

        // Правило XOR: нельзя юнитов если уже выложен ресурс
        if (CurrentTurnAction == TurnAction.PlayedResource)
        {
            Debug.LogWarning("  ✘ Нельзя: уже выложен ресурс (правило XOR)");
            return false;
        }

        if (!player.hand.Contains(card))
        {
            Debug.LogWarning("  ✘ Карта не найдена в руке");
            return false;
        }

        // Проверка ресурсов
        if (!player.CanAfford(card))
        {
            int[] cost = card.GetCostArray();
            Debug.LogWarning(
                $"  ✘ Недостаточно ресурсов для {card.cardName}.\n" +
                $"    Нужно:  Д={cost[0]} Л={cost[1]} П={cost[2]} Т={cost[3]}\n" +
                $"    Есть:   Д={player.resourcePool[0]} Л={player.resourcePool[1]} " +
                $"П={player.resourcePool[2]} Т={player.resourcePool[3]}");
            return false;
        }

        // ── Выкладываем ──
        player.hand.Remove(card);
        player.SpendResources(card);

        RuntimeUnitCard runtimeUnit = new RuntimeUnitCard(card);
        player.unitField.Add(runtimeUnit);

        CurrentTurnAction = TurnAction.PlayedUnits;

        Debug.Log($"  ▶ Выложен юнит: {card.cardName} " +
                  $"(HP={card.hp}, Атака={card.damageToUnits}, Броня={card.armor})");

        OnUnitCardPlayed?.Invoke(card, runtimeUnit);
        return true;
    }

    // ═══════════════════════════════════════════
    //             ПЕРЕХОДЫ МЕЖДУ ФАЗАМИ
    // ═══════════════════════════════════════════

    /// <summary>
    /// Завершить основную фазу. Вызывается кнопкой UI.
    /// </summary>
    public void EndMainPhase()
    {
        if (CurrentPhase != GamePhase.MainPhase) return;

        if (gameManager.CurrentPlayerTurn >= 3)
        {
            SetPhase(GamePhase.CombatPhase);
            Debug.Log("  → Боевая фаза");
        }
        else
        {
            // Ходы 1-2: боя нет, сразу завершаем ход
            EndTurn();
        }
    }

    /// <summary>
    /// Завершить боевую фазу. Вызывается кнопкой UI или CombatManager.
    /// </summary>
    public void EndCombatPhase()
    {
        if (CurrentPhase != GamePhase.CombatPhase) return;

        // Проверяем победу после боя
        if (gameManager.CheckVictoryCondition())
        {
            SetPhase(GamePhase.GameOver);
            return;
        }

        EndTurn();
    }

    // ═══════════════════════════════════════════
    //                КОНЕЦ ХОДА
    // ═══════════════════════════════════════════

    private void EndTurn()
    {
        SetPhase(GamePhase.EndPhase);

        // Увеличиваем счётчик turnsOnField для юнитов текущего игрока
        PlayerState player = gameManager.ActivePlayer;
        foreach (RuntimeUnitCard unit in player.unitField)
        {
            unit.turnsOnField++;
        }

        Debug.Log($"  ═══ Конец хода: {player.playerName} ═══");

        // Переключаем активного игрока
        gameManager.SwitchActivePlayer();

        // Сообщаем UI — пора показать экран передачи хода.
        // UI вызовет StartNewTurn(), когда следующий игрок будет готов.
        OnTurnEnded?.Invoke();
    }

    // ═══════════════════════════════════════════
    //         ВСПОМОГАТЕЛЬНЫЕ ЗАПРОСЫ ДЛЯ UI
    // ═══════════════════════════════════════════

    /// <summary>Можно ли в принципе сейчас положить карту ресурса.</summary>
    public bool CanPlayAnyResourceCard()
    {
        if (CurrentPhase != GamePhase.MainPhase) return false;
        if (CurrentTurnAction == TurnAction.PlayedResource) return false;
        if (CurrentTurnAction == TurnAction.PlayedUnits) return false;
        return true;
    }

    /// <summary>Можно ли в принципе сейчас выкладывать юнитов.</summary>
    public bool CanPlayAnyUnitCard()
    {
        if (CurrentPhase != GamePhase.MainPhase) return false;
        if (gameManager.CurrentPlayerTurn <= 2) return false;
        if (CurrentTurnAction == TurnAction.PlayedResource) return false;
        return true;
    }

    /// <summary>Есть ли у активного игрока юниты, способные атаковать.</summary>
    public bool HasReadyAttackers()
    {
        foreach (RuntimeUnitCard unit in gameManager.ActivePlayer.unitField)
        {
            if (unit.CanAttack()) return true;
        }
        return false;
    }

    // ═══════════════════════════════════════════
    //               УТИЛИТЫ
    // ═══════════════════════════════════════════

    private void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        Debug.Log($"  [Фаза → {phase}]");
        OnPhaseChanged?.Invoke(phase);
    }

    private void ResetUnitAttackFlags()
    {
        foreach (RuntimeUnitCard unit in gameManager.ActivePlayer.unitField)
        {
            unit.hasAttackedThisTurn = false;
        }
    }

    private bool ValidateMainPhase()
    {
        if (CurrentPhase != GamePhase.MainPhase)
        {
            Debug.LogWarning("  ✘ Сейчас не основная фаза");
            return false;
        }
        return true;
    }
}