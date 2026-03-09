using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "NewResourceCard", menuName = "Cards/Resource Card")]
    public class ResourceCardData : ScriptableObject
    {
        [Header("Основная информация")]
        public string cardName;

        [TextArea(2, 4)]
        public string description;

        [Header("Тип ресурса")]
        public ResourceType resourceType;

        [Header("Коэффициент генерации")]
        [Tooltip("Сколько единиц ресурса генерирует карта за ход. По умолчанию 1.")]
        [Min(1)]
        public int generationCoefficient = 1;
    }
}