using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Управляет колодами карт.
    /// Ресурсы — у каждого игрока своя конечная колода.
    /// Юниты — два бесконечных пула:
    ///   - Базовый: доступен с 1-го хода.
    ///   - Продвинутый: доступен с 5-го хода (объединяется с базовым).
    /// </summary>
    public class DeckManager : MonoBehaviour
    {
        [Header("Состав колоды ресурсов (конечная, для каждого игрока своя)")]
        [SerializeField] private ResourceDeckEntry[] resourceEntries;

        [Header("Пул юнитов: стартовый")]
        [SerializeField] private UnitCardData[] unitPoolBase;

        [Header("Пул юнитов: продвинутый")]
        [SerializeField] private UnitCardData[] unitPoolAdvanced;

        [Header("С какого хода открывается продвинутый пул")]
        [SerializeField] private int advancedPoolUnlockTurn = 5;

        // ── Рантайм-колоды ресурсов ──
        private Dictionary<PlayerState, List<ResourceCardData>> resourceDecks;

        // ── Публичные свойства ──
        public int GetResourceCardsRemaining(PlayerState player)
        {
            if (resourceDecks != null && resourceDecks.TryGetValue(player, out var deck))
                return deck.Count;
            return 0;
        }

        // ═══════════════════════════════════════════
        //           ИНИЦИАЛИЗАЦИЯ
        // ═══════════════════════════════════════════

        public void InitializeDecks(PlayerState player1, PlayerState player2)
        {
            resourceDecks = new Dictionary<PlayerState, List<ResourceCardData>>();

            resourceDecks[player1] = BuildResourceDeck();
            resourceDecks[player2] = BuildResourceDeck();

            int baseCount = unitPoolBase != null ? unitPoolBase.Length : 0;
            int advCount = unitPoolAdvanced != null ? unitPoolAdvanced.Length : 0;

            Debug.Log($"  Колоды ресурсов собраны: " +
                      $"{player1.playerName} = {resourceDecks[player1].Count} карт, " +
                      $"{player2.playerName} = {resourceDecks[player2].Count} карт. " +
                      $"Пул юнитов: базовый {baseCount}, продвинутый {advCount} " +
                      $"(открывается с хода {advancedPoolUnlockTurn}).");
        }

        private List<ResourceCardData> BuildResourceDeck()
        {
            List<ResourceCardData> deck = new List<ResourceCardData>();

            if (resourceEntries == null) return deck;

            foreach (ResourceDeckEntry entry in resourceEntries)
            {
                if (entry.card == null) continue;
                for (int i = 0; i < entry.count; i++)
                    deck.Add(entry.card);
            }

            Shuffle(deck);
            return deck;
        }

        // ═══════════════════════════════════════════
        //              ВЫТЯГИВАНИЕ КАРТ
        // ═══════════════════════════════════════════

        public ResourceCardData DrawRandomResourceCard(PlayerState player)
        {
            if (resourceDecks == null || !resourceDecks.TryGetValue(player, out var deck))
            {
                Debug.LogWarning($"DeckManager: колода ресурсов для {player.playerName} не найдена!");
                return null;
            }

            if (deck.Count == 0)
            {
                Debug.LogWarning($"DeckManager: колода ресурсов {player.playerName} пуста!");
                return null;
            }

            int lastIndex = deck.Count - 1;
            ResourceCardData card = deck[lastIndex];
            deck.RemoveAt(lastIndex);

            Debug.Log($"    {player.playerName} вытянул ресурс: {card.cardName} " +
                      $"(осталось: {deck.Count})");
            return card;
        }

        /// <summary>
        /// Вытянуть случайную карту юнита.
        /// До хода advancedPoolUnlockTurn — только из базового пула.
        /// С хода advancedPoolUnlockTurn — из объединённого (базовый + продвинутый).
        /// </summary>
        public UnitCardData DrawRandomUnitCard(int currentTurn)
        {
            bool baseAvailable = unitPoolBase != null && unitPoolBase.Length > 0;
            bool advancedAvailable = unitPoolAdvanced != null
                                     && unitPoolAdvanced.Length > 0
                                     && currentTurn >= advancedPoolUnlockTurn;

            if (!baseAvailable && !advancedAvailable)
            {
                Debug.LogWarning("DeckManager: пул карт юнитов пуст!");
                return null;
            }

            int baseCount = baseAvailable ? unitPoolBase.Length : 0;
            int advCount = advancedAvailable ? unitPoolAdvanced.Length : 0;
            int totalCount = baseCount + advCount;

            int roll = Random.Range(0, totalCount);

            UnitCardData card;
            if (roll < baseCount)
            {
                card = unitPoolBase[roll];
            }
            else
            {
                card = unitPoolAdvanced[roll - baseCount];
                Debug.Log($"    ★ Выпал продвинутый юнит: {card.cardName}");
            }

            return card;
        }

        // ═══════════════════════════════════════════
        //              ПЕРЕМЕШИВАНИЕ
        // ═══════════════════════════════════════════

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}