using UnityEngine;
using System;
using Core;
using Data;

/// <summary>
/// Синглтон. Хранит глобальное состояние матча:
/// двух игроков, активного игрока, счётчики ходов.
/// Инициализирует игру и проверяет условие победы.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────── Синглтон ───────────
    public static GameManager Instance { get; private set; }

    // ─────────── Настройки ───────────
    [Header("Настройки матча")]
    [SerializeField] private int startingStability = 30;

    // ─────────── Ссылки на менеджеры ───────────
    [Header("Менеджеры (назначить в инспекторе или авто-поиск)")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private ResourceManager resourceManager;

    // ─────────── Состояние игроков ───────────
    public PlayerState Player1 { get; private set; }
    public PlayerState Player2 { get; private set; }

    // ─────────── Активный игрок ───────────
    private int activePlayerIndex; // 0 = Player1, 1 = Player2
    private int[] playerTurnNumbers = new int[2];

    public PlayerState ActivePlayer => activePlayerIndex == 0 ? Player1 : Player2;
    public PlayerState OpponentPlayer => activePlayerIndex == 0 ? Player2 : Player1;
    public int ActivePlayerIndex => activePlayerIndex;
    public int CurrentPlayerTurn => playerTurnNumbers[activePlayerIndex];

    // ─────────── Публичный доступ к менеджерам ───────────
    public DeckManager Deck => deckManager;
    public TurnManager Turn => turnManager;
    public ResourceManager Resource => resourceManager;

    // ─────────── Флаг конца игры ───────────
    public bool IsGameOver { get; private set; }

    // ─────────── События ───────────
    /// <summary>Игра инициализирована, стартовые руки розданы.</summary>
    public event Action OnGameStarted;

    /// <summary>Матч завершён. Аргумент — победивший игрок.</summary>
    public event Action<PlayerState> OnGameOver;

    /// <summary>Активный игрок сменился. Аргумент — новый индекс (0 или 1).</summary>
    public event Action<int> OnActivePlayerChanged;

    // ═══════════════════════════════════════════
    //               ЖИЗНЕННЫЙ ЦИКЛ
    // ═══════════════════════════════════════════

    void Awake()
    {
        // Синглтон
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Автопоиск менеджеров, если не назначены
        if (deckManager == null) deckManager = FindObjectOfType<DeckManager>();
        if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        if (resourceManager == null) resourceManager = FindObjectOfType<ResourceManager>();
    }

    void Start()
    {
        StartGame();
    }

    // ═══════════════════════════════════════════
    //            ИНИЦИАЛИЗАЦИЯ МАТЧА
    // ═══════════════════════════════════════════

    public void StartGame()
    {
        IsGameOver = false;

        // Создаём игроков
        Player1 = new PlayerState("Государство 1", startingStability);
        Player2 = new PlayerState("Государство 2", startingStability);
        
        deckManager.InitializeDecks(Player1, Player2);

        // Стартовая раздача: 3 ресурса + 1 юнит каждому
        DealStartingHand(Player1);
        DealStartingHand(Player2);

        // Первым ходит Игрок 1
        activePlayerIndex = 0;
        playerTurnNumbers[0] = 0;
        playerTurnNumbers[1] = 0;

        Debug.Log("══════════ МАТЧ НАЧАЛСЯ ══════════");
        OnGameStarted?.Invoke();

        // Запускаем первый ход
        turnManager.StartNewTurn();
    }

    private void DealStartingHand(PlayerState player)
    {
        for (int i = 0; i < 3; i++)
        {
            ResourceCardData card = deckManager.DrawRandomResourceCard(player);
            if (card != null)
            {
                player.hand.Add(card);
                Debug.Log($"  {player.playerName} получил ресурс: {card.cardName}");
            }
        }

        UnitCardData unit = deckManager.DrawRandomUnitCard(1);
        if (unit != null)
        {
            player.hand.Add(unit);
            Debug.Log($"  {player.playerName} получил юнита: {unit.cardName}");
        }
    }

    // ═══════════════════════════════════════════
    //             УПРАВЛЕНИЕ ХОДАМИ
    // ═══════════════════════════════════════════

    /// <summary>
    /// Увеличить счётчик ходов активного игрока на 1.
    /// Вызывается TurnManager в начале каждого хода.
    /// </summary>
    public void IncrementCurrentPlayerTurn()
    {
        playerTurnNumbers[activePlayerIndex]++;
    }

    /// <summary>
    /// Передать ход другому игроку.
    /// </summary>
    public void SwitchActivePlayer()
    {
        activePlayerIndex = 1 - activePlayerIndex;
        Debug.Log($"  ► Ход переходит к: {ActivePlayer.playerName}");
        OnActivePlayerChanged?.Invoke(activePlayerIndex);
    }

    // ═══════════════════════════════════════════
    //            ПРОВЕРКА ПОБЕДЫ
    // ═══════════════════════════════════════════

    /// <summary>
    /// Проверить, не обнулилась ли стабильность одного из игроков.
    /// Возвращает true, если игра окончена.
    /// </summary>
    public bool CheckVictoryCondition()
    {
        if (IsGameOver) return true;

        PlayerState winner = null;

        if (!Player1.IsAlive()) winner = Player2;
        else if (!Player2.IsAlive()) winner = Player1;

        if (winner != null)
        {
            IsGameOver = true;
            OnGameOver?.Invoke(winner);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Перезапуск матча.
    /// </summary>
    public void RestartGame()
    {
        StartGame();
    }
}