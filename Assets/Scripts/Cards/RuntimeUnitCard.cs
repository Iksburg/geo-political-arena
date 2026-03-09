using Data;

namespace Cards
{
    /// <summary>
    /// Мутабельный экземпляр юнита, находящегося на игровом поле.
    /// Хранит текущее состояние, отличное от исходных данных ScriptableObject.
    /// </summary>
    public class RuntimeUnitCard
    {
        /// <summary>
        /// Ссылка на исходные неизменяемые данные карты.
        /// </summary>
        public UnitCardData data;

        /// <summary>
        /// Текущее здоровье. Когда <= 0, юнит уничтожается.
        /// </summary>
        public int currentHp;

        /// <summary>
        /// Сколько полных ходов юнит провёл на поле.
        /// Увеличивается на 1 в конце каждого хода владельца.
        /// Юнит может атаковать, когда turnsOnField >= data.attackDelay.
        /// </summary>
        public int turnsOnField;

        /// <summary>
        /// Флаг: атаковал ли юнит в текущем ходу.
        /// Сбрасывается в начале каждого хода владельца.
        /// </summary>
        public bool hasAttackedThisTurn;

        public RuntimeUnitCard(UnitCardData sourceData)
        {
            data = sourceData;
            currentHp = sourceData.hp;
            turnsOnField = 0;
            hasAttackedThisTurn = false;
        }

        /// <summary>
        /// Может ли юнит атаковать прямо сейчас.
        /// </summary>
        public bool CanAttack()
        {
            return !hasAttackedThisTurn && turnsOnField >= data.attackDelay;
        }

        /// <summary>
        /// Юнит жив?
        /// </summary>
        public bool IsAlive()
        {
            return currentHp > 0;
        }

        /// <summary>
        /// Применить урон с учётом брони.
        /// Возвращает фактически нанесённый урон.
        /// </summary>
        public int TakeDamage(int incomingDamage)
        {
            var effectiveDamage = incomingDamage - data.armor;
            if (effectiveDamage < 0)
                effectiveDamage = 0;

            currentHp -= effectiveDamage;
            return effectiveDamage;
        }
    }
}