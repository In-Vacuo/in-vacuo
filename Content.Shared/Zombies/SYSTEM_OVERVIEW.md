# Zombie System

> **Note**: This document covers the entire zombie system across Client/Server/Shared projects.
> Components and events defined here in `Content.Shared/Zombies/` form the data contract,
> while behavior is implemented in `Content.Server/Zombies/` and `Content.Client/Zombies/`.

## Overview
Handles zombie transformation, infection mechanics, and zombie behavior for infected entities.

## Project Structure

### Content.Shared/Zombies/
Contains components and events that define the zombie system's data contract:
- `ZombieComponent` - Main zombie state
- `ZombieTransformationConfigComponent` - Configuration (24 settings)
- `PendingZombieComponent`, `ZombifyOnDeathComponent`, etc. - State markers
- `ZombificationRequestedEvent`, `EntityZombifiedEvent` - System events
- `SharedZombieSystem` - Base system class

### Content.Server/Zombies/
Contains server-side logic and transformation implementation:
- `ZombieSystem` - Infection, healing, emotes, cloning
- `ZombieTransformationSystem` - Transformation coordinator (5 partial classes)
  - `Appearance.cs` - Visual changes
  - `Physiology.cs` - Biology changes
  - `Combat.cs` - Combat abilities
  - `Mind.cs` - Role/mind management
  - `NPC.cs` - AI configuration
- `ZombieAccentOverrideComponent` - Custom accent support (server-only)
- `NonSpreaderZombieComponent` - Non-infectious marker (server-only)

### Content.Client/Zombies/
Contains client-side visual updates:
- `ZombieSystem` - Status icons and sprite coloring

## Core Systems

### ZombieSystem
Manages zombie behavior and infection mechanics:
- **Infection**: Bite attacks spread zombie infection to victims
- **Passive healing**: Living zombies gradually regenerate health
- **Infection progression**: Infected entities take damage over time until death
- **Emotes**: Zombies groan and scream automatically
- **Cloning**: Restores original appearance when zombies are cloned

### ZombieTransformationSystem
Handles the actual transformation process when an entity becomes a zombie:
- **Appearance**: Changes skin/eye color, updates identity
- **Physiology**: Removes hunger/thirst/breathing, changes blood, grants temperature immunity
- **Combat**: Adds door prying, melee attacks, removes hands
- **Mind**: Assigns zombie antagonist role, sends welcome message
- **AI**: Configures hostile NPC behavior for mindless zombies

## How Zombification Works

### Triggers
1. **Bite from zombie** → victim becomes infected
2. **Death while infected** → instant zombification
3. **Incapacitated by zombie** → instant zombification
4. **Admin command** → instant zombification
5. **Zombify-self action** → player-triggered (incurable zombie trait)

### Infection Process
1. Zombie bites victim (chance based on armor)
2. Victim gains `PendingZombieComponent` (infected)
3. After grace period, victim takes damage over time
4. On death, victim zombifies instantly
5. New zombie can spread infection to others

### Transformation
When zombification triggers:
1. Check immunity (`ZombieImmuneComponent`) and role bans
2. Allow cancellation via `ZombificationRequestedEvent`
3. Transform appearance (zombie skin/eyes)
4. Modify physiology (no hunger, zombie blood, etc.)
5. Setup combat abilities (door prying, melee)
6. Assign mind role (if player) or setup AI (if NPC)
7. Add to zombie faction, make hostile to crew

## Components

### ZombieComponent
Main zombie marker with behavior configuration:
- Infection chances and armor effectiveness
- Passive healing rates
- Appearance settings (colors, layers)
- Pre-zombification state (for unzombify)

### ZombieTransformationConfigComponent
Configuration for transformation behavior (24 settings):
- Faction, mind role, AI task, accent
- Blood reagent, damage modifiers
- Inventory slots to remove
- Combat settings (melee range, prying)
- Flags (heal on transform, make sentient, etc.)

### PendingZombieComponent
Marks entity as infected (not yet zombified):
- Grace period before damage starts
- Damage rate (increases when critical)
- Infection warning messages

### ZombifyOnDeathComponent
Auto-zombifies entity on death.

### IncurableZombieComponent
For initial zombie spawns - grants zombify-self action.

### ZombieImmuneComponent
Prevents zombification entirely.

### NonSpreaderZombieComponent
Zombie that cannot infect others via bites.

### ZombieAccentOverrideComponent
Overrides default zombie speech accent (e.g., moths get "zombieMoth").

## Events

### ZombificationRequestedEvent
Raised before zombification begins. Can be cancelled by external systems.

### EntityZombifiedEvent
Raised after zombification completes. Used by game modes to track infections.

## Configuration

All transformation behavior is data-driven via `ZombieTransformationConfigComponent`:
- **Faction**: Which faction zombie joins (default: "Zombie")
- **MindRole**: Role assigned to players (default: "MindRoleZombie")
- **Accent**: Speech accent (default: "zombie", can override)
- **BloodReagent**: Blood type (default: "ZombieBlood")
- **RemoveSlots**: Inventory to unequip (default: gloves, ears)
- **HealOnTransformation**: Fully heal when transformed (default: true)
- **MakeSentient**: Grant sentience if not sentient (default: true)
- **And 17 more configurable values...**

## Integration

### Zombify an Entity
```csharp
[Dependency] private readonly ZombieTransformationSystem _zombieTransformation = default!;

// Use default config
_zombieTransformation.TryZombifyEntity(targetUid);

// Use custom config
_zombieTransformation.TryZombifyEntity(targetUid, configEntityUid);
```

### Cancel Zombification
```csharp
SubscribeLocalEvent<YourComponent, ZombificationRequestedEvent>(OnZombificationRequested);

private void OnZombificationRequested(EntityUid uid, YourComponent comp, ZombificationRequestedEvent args)
{
    if (ShouldPreventZombification())
    {
        args.Cancel();
        args.CancellationReason = "Reason here";
    }
}
```

### React to Zombification
```csharp
SubscribeLocalEvent<YourComponent, EntityZombifiedEvent>(OnEntityZombified);

private void OnEntityZombified(EntityUid uid, YourComponent comp, ref EntityZombifiedEvent args)
{
    // React to args.Target becoming a zombie
}
```

## Special Cases

### Player Zombies
- Retain control of character
- Assigned zombie antagonist role
- Receive welcome message and sound
- Can pry doors, attack crew
- Cannot use complex interactions

### NPC Zombies
- Hostile AI (HTN: SimpleHostileCompound)
- Target crew members
- Pathfind and chase targets
- Attack on contact

### Ghost Role Takeover
Zombies without player control become ghost roles:
- Ghosts can take over zombie bodies
- Automatically assigned zombie role
- Inherits zombie abilities and faction

### Role Bans
Players banned from zombie role are automatically ghosted on zombification and shown a message.

### Cloning Zombies
Cloning a zombie:
- Restores original skin/eye color
- Restores blood reagent
- Clone is NOT a zombie
- Original body remains zombie

## Armor and Protection

Infection chance reduced by armor:
- Slash resistance: 50% effective
- Pierce resistance: 30% effective
- Blunt resistance: 10% effective
- Special zombification resistance (equipment can grant)
- Minimum 5% infection chance (can't fully prevent)

Protective slots: feet, head, eyes, gloves, mask, neck, inner clothing, outer clothing

## Typical Zombie Stats
- **Movement speed**: 70% of normal
- **Passive healing**: 0.4 blunt, 0.2 slash/pierce, 0.02 heat/shock per second
- **Critical healing**: 2x multiplier
- **Healing on bite**: 2 damage healed across blunt/slash/pierce
- **Door prying**: 75% speed, can pry powered doors
- **Melee range**: 1.2 tiles (extended reach)
- **Temperature**: Immune to cold damage
- **Blood loss**: Cannot die from blood loss
