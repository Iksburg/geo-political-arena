using System;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Отвечает за генерацию ресурсов из карт на поле.
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        /// <summary>
        /// Вызывается после генерации. Аргументы: игрок, массив приростов [4].
        /// </summary>
        public event Action<PlayerState, int[]> OnResourcesGenerated;

        /// <summary>
        /// Подсчитать и начислить ресурсы от всех карт на поле игрока.
        /// </summary>
        public void GenerateResourcesForPlayer(PlayerState player)
        {
            // Запоминаем состояние до генерации
            var before = (int[])player.resourcePool.Clone();

            // Генерация (метод из PlayerState)
            player.GenerateResources();

            // Считаем прирост
            var income = new int[4];
            for (var i = 0; i < 4; i++)
                income[i] = player.resourcePool[i] - before[i];

            Debug.Log($"  Ресурсы {player.playerName}: " +
                      $"Деньги +{income[0]}={player.resourcePool[0]}, " +
                      $"Лояльность +{income[1]}={player.resourcePool[1]}, " +
                      $"Производство +{income[2]}={player.resourcePool[2]}, " +
                      $"Технологии +{income[3]}={player.resourcePool[3]}");

            OnResourcesGenerated?.Invoke(player, income);
        }
    }
}