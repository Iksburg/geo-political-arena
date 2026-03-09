using System;
using UnityEngine;

namespace Data
{
    [Serializable]
    public struct ResourceDeckEntry
    {
        [Tooltip("Карта ресурса")]
        public ResourceCardData card;

        [Tooltip("Количество копий в колоде")]
        [Min(1)]
        public int count;
    }

    [Serializable]
    public struct UnitDeckEntry
    {
        [Tooltip("Карта юнита")]
        public UnitCardData card;

        [Tooltip("Количество копий в колоде")]
        [Min(1)]
        public int count;
    }
}