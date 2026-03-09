using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Cards;
using Data;

public enum CardLocation
{
    Hand,
    OwnField,
    EnemyField
}

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class CardUI : MonoBehaviour
{
    [Header("Дочерние текстовые поля")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text infoText;

    // ── Кэш компонентов ──
    private Image background;
    private Button button;
    private Outline outline;

    // ── Публичные данные (read-only) ──
    public ScriptableObject CardData { get; private set; }
    public RuntimeUnitCard RuntimeUnit { get; private set; }
    public CardLocation Location { get; private set; }

    // ── Событие ──
    public event Action<CardUI> OnClicked;

    // ── Палитра ──
    private static readonly Color ColMoney      = new Color(1f, 0.84f, 0f);
    private static readonly Color ColLoyalty     = new Color(0.4f, 0.6f, 1f);
    private static readonly Color ColProduction  = new Color(1f, 0.6f, 0.2f);
    private static readonly Color ColTechnology  = new Color(0.65f, 0.4f, 1f);
    private static readonly Color ColUnit        = new Color(0.78f, 0.78f, 0.78f);
    private static readonly Color ColEnemyUnit   = new Color(0.95f, 0.55f, 0.55f);
    private static readonly Color ColReadyUnit   = new Color(0.55f, 0.9f, 0.55f);

    // ═══════════════════════════════════════════

    void Awake()
    {
        background = GetComponent<Image>();
        button = GetComponent<Button>();

        outline = GetComponent<Outline>();
        if (outline == null)
            outline = gameObject.AddComponent<Outline>();
        outline.effectDistance = new Vector2(3, 3);
        outline.enabled = false;

        button.onClick.AddListener(() => OnClicked?.Invoke(this));
    }

    // ═══════════════════ SETUP ═══════════════════

    /// <summary>Карта ресурса в руке.</summary>
    public void SetupResourceInHand(ResourceCardData data)
    {
        CardData = data;
        RuntimeUnit = null;
        Location = CardLocation.Hand;

        nameText.text = data.cardName;
        infoText.text = $"Тип: {TypeName(data.resourceType)}\nГенерация: x{data.generationCoefficient}";
        background.color = TypeColor(data.resourceType);
        ClearHighlight();
    }

    /// <summary>Карта юнита в руке.</summary>
    public void SetupUnitInHand(UnitCardData data)
    {
        CardData = data;
        RuntimeUnit = null;
        Location = CardLocation.Hand;

        nameText.text = data.cardName;
        infoText.text = $"Урон по юнитам: {data.damageToUnits} \nУрон по гос.: {data.damageToStability} \nHP: {data.hp} \nБроня: {data.armor}"
                      + (data.canDirectAttack ? "\nСпособность: прямая атака" : "")
                      + "\n" + FormatCost(data);
        background.color = ColUnit;
        ClearHighlight();
    }

    /// <summary>Карта ресурса на поле.</summary>
    public void SetupResourceOnField(RuntimeResourceCard runtimeRes, bool isEnemy)
    {
        CardData = runtimeRes.data;
        RuntimeUnit = null;
        Location = isEnemy ? CardLocation.EnemyField : CardLocation.OwnField;

        nameText.text = runtimeRes.data.cardName;
        infoText.text = $"Тип: {TypeName(runtimeRes.resourceType)}\nГенерация: x{runtimeRes.currentGeneration}";
        Color c = TypeColor(runtimeRes.resourceType);
        background.color = isEnemy ? c * 0.65f : c;
        ClearHighlight();
    }

    /// <summary>Юнит на поле.</summary>
    public void SetupUnitOnField(RuntimeUnitCard unit, bool isEnemy)
    {
        CardData = unit.data;
        RuntimeUnit = unit;
        Location = isEnemy ? CardLocation.EnemyField : CardLocation.OwnField;

        nameText.text = unit.data.cardName;
        infoText.text = $"Урон по юнитам: {unit.data.damageToUnits} \nУрон по гос.: {unit.data.damageToStability} \nHP: {unit.currentHp}/{unit.data.hp}  \nБроня: {unit.data.armor}"
                        + (unit.data.canDirectAttack ? "\nСпособность: прямая атака" : "");

        if (isEnemy)
            background.color = ColEnemyUnit;
        else
            background.color = unit.CanAttack() ? ColReadyUnit : ColUnit;

        ClearHighlight();
    }

    // ═══════════════════ ПОДСВЕТКА ═══════════════════

    public void SetHighlight(Color color)
    {
        outline.effectColor = color;
        outline.enabled = true;
    }

    public void ClearHighlight()
    {
        outline.enabled = false;
    }

    // ═══════════════════ УТИЛИТЫ ═══════════════════

    private string TypeName(ResourceType t) => t switch
    {
        ResourceType.Money      => "Деньги",
        ResourceType.Loyalty    => "Лояльность",
        ResourceType.Production => "Производство",
        ResourceType.Technology => "Технологии",
        _ => "?"
    };

    private Color TypeColor(ResourceType t) => t switch
    {
        ResourceType.Money      => ColMoney,
        ResourceType.Loyalty    => ColLoyalty,
        ResourceType.Production => ColProduction,
        ResourceType.Technology => ColTechnology,
        _ => Color.white
    };

    private string FormatCost(UnitCardData d)
    {
        List<string> p = new List<string>();
        if (d.costMoney > 0)      p.Add($"Д:{d.costMoney}");
        if (d.costLoyalty > 0)    p.Add($"Л:{d.costLoyalty}");
        if (d.costProduction > 0) p.Add($"П:{d.costProduction}");
        if (d.costTechnology > 0) p.Add($"Т:{d.costTechnology}");
        return p.Count > 0 ? string.Join(" ", p) : "Бесплатно";
    }
}