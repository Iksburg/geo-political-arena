using Data;

namespace Cards
{
    /// <summary>
    /// Мутабельный экземпляр карты ресурса на поле.
    /// Позволяет изменять коэффициент генерации при объединении карт одного типа.
    /// </summary>
    public class RuntimeResourceCard
    {
        /// <summary>Ссылка на исходные данные последней добавленной карты.</summary>
        public ResourceCardData data;

        /// <summary>Тип ресурса (для быстрого сравнения).</summary>
        public ResourceType resourceType;

        /// <summary>Текущий коэффициент генерации. Растёт при объединении.</summary>
        public int currentGeneration;

        public RuntimeResourceCard(ResourceCardData sourceData)
        {
            data = sourceData;
            resourceType = sourceData.resourceType;
            currentGeneration = sourceData.generationCoefficient;
        }

        /// <summary>
        /// Объединить с ещё одной картой того же типа.
        /// Увеличивает генерацию на 1.
        /// </summary>
        public void Merge()
        {
            currentGeneration += 1;
        }
    }
}