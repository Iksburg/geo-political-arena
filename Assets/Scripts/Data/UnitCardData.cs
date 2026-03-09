using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "NewUnitCard", menuName = "Cards/Unit Card")]
    public class UnitCardData : ScriptableObject
    {
        [Header("Основная информация")]
        public string cardName;

        [TextArea(2, 4)]
        public string description;

        [Header("Стоимость розыгрыша")]
        [Tooltip("Деньги")]
        [Min(0)]
        public int costMoney;

        [Tooltip("Лояльность населения")]
        [Min(0)]
        public int costLoyalty;

        [Tooltip("Производственные мощности")]
        [Min(0)]
        public int costProduction;

        [Tooltip("Технологии")]
        [Min(0)]
        public int costTechnology;

        [Header("Боевые характеристики")]
        [Tooltip("Урон, наносимый вражеским юнитам")]
        [Min(0)]
        public int damageToUnits;

        [Tooltip("Урон, наносимый стабильности государства. По умолчанию = damageToUnits - 1")]
        [Min(0)]
        public int damageToStability;

        [Tooltip("Урон, наносимый атакующему при защите. По умолчанию 0")]
        [Min(0)]
        public int counterDamage;

        [Header("Защитные характеристики")]
        [Tooltip("Запас прочности юнита")]
        [Min(1)]
        public int hp = 1;

        [Tooltip("Снижение входящего урона")]
        [Min(0)]
        public int armor;

        [Header("Особые свойства")]
        [Tooltip("Через сколько ходов после выкладки юнит может атаковать. По умолчанию 1")]
        [Min(0)]
        public int attackDelay = 1;

        [Tooltip("Может ли юнит игнорировать вражеских юнитов и бить напрямую по стабильности")]
        public bool canDirectAttack;

        /// <summary>
        /// Возвращает стоимость в виде массива [Money, Loyalty, Production, Technology].
        /// Удобно для программной проверки.
        /// </summary>
        public int[] GetCostArray()
        {
            return new int[] { costMoney, costLoyalty, costProduction, costTechnology };
        }
    }
}