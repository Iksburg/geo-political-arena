# Geo-Political Arena

A two-player, turn-based card game where opponents collect resources, deploy units, and battle to reduce the enemy's stability to zero.

---

## Overview

Geo-Political Arena is built in Unity (URP, 2D) and uses a manager-based architecture with ScriptableObjects for card data. Players take turns drawing cards, playing resource cards to fuel their economy, and deploying unit cards to attack the opponent's units or stability directly.

---

## Architecture

### Patterns

| Pattern | Usage |
|--------|--------|
| **Singleton** | `GameManager` – central match coordinator |
| **Manager / Service** | `TurnManager`, `DeckManager`, `ResourceManager`, `CombatManager` – separate responsibilities |
| **ScriptableObject Data** | Card definitions (`ResourceCardData`, `UnitCardData`) as assets |
| **Event-based UI** | Managers expose C# events; `UIManager` subscribes and refreshes views |
| **Runtime Wrappers** | `RuntimeResourceCard`, `RuntimeUnitCard` – in-game state separate from data assets |

### Namespaces

- **Data** – Enums, ScriptableObject types, deck entry structs
- **Cards** – Runtime card wrappers
- **Core** – Deck, resources, turn logic
- **Combat** – Combat resolution
- **UI** – UIManager, CardUI, player panels

### Main Systems

| System | Role |
|--------|------|
| `GameManager` | Match state, players, victory check; starts game and wires managers |
| `TurnManager` | Turn phases (Draw → Resource → Main → Combat → End); validates card plays |
| `DeckManager` | Resource and unit decks per player; drawing and deck setup |
| `ResourceManager` | Resource generation from resource-field cards |
| `CombatManager` | Attacker selection, targeting, damage, unit death, stability damage |
| `UIManager` | UI refresh from events; card clicks, phase/buttons, pass-turn screen |

---

## Card Mechanics

### Card Types

#### Resource Cards (`ResourceCardData`)

- **Types:** Money, Loyalty, Production, Technology
- **Purpose:** Generate resources each turn
- **Merge rule:** Same-type cards on the resource field stack; `Merge()` increases generation coefficient
- **Play limit:** One resource card per turn (XOR with unit plays on turn 3+)

#### Unit Cards (`UnitCardData`)

- **Cost:** Money, Loyalty, Production, Technology (per card definition)
- **Stats:**
  - `damageToUnits` / `damageToStability` – combat damage
  - `counterDamage` – damage dealt when defending
  - `hp`, `armor` – survivability
  - `attackDelay` – turns before first attack
  - `canDirectAttack` – can target opponent stability when they have no units
- **Availability:** Playable from turn 3 onward; paid and placed on the unit field

### Runtime Card Behavior

- **RuntimeResourceCard** – Tracks `currentGeneration`; `Merge()` for stacking
- **RuntimeUnitCard** – Tracks `currentHp`, `turnsOnField`, `hasAttackedThisTurn`; `CanAttack()` (delay, attack flag); `TakeDamage()` with armor

### Victory Condition

- Win when opponent stability reaches 0
- Stability can be attacked directly only if:
  - The attacker has `canDirectAttack`, or
  - The opponent has no units left

---

## Turn Flow

1. **Draw Phase**
   - Turn 1: No draw
   - Turn 2: 1 resource + 1 unit
   - Turn 3+: 1 unit; extra resource every 4 turns
2. **Resource Phase** – Resources generated from resource-field cards
3. **Main Phase** – Play up to 1 resource **or** any number of units (exclusive on turn 3+)
4. **Combat Phase** (turn 3+) – Select attacker → target enemy unit or stability
5. **End Phase** – Increment `turnsOnField`, switch active player, show pass-turn screen

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # Game loop, turns, deck, resources
│   ├── Cards/          # Runtime card wrappers
│   ├── Combat/         # Combat resolution
│   ├── Data/           # ScriptableObject definitions, enums, deck entries
│   └── UI/             # UIManager, CardUI, PlayerPanelUI
├── ScriptableObjects/
│   ├── ResourceCards/  # Money, Loyalty, Production, Technology
│   └── UnitCards/
│       ├── TwoCost/    # Bot, Civilian Drone, Propagandist, etc.
│       ├── ThreeCost/  # Cyberattack, MLRS, Regular Army, etc.
│       └── FourCost/   # Military Drone, Heavy Armored, Special Forces, etc.
├── Prefabs/            # CardPrefab (140×190, Image, Button, CardUI)
├── Scenes/             # CardField.unity (main scene)
└── InputSystem_Actions.inputactions
```

---

## Tech Stack

- **Unity** (URP, 2D)
- **Input System** (1.18+)
- **TextMesh Pro**
- **Default resolution:** 1920×1080

---

## How to Run

1. Open the project in Unity.
2. Ensure `Assets/Scenes/CardField.unity` is the active scene.
3. Press Play.
