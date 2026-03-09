using System.Collections.Generic;
using UnityEngine;
using Cards;
using Data;

public class PlayerState
{
    public string playerName;
    public int stability;
    public int[] resourcePool;

    /// <summary>
    /// Карты ресурсов на поле. Максимум одна запись на каждый тип ресурса.
    /// </summary>
    public List<RuntimeResourceCard> resourceField;

    public List<RuntimeUnitCard> unitField;
    public List<ScriptableObject> hand;

    public PlayerState(string name, int startingStability = 30)
    {
        playerName = name;
        stability = startingStability;
        resourcePool = new int[4];
        resourceField = new List<RuntimeResourceCard>();
        unitField = new List<RuntimeUnitCard>();
        hand = new List<ScriptableObject>();
    }

    /// <summary>
    /// Генерация ресурсов со всех карт на поле.
    /// </summary>
    public void GenerateResources()
    {
        foreach (RuntimeResourceCard card in resourceField)
        {
            int index = (int)card.resourceType;
            resourcePool[index] += card.currentGeneration;
        }
    }

    /// <summary>
    /// Найти карту ресурса на поле по типу. Null если нет.
    /// </summary>
    public RuntimeResourceCard FindResourceOnField(ResourceType type)
    {
        foreach (RuntimeResourceCard card in resourceField)
        {
            if (card.resourceType == type)
                return card;
        }
        return null;
    }

    public bool CanAfford(UnitCardData unit)
    {
        int[] cost = unit.GetCostArray();
        for (int i = 0; i < 4; i++)
        {
            if (resourcePool[i] < cost[i])
                return false;
        }
        return true;
    }

    public void SpendResources(UnitCardData unit)
    {
        int[] cost = unit.GetCostArray();
        for (int i = 0; i < 4; i++)
            resourcePool[i] -= cost[i];
    }

    public int GetResource(ResourceType type)
    {
        return resourcePool[(int)type];
    }

    public bool IsAlive()
    {
        return stability > 0;
    }
}