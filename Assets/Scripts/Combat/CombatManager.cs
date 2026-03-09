using System;
using System.Collections.Generic;
using Cards;
using Core;
using Data;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// Управляет боевой фазой: выбор атакующего, назначение цели,
    /// расчёт урона, уничтожение юнитов, атака по стабильности.
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        // ─────────── Ссылки ───────────
        [Header("Менеджеры")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TurnManager turnManager;

        // ─────────── Выбор в бою ───────────
        /// <summary>Юнит, выбранный для атаки (null — ничего не выбрано).</summary>
        public RuntimeUnitCard SelectedAttacker { get; private set; }

        // ─────────── События ───────────

        /// <summary>Игрок выбрал атакующего юнита.</summary>
        public event Action<RuntimeUnitCard> OnAttackerSelected;

        /// <summary>Выбор атакующего снят.</summary>
        public event Action OnAttackerDeselected;

        /// <summary>
        /// Юнит атаковал другого юнита.
        /// Аргументы: атакующий, защитник, урон по защитнику, ответный урон по атакующему.
        /// </summary>
        public event Action<RuntimeUnitCard, RuntimeUnitCard, int, int> OnUnitAttackedUnit;

        /// <summary>
        /// Юнит атаковал стабильность противника.
        /// Аргументы: атакующий, нанесённый урон, оставшаяся стабильность.
        /// </summary>
        public event Action<RuntimeUnitCard, int, int> OnUnitAttackedStability;

        /// <summary>Юнит уничтожен (HP <= 0). Аргумент: погибший юнит.</summary>
        public event Action<RuntimeUnitCard> OnUnitDestroyed;

        // ═══════════════════════════════════════════

        void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
            if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        }

        // ═══════════════════════════════════════════
        //              ВЫБОР АТАКУЮЩЕГО
        // ═══════════════════════════════════════════

        /// <summary>
        /// Выбрать юнита для атаки. Вызывается по клику на своего юнита в фазе боя.
        /// </summary>
        public bool SelectAttacker(RuntimeUnitCard unit)
        {
            if (!ValidateCombatPhase()) return false;

            // Юнит должен принадлежать активному игроку
            PlayerState player = gameManager.ActivePlayer;
            if (!player.unitField.Contains(unit))
            {
                Debug.LogWarning("  ✘ Этот юнит не принадлежит активному игроку");
                return false;
            }

            // Проверяем, может ли юнит атаковать
            if (!unit.CanAttack())
            {
                if (unit.hasAttackedThisTurn)
                    Debug.LogWarning($"  ✘ {unit.data.cardName} уже атаковал в этом ходу");
                else
                    Debug.LogWarning($"  ✘ {unit.data.cardName} ещё не готов " +
                                     $"(на поле {unit.turnsOnField} ходов, нужно {unit.data.attackDelay})");
                return false;
            }

            SelectedAttacker = unit;
            Debug.Log($"  ► Выбран атакующий: {unit.data.cardName} " +
                      $"(Атака={unit.data.damageToUnits}, HP={unit.currentHp})");

            OnAttackerSelected?.Invoke(unit);
            return true;
        }

        /// <summary>
        /// Снять выбор атакующего.
        /// </summary>
        public void DeselectAttacker()
        {
            if (SelectedAttacker != null)
            {
                Debug.Log($"  ◄ Выбор снят: {SelectedAttacker.data.cardName}");
                SelectedAttacker = null;
                OnAttackerDeselected?.Invoke();
            }
        }

        // ═══════════════════════════════════════════
        //           АТАКА ПО ВРАЖЕСКОМУ ЮНИТУ
        // ═══════════════════════════════════════════

        /// <summary>
        /// Атаковать вражеского юнита. Вызывается по клику на юнита противника.
        /// </summary>
        public bool AttackUnit(RuntimeUnitCard target)
        {
            if (!ValidateAttackerSelected()) return false;

            PlayerState opponent = gameManager.OpponentPlayer;

            // Цель должна быть на поле противника
            if (!opponent.unitField.Contains(target))
            {
                Debug.LogWarning("  ✘ Цель не найдена на поле противника");
                return false;
            }

            RuntimeUnitCard attacker = SelectedAttacker;

            // ── Расчёт урона по защитнику ──
            int damageToTarget = target.TakeDamage(attacker.data.damageToUnits);

            // ── Ответный урон по атакующему ──
            int counterDmg = 0;
            if (target.data.counterDamage > 0 && target.IsAlive())
            {
                counterDmg = attacker.TakeDamage(target.data.counterDamage);
            }

            // Отметка: атаковал
            attacker.hasAttackedThisTurn = true;

            Debug.Log($"  ⚔ {attacker.data.cardName} → {target.data.cardName}");
            Debug.Log($"      Урон: {damageToTarget} (входящий {attacker.data.damageToUnits}" +
                      $" − броня {target.data.armor})");
            Debug.Log($"      HP цели: {target.currentHp}/{target.data.hp}");
            if (counterDmg > 0)
            {
                Debug.Log($"      Ответный урон: {counterDmg}");
                Debug.Log($"      HP атакующего: {attacker.currentHp}/{attacker.data.hp}");
            }

            OnUnitAttackedUnit?.Invoke(attacker, target, damageToTarget, counterDmg);

            // ── Проверяем гибель ──
            if (!target.IsAlive())
                DestroyUnit(target, opponent);

            if (!attacker.IsAlive())
                DestroyUnit(attacker, gameManager.ActivePlayer);

            // Сброс выбора
            SelectedAttacker = null;
            OnAttackerDeselected?.Invoke();

            return true;
        }

        // ═══════════════════════════════════════════
        //          АТАКА ПО СТАБИЛЬНОСТИ
        // ═══════════════════════════════════════════

        /// <summary>
        /// Атаковать стабильность (HP) противника.
        /// Вызывается кнопкой UI "Атаковать стабильность".
        /// </summary>
        public bool AttackStability()
        {
            if (!ValidateAttackerSelected()) return false;

            RuntimeUnitCard attacker = SelectedAttacker;
            PlayerState opponent = gameManager.OpponentPlayer;

            // Проверка: есть ли юниты у противника
            bool enemyHasUnits = opponent.unitField.Count > 0;

            if (enemyHasUnits && !attacker.data.canDirectAttack)
            {
                Debug.LogWarning($"  ✘ Нельзя атаковать стабильность: " +
                                 $"у противника {opponent.unitField.Count} юнит(ов) на поле.\n" +
                                 $"    Уничтожьте их или используйте юнита со способностью прямой атаки.");
                return false;
            }

            // ── Нанесение урона стабильности ──
            int damage = attacker.data.damageToStability;
            opponent.stability -= damage;
            if (opponent.stability < 0)
                opponent.stability = 0;

            attacker.hasAttackedThisTurn = true;

            Debug.Log($"  💥 {attacker.data.cardName} → Стабильность {opponent.playerName}");
            Debug.Log($"      Урон: {damage}");
            Debug.Log($"      Стабильность: {opponent.stability}/{30}");

            if (enemyHasUnits)
                Debug.Log($"      (прямая атака — юниты противника проигнорированы)");

            OnUnitAttackedStability?.Invoke(attacker, damage, opponent.stability);

            // Сброс выбора
            SelectedAttacker = null;
            OnAttackerDeselected?.Invoke();

            // ── Проверка победы ──
            gameManager.CheckVictoryCondition();

            return true;
        }

        // ═══════════════════════════════════════════
        //          УНИЧТОЖЕНИЕ ЮНИТА
        // ═══════════════════════════════════════════

        private void DestroyUnit(RuntimeUnitCard unit, PlayerState owner)
        {
            owner.unitField.Remove(unit);
            Debug.Log($"  ☠ {unit.data.cardName} уничтожен! (владелец: {owner.playerName})");
            OnUnitDestroyed?.Invoke(unit);
        }

        // ═══════════════════════════════════════════
        //        ЗАПРОСЫ ДЛЯ UI / ПОДСВЕТКИ
        // ═══════════════════════════════════════════

        /// <summary>
        /// Может ли текущий выбранный атакующий бить по стабильности.
        /// </summary>
        public bool CanSelectedAttackStability()
        {
            if (SelectedAttacker == null) return false;

            PlayerState opponent = gameManager.OpponentPlayer;

            if (opponent.unitField.Count == 0)
                return true;

            return SelectedAttacker.data.canDirectAttack;
        }

        /// <summary>
        /// Список юнитов активного игрока, готовых атаковать.
        /// </summary>
        public List<RuntimeUnitCard> GetReadyAttackers()
        {
            List<RuntimeUnitCard> ready = new List<RuntimeUnitCard>();
            foreach (RuntimeUnitCard unit in gameManager.ActivePlayer.unitField)
            {
                if (unit.CanAttack())
                    ready.Add(unit);
            }
            return ready;
        }

        /// <summary>
        /// Список допустимых целей для текущего выбранного атакующего.
        /// Возвращает вражеских юнитов. Если список пуст или у атакующего canDirectAttack —
        /// стабильность тоже доступна (проверять через CanSelectedAttackStability).
        /// </summary>
        public List<RuntimeUnitCard> GetValidTargets()
        {
            List<RuntimeUnitCard> targets = new List<RuntimeUnitCard>();

            if (SelectedAttacker == null) return targets;

            foreach (RuntimeUnitCard unit in gameManager.OpponentPlayer.unitField)
            {
                targets.Add(unit);
            }

            return targets;
        }

        /// <summary>
        /// Остались ли у активного игрока юниты, которые ещё не атаковали и могут это сделать.
        /// Полезно для UI: показывать подсказку или автоматически завершать боевую фазу.
        /// </summary>
        public bool HasRemainingAttacks()
        {
            foreach (RuntimeUnitCard unit in gameManager.ActivePlayer.unitField)
            {
                if (unit.CanAttack())
                    return true;
            }
            return false;
        }

        // ═══════════════════════════════════════════
        //              ВАЛИДАЦИИ
        // ═══════════════════════════════════════════

        private bool ValidateCombatPhase()
        {
            if (turnManager.CurrentPhase != GamePhase.CombatPhase)
            {
                Debug.LogWarning("  ✘ Сейчас не боевая фаза");
                return false;
            }
            return true;
        }

        private bool ValidateAttackerSelected()
        {
            if (!ValidateCombatPhase()) return false;

            if (SelectedAttacker == null)
            {
                Debug.LogWarning("  ✘ Атакующий не выбран");
                return false;
            }

            if (!SelectedAttacker.IsAlive())
            {
                Debug.LogWarning("  ✘ Атакующий мёртв");
                SelectedAttacker = null;
                return false;
            }

            return true;
        }
    }
}