using Cards;
using Combat;
using Core;
using Data;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // ─────────── Префаб ───────────
    [Header("Префаб карты")]
    [SerializeField] private GameObject cardPrefab;

    // ─────────── Поле Игрока 1 (низ экрана) ───────────
    [Header("Поле Игрока 1 (низ)")]
    [SerializeField] private Transform p1ResourceContainer;
    [SerializeField] private Transform p1UnitContainer;

    // ─────────── Поле Игрока 2 (верх экрана) ───────────
    [Header("Поле Игрока 2 (верх)")]
    [SerializeField] private Transform p2ResourceContainer;
    [SerializeField] private Transform p2UnitContainer;

    // ─────────── Рука ───────────
    [Header("Рука активного игрока")]
    [SerializeField] private Transform handContainer;

    // ─────────── Инфо Игрок 1 ───────────
    [Header("Инфо Игрок 1")]
    [SerializeField] private TMP_Text p1NameText;
    [SerializeField] private TMP_Text p1StabilityText;
    [SerializeField] private TMP_Text p1ResourcesText;

    // ─────────── Инфо Игрок 2 ───────────
    [Header("Инфо Игрок 2")]
    [SerializeField] private TMP_Text p2NameText;
    [SerializeField] private TMP_Text p2StabilityText;
    [SerializeField] private TMP_Text p2ResourcesText;

    // ─────────── Индикаторы ───────────
    [Header("Индикаторы")]
    [SerializeField] private TMP_Text phaseText;
    [SerializeField] private TMP_Text hintText;

    // ─────────── Кнопки действий ───────────
    [Header("Кнопки")]
    [SerializeField] private Button endMainPhaseButton;
    [SerializeField] private Button endCombatPhaseButton;
    [SerializeField] private Button attackStabilityButton;

    // ─────────── Экран передачи хода ───────────
    [Header("Экран передачи хода")]
    [SerializeField] private GameObject transitionScreen;
    [SerializeField] private TMP_Text transitionText;
    [SerializeField] private Button continueButton;

    // ─────────── Экран победы ───────────
    [Header("Экран победы")]
    [SerializeField] private GameObject victoryScreen;
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private Button restartButton;

    // ─────────── Кэш менеджеров ───────────
    private GameManager gm;
    private TurnManager tm;
    private CombatManager cm;

    // ═══════════════════════════════════════════
    //             ЖИЗНЕННЫЙ ЦИКЛ
    // ═══════════════════════════════════════════

    void Awake()
    {
        // Находим менеджеры (все Awake до всех Start — подписка будет готова)
        gm = FindObjectOfType<GameManager>();
        tm = FindObjectOfType<TurnManager>();
        cm = FindObjectOfType<CombatManager>();

        SubscribeEvents();
        SubscribeButtons();
    }

    void Start()
    {
        transitionScreen.SetActive(false);
        victoryScreen.SetActive(false);
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ═══════════════════════════════════════════
    //               ПОДПИСКИ
    // ═══════════════════════════════════════════

    private void SubscribeEvents()
    {
        // TurnManager
        tm.OnPhaseChanged        += HandlePhaseChanged;
        tm.OnTurnEnded           += HandleTurnEnded;
        tm.OnResourceCardPlayed  += (c)    => RefreshAll();
        tm.OnUnitCardPlayed      += (c, r) => RefreshAll();

        // CombatManager
        cm.OnAttackerSelected       += (u)          => RefreshAll();
        cm.OnAttackerDeselected     += ()            => RefreshAll();
        cm.OnUnitAttackedUnit       += (a, t, d, c) => RefreshAll();
        cm.OnUnitAttackedStability  += (a, d, s)     => RefreshAll();
        cm.OnUnitDestroyed          += (u)           => RefreshAll();

        // GameManager
        gm.OnGameOver += ShowVictoryScreen;
    }

    private void UnsubscribeEvents()
    {
        if (tm != null)
        {
            tm.OnPhaseChanged -= HandlePhaseChanged;
            tm.OnTurnEnded    -= HandleTurnEnded;
        }
        if (gm != null)
        {
            gm.OnGameOver -= ShowVictoryScreen;
        }
    }

    private void SubscribeButtons()
    {
        endMainPhaseButton.onClick.AddListener(OnEndMainPhaseClicked);
        endCombatPhaseButton.onClick.AddListener(OnEndCombatPhaseClicked);
        attackStabilityButton.onClick.AddListener(OnAttackStabilityClicked);
        continueButton.onClick.AddListener(OnContinueClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
    }

    // ═══════════════════════════════════════════
    //            ОБРАБОТЧИКИ СОБЫТИЙ
    // ═══════════════════════════════════════════

    private void HandlePhaseChanged(GamePhase phase)
    {
        RefreshAll();
    }

    private void HandleTurnEnded()
    {
        ShowTransitionScreen();
    }

    // ═══════════════════════════════════════════
    //              REFRESH ALL
    // ═══════════════════════════════════════════

    private void RefreshAll()
    {
        if (gm.IsGameOver) return;

        RefreshPlayerInfo();
        RefreshFields();
        RefreshHand();
        RefreshButtons();
        RefreshPhaseInfo();
    }

    // ═══════════════════════════════════════════
    //            ИНФОРМАЦИЯ ОБ ИГРОКАХ
    // ═══════════════════════════════════════════

    private void RefreshPlayerInfo()
    {
        SetPlayerPanel(gm.Player1, p1NameText, p1StabilityText, p1ResourcesText);
        SetPlayerPanel(gm.Player2, p2NameText, p2StabilityText, p2ResourcesText);
    }

    private void SetPlayerPanel(PlayerState p, TMP_Text nameT, TMP_Text stabT, TMP_Text resT)
    {
        nameT.text = p.playerName;
        stabT.text = $"Стабильность: {p.stability}";
        resT.text  = $"Д:{p.resourcePool[0]}  Л:{p.resourcePool[1]}  "
                   + $"П:{p.resourcePool[2]}  Т:{p.resourcePool[3]}";
    }

    // ═══════════════════════════════════════════
    //                  ПОЛЯ
    // ═══════════════════════════════════════════

    private void RefreshFields()
    {
        // Игрок 1 — враг, когда активен Игрок 2 (и наоборот)
        bool p1IsEnemy = gm.ActivePlayerIndex == 1;
        bool p2IsEnemy = gm.ActivePlayerIndex == 0;

        RebuildField(gm.Player1, p1ResourceContainer, p1UnitContainer, p1IsEnemy);
        RebuildField(gm.Player2, p2ResourceContainer, p2UnitContainer, p2IsEnemy);
    }

    private void RebuildField(PlayerState player,
                              Transform resCont, Transform unitCont, bool isEnemy)
    {
        ClearContainer(resCont);
        ClearContainer(unitCont);

        // Карты ресурсов на поле
        foreach (RuntimeResourceCard res in player.resourceField)
        {
            CardUI card = SpawnCard(resCont);
            card.SetupResourceOnField(res, isEnemy);
        }

        // Юниты на поле
        foreach (RuntimeUnitCard unit in player.unitField)
        {
            CardUI card = SpawnCard(unitCont);
            card.SetupUnitOnField(unit, isEnemy);

            // Подсветка выбранного атакующего
            if (cm.SelectedAttacker == unit)
                card.SetHighlight(Color.yellow);
        }
    }

    // ═══════════════════════════════════════════
    //                   РУКА
    // ═══════════════════════════════════════════

    private void RefreshHand()
    {
        ClearContainer(handContainer);

        foreach (ScriptableObject data in gm.ActivePlayer.hand)
        {
            CardUI card = SpawnCard(handContainer);

            if (data is ResourceCardData res)
                card.SetupResourceInHand(res);
            else if (data is UnitCardData unit)
                card.SetupUnitInHand(unit);
        }
    }

    // ═══════════════════════════════════════════
    //                 КНОПКИ
    // ═══════════════════════════════════════════

    private void RefreshButtons()
    {
        GamePhase phase = tm.CurrentPhase;

        endMainPhaseButton.gameObject.SetActive(
            phase == GamePhase.MainPhase);

        endCombatPhaseButton.gameObject.SetActive(
            phase == GamePhase.CombatPhase);

        attackStabilityButton.gameObject.SetActive(
            phase == GamePhase.CombatPhase
            && cm.SelectedAttacker != null
            && cm.CanSelectedAttackStability());
    }

    // ═══════════════════════════════════════════
    //           ФАЗА И ПОДСКАЗКИ
    // ═══════════════════════════════════════════

    private void RefreshPhaseInfo()
    {
        int turn = gm.CurrentPlayerTurn;

        phaseText.text = $"{gm.ActivePlayer.playerName}  |  Ход {turn}  |  "
                       + PhaseName(tm.CurrentPhase);

        hintText.text = GetHint(turn);
    }

    private string GetHint(int turn)
    {
        GamePhase phase = tm.CurrentPhase;

        if (phase == GamePhase.MainPhase)
        {
            if (turn <= 2)
                return "Выложите 1 карту ресурса (юниты недоступны на ходах 1-2)";

            return tm.CurrentTurnAction switch
            {
                TurnAction.None =>
                    "Выложите карты юнитов или 1 карту ресурса",
                TurnAction.PlayedResource =>
                    "Ресурс выложен. Нажмите «Завершить фазу».",
                TurnAction.PlayedUnits =>
                    "Можете выложить ещё юнитов или завершить фазу.",
                _ => ""
            };
        }

        if (phase == GamePhase.CombatPhase)
        {
            if (cm.SelectedAttacker != null)
                return "Выберите цель: вражеский юнит или «Атака по государству»";

            if (cm.HasRemainingAttacks())
                return "Выберите своего юнита для атаки или завершите бой";

            return "Нет готовых юнитов. Завершите бой.";
        }

        return "";
    }

    private string PhaseName(GamePhase p) => p switch
    {
        GamePhase.DrawPhase     => "Набор карт",
        GamePhase.ResourcePhase => "Генерация",
        GamePhase.MainPhase     => "Основная фаза",
        GamePhase.CombatPhase   => "Боевая фаза",
        GamePhase.EndPhase      => "Конец хода",
        GamePhase.GameOver      => "Игра окончена",
        _ => ""
    };

    // ═══════════════════════════════════════════
    //             КЛИК ПО КАРТЕ
    // ═══════════════════════════════════════════

    private void OnCardClicked(CardUI card)
    {
        switch (card.Location)
        {
            case CardLocation.Hand:
                HandleHandClick(card);
                break;
            case CardLocation.OwnField:
                HandleOwnFieldClick(card);
                break;
            case CardLocation.EnemyField:
                HandleEnemyFieldClick(card);
                break;
        }
    }

    private void HandleHandClick(CardUI card)
    {
        if (tm.CurrentPhase != GamePhase.MainPhase) return;

        if (card.CardData is ResourceCardData res)
            tm.TryPlayResourceCard(res);
        else if (card.CardData is UnitCardData unit)
            tm.TryPlayUnitCard(unit);
    }

    private void HandleOwnFieldClick(CardUI card)
    {
        if (tm.CurrentPhase != GamePhase.CombatPhase) return;
        if (card.RuntimeUnit == null) return;

        // Повторный клик — снять выбор
        if (cm.SelectedAttacker == card.RuntimeUnit)
            cm.DeselectAttacker();
        else
            cm.SelectAttacker(card.RuntimeUnit);
    }

    private void HandleEnemyFieldClick(CardUI card)
    {
        if (tm.CurrentPhase != GamePhase.CombatPhase) return;
        if (card.RuntimeUnit == null) return;

        cm.AttackUnit(card.RuntimeUnit);
    }

    // ═══════════════════════════════════════════
    //          ОБРАБОТЧИКИ КНОПОК
    // ═══════════════════════════════════════════

    private void OnEndMainPhaseClicked()
    {
        tm.EndMainPhase();
    }

    private void OnEndCombatPhaseClicked()
    {
        tm.EndCombatPhase();
    }

    private void OnAttackStabilityClicked()
    {
        cm.AttackStability();
    }

    // ═══════════════════════════════════════════
    //           ЭКРАН ПЕРЕДАЧИ ХОДА
    // ═══════════════════════════════════════════

    private void ShowTransitionScreen()
    {
        transitionScreen.SetActive(true);
        transitionText.text = $"Передайте управление {gm.ActivePlayer.playerName}";
    }

    private void OnContinueClicked()
    {
        transitionScreen.SetActive(false);
        tm.StartNewTurn();
    }

    // ═══════════════════════════════════════════
    //            ЭКРАН ПОБЕДЫ
    // ═══════════════════════════════════════════

    private void ShowVictoryScreen(PlayerState winner)
    {
        victoryScreen.SetActive(true);
        victoryText.text = $"{winner.playerName} победило!";
    }

    private void OnRestartClicked()
    {
        victoryScreen.SetActive(false);
        gm.RestartGame();
    }

    // ═══════════════════════════════════════════
    //               УТИЛИТЫ
    // ═══════════════════════════════════════════

    private CardUI SpawnCard(Transform parent)
    {
        GameObject go = Instantiate(cardPrefab, parent);
        CardUI card = go.GetComponent<CardUI>();
        card.OnClicked += OnCardClicked;
        return card;
    }

    private void ClearContainer(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            GameObject child = container.GetChild(i).gameObject;
            CardUI card = child.GetComponent<CardUI>();
            if (card != null) card.OnClicked -= OnCardClicked;
            child.SetActive(false);   // скрыть сразу (Destroy — конец кадра)
            Destroy(child);
        }
    }
}