# In Vacuo - Development Guide for AI Assistants

## Project Overview

**In Vacuo** is a Space Station 14 fork being transformed into an asymmetric spaceship combat game focused on:
- Beyond Visual Range (BVR) missile combat
- Electronic warfare and intelligence gathering
- Semi-realistic radar and sensor systems
- Logistics and resource management
- Combined arms (ship combat + infantry/boarding)

**Base Codebase**: Space Station 14 (SS14)
- Engine: Robust Toolbox (custom C# game engine)
- Architecture: Entity Component System (ECS)
- Language: C# (.NET 9)
- Networking: Client-Server model
- Data: YAML-based prototypes

---

## Critical Development Conventions

### 1. Code Organization: `_vacuo/` Directory Rule

**ALL In Vacuo-specific code MUST go in `_vacuo/` subdirectories:**

```
 CORRECT:
Content.Shared/_vacuo/Sensors/Components/RadarSignatureComponent.cs
Content.Server/_vacuo/Missiles/Systems/MissileGuidanceSystem.cs
Resources/Prototypes/_vacuo/Entities/Ships/combat_ships.yml
Resources/Maps/_vacuo/arena_open.yml

L INCORRECT:
Content.Shared/Sensors/Components/RadarSignatureComponent.cs  (missing _vacuo)
Content.Server/Combat/NewCombatSystem.cs                      (missing _vacuo)
```

**Why?** This keeps In Vacuo code cleanly separated from base SS14 systems, making it:
- Easy to identify custom code
- Simple to merge upstream SS14 updates
- Clear what can be modified vs what's base SS14
- Possible to extract as a standalone module later

### 2. Modifying Existing Files

When you need to modify existing SS14 files:

**Minor Changes (< 10 lines):**
Mark with inline comments:
```csharp
// Vacuo - Add missile guidance support
if (TryComp<MissileGuidanceComponent>(uid, out var guidance))
{
    ApplyGuidance(uid, guidance);
}
```

**Major Changes (functions/significant logic):**
Create a `.vacuo.md` file in the same directory:
```
Content.Server/Weapons/Ranged/Systems/GunSystem.cs
Content.Server/Weapons/Ranged/Systems/GunSystem.vacuo.md  � Documents changes
```

Example `GunSystem.vacuo.md`:
```markdown
# Vacuo Modifications to GunSystem

## Changes Made:
- Added fire control integration in `AttemptShoot()` method
- Added missile launcher support in `TryShoot()` method
- Added guided projectile handling

## Modified Methods:
- `AttemptShoot()` - Lines 234-256
- `TryShoot()` - Lines 412-445

## Reason:
Required to support fire control systems and missile launchers
```

### 3. Never Remove Base SS14 Systems

**DON'T:**
- Delete existing SS14 systems (chemistry, botany, medical, etc.)
- Remove job definitions
- Delete prototype files
- Remove maps

**WHY:**
- Causes cascading dependency errors
- Breaks existing maps
- Creates YAML inheritance issues
- Makes merging upstream updates impossible

**DO:**
- Build new systems alongside existing ones
- Create combat-specific maps in `Resources/Maps/_vacuo/`
- Use game modes to toggle between RP and combat
- Optionally disable via configuration (post-MVP)

---

## ECS Architecture Quick Reference

### Component Pattern
```csharp
// Components are pure data containers
[RegisterComponent, NetworkedComponent]  // Register + auto-sync to client
public sealed partial class MyComponent : Component
{
    [DataField]  // Serializable from YAML
    public float SomeValue = 1.0f;

    [DataField, AutoNetworkedField]  // Auto-sync specific field
    public int NetworkedValue = 0;
}
```

### System Pattern
```csharp
// Systems contain logic, operate on components
public sealed class MySystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        // Subscribe to events
        SubscribeLocalEvent<MyComponent, ComponentStartup>(OnStartup);
    }

    public override void Update(float frameTime)
    {
        // Per-frame logic
        var query = EntityQueryEnumerator<MyComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            // Process entities with both components
        }
    }
}
```

### Prototype Pattern (YAML)
```yaml
- type: entity
  id: MyEntity
  parent: BaseEntity  # Inheritance supported
  components:
  - type: MyComponent
    someValue: 2.0
  - type: Transform
  - type: Sprite
    sprite: path/to/sprite.rsi
```

---

## Key SS14 Systems to Leverage

### Already Excellent (Use As-Is):

**1. ShuttleSystem** - Ship physics and movement
- File: `Content.Server/Shuttles/Systems/ShuttleSystem.cs`
- Don't modify - works perfectly for space combat
- Add `RadarSignatureComponent` to shuttles instead

**2. GunSystem** - Weapon firing
- File: `Content.Server/Weapons/Ranged/Systems/GunSystem.cs`
- Supports projectiles, hitscan, energy weapons
- Extend for missiles with `// Vacuo - ` markers

**3. DeviceNetworkSystem** - Wireless communication
- File: `Content.Shared/DeviceNetwork/`
- Perfect for ship-to-ship datalinks, ECM
- Use frequency-based channels for fleet coordination

**4. ProjectileComponent** - Bullets/missiles
- File: `Content.Shared/Projectiles/ProjectileComponent.cs`
- Add `MissileGuidanceComponent` separately, don't modify base

**5. DamageableSystem** - Damage handling
- File: `Content.Shared/Damage/Systems/DamageableSystem.cs`
- Comprehensive damage types already exist
- No changes needed

**6. PowerNetSystem** - Power distribution
- File: `Content.Server/Power/EntitySystems/PowerNetSystem.cs`
- Use for radar/weapon power requirements
- Add power consumption to new components

**7. RadarConsoleSystem** - Basic radar
- File: `Content.Server/Shuttles/Systems/RadarConsoleSystem.cs`
- Good foundation, create `AdvancedRadarConsoleComponent` instead of modifying

---

## Common Patterns

### Adding a New Combat Feature

**1. Create Component** (Shared):
```csharp
// Content.Shared/_vacuo/FeatureName/Components/FeatureComponent.cs
namespace Content.Shared._vacuo.FeatureName.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FeatureComponent : Component
{
    [DataField]
    public float SomeValue = 1.0f;
}
```

**2. Create System** (Server):
```csharp
// Content.Server/_vacuo/FeatureName/Systems/FeatureSystem.cs
namespace Content.Server._vacuo.FeatureName.Systems;

public sealed class FeatureSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<FeatureComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, FeatureComponent component, ComponentStartup args)
    {
        // Initialization logic
    }
}
```

**3. Create Prototype** (YAML):
```yaml
# Resources/Prototypes/_vacuo/Entities/feature_entities.yml
- type: entity
  id: MyFeatureEntity
  parent: BaseItem
  components:
  - type: Feature
    someValue: 2.0
```

### Networking Best Practices

**Auto-sync fields:**
```csharp
[DataField, AutoNetworkedField]
public float Value = 1.0f;  // Automatically syncs to clients
```

**Manual sync (for complex data):**
```csharp
// Raise event, handle on client
RaiseNetworkEvent(new MyDataUpdatedEvent(uid, data));
```

**Client prediction:**
```csharp
// Shared system for client prediction
public abstract class SharedMySystem : EntitySystem { }
public sealed class MySystem : SharedMySystem { }  // Server
public sealed class MySystem : SharedMySystem { }  // Client
```

---

## Prototype Inheritance

Prototypes use YAML inheritance:
```yaml
# Base definition
- type: entity
  id: BaseMissile
  abstract: true
  components:
  - type: Projectile
    damage:
      types:
        Explosive: 100

# Inherit and override
- type: entity
  id: HeavyMissile
  parent: BaseMissile
  components:
  - type: Projectile
    damage:
      types:
        Explosive: 200  # Override damage
```

**Multiple inheritance:**
```yaml
- type: entity
  id: CombatShip
  parent: [BaseShuttle, BaseCombatEntity, BaseWeaponPlatform]
```

---

## Performance Considerations

### Entity Queries (Efficient):
```csharp
// Good - iterator pattern
var query = EntityQueryEnumerator<MyComponent, TransformComponent>();
while (query.MoveNext(out var uid, out var comp, out var xform))
{
    // Process efficiently
}
```

### Avoid Every Frame:
```csharp
// Bad - don't query all entities every frame if not needed
public override void Update(float frameTime)
{
    var all = EntityQuery<MyComponent>();  // Expensive!
}

// Good - use timers for infrequent updates
private float _updateTimer = 0f;
public override void Update(float frameTime)
{
    _updateTimer += frameTime;
    if (_updateTimer < 1.0f) return;  // Update once per second
    _updateTimer = 0f;
    // Do expensive work
}
```

---

## Testing

### Run the game:
```bash
dotnet run --project Content.Server
```

### Build only:
```bash
dotnet build
```

### Run specific map:
```bash
dotnet run --project Content.Server --cvar game.map=_vacuo/arena_open
```

---

## Common Pitfalls

### 1. Prototype ID Conflicts
```yaml
# Bad - overwrites base SS14 entity
- type: entity
  id: BaseShuttle  # DON'T reuse existing IDs

# Good - unique ID with Vacuo prefix
- type: entity
  id: VacuoDestroyerShip
```

### 2. Missing Component Registration
```csharp
// Component won't be found without [RegisterComponent]
[RegisterComponent]  // � REQUIRED
public sealed partial class MyComponent : Component { }
```

### 3. Networking Issues
```csharp
// Bad - not synced to client
public float ImportantValue = 1.0f;

// Good - synced
[DataField, AutoNetworkedField]
public float ImportantValue = 1.0f;
```

### 4. Prototype Paths
```yaml
# Bad - wrong path
sprite: Objects/Weapons/missile.rsi

# Good - correct path from Resources/Textures
sprite: Objects/Weapons/Missiles/missile.rsi
```

---

## Quick Reference: Key Locations

### Existing Systems to Study:
- **Shuttles**: `Content.Server/Shuttles/Systems/`
- **Weapons**: `Content.Server/Weapons/Ranged/Systems/GunSystem.cs`
- **Radar**: `Content.Server/Shuttles/Systems/RadarConsoleSystem.cs`
- **Turrets**: `Content.Server/Turrets/`
- **DeviceNetwork**: `Content.Shared/DeviceNetwork/`
- **Damage**: `Content.Shared/Damage/`
- **Power**: `Content.Server/Power/`

### In Vacuo Code:
- **All custom code**: `Content.*/_vacuo/`
- **Prototypes**: `Resources/Prototypes/_vacuo/`
- **Maps**: `Resources/Maps/_vacuo/`
- **Textures**: `Resources/Textures/_vacuo/`

### Documentation:
- **Roadmap**: `vacuo_roadmap.md`
- **This file**: `CLAUDE.md`
- **Change docs**: `*.vacuo.md` files alongside modified files
- **System docs**: `SYSTEM_OVERVIEW.md` files for complex systems

---

## Documentation Standards

### System Documentation Hierarchy

When working with or creating systems, documentation should exist at the **highest applicable level**:

**Priority Order** (check in this order):
1. **Content.Server/SystemName/** - Preferred location if server-side logic exists
2. **Content.Shared/SystemName/** - If system is primarily shared logic
3. **Content.Client/SystemName/** - Only if purely client-side

**Files to Create:**

**SYSTEM_OVERVIEW.md** (for complex systems):
- Comprehensive system documentation
- Architecture overview
- Component descriptions
- Integration examples
- Use cases and examples
- Place in directory with primary system logic

**SystemName.vacuo.md** (for modified systems):
- Documents In Vacuo changes to base SS14 systems
- What was changed and why
- Migration guide (old API → new API)
- Files modified/created/deleted
- Place alongside the modified file

**Example Structure:**
```
Content.Server/Zombies/
├── ZombieSystem.cs
├── ZombieTransformationSystem.cs
├── SYSTEM_OVERVIEW.md          ← System documentation
└── ZombieSystem.vacuo.md       ← Vacuo modification notes

Content.Server/_vacuo/Sensors/
├── Systems/AdvancedRadarSystem.cs
└── SYSTEM_OVERVIEW.md          ← New system documentation
```

### When to Document

**Create SYSTEM_OVERVIEW.md when:**
- System has 3+ components or 2+ systems
- Complex interactions between components
- External systems need integration guide
- Non-obvious behavior or edge cases
- You find yourself explaining it more than once

**Create SystemName.vacuo.md when:**
- Modifying existing SS14 system significantly (>20 lines)
- Changing system architecture
- Adding new API methods to base systems
- Refactoring existing code

**Document Undocumented Systems:**
If you encounter an undocumented system while working:
1. Create `SYSTEM_OVERVIEW.md` at appropriate level (Server > Shared > Client)
2. Document architecture, components, and usage
3. Add integration examples
4. Note any quirks or important patterns

**Example: Zombie System**
- `Content.Server/Zombies/SYSTEM_OVERVIEW.md` - Complete system docs
- `Content.Server/Zombies/ZombieSystem.vacuo.md` - Refactoring notes

---

## Development Workflow

### When Adding New Feature:

1. **Plan** - Define components needed
2. **Create Components** in `Content.Shared/_vacuo/FeatureName/Components/`
3. **Create Systems** in `Content.Server/_vacuo/FeatureName/Systems/`
4. **Create UI** (if needed) in `Content.Client/_vacuo/FeatureName/UI/`
5. **Create Prototypes** in `Resources/Prototypes/_vacuo/FeatureName/`
6. **Test** - Build and run
7. **Document** - Update relevant `.vacuo.md` if modifying base systems

### When Modifying Existing System:

1. **Document First** - Create `SystemName.vacuo.md` if major change
2. **Mark Changes** - Use `// Vacuo - ` comments
3. **Extend, Don't Replace** - Add optional behavior, keep existing functionality
4. **Test Compatibility** - Ensure base SS14 features still work
5. **Update CLAUDE.md** - If you discover important patterns

---

## Integration Points

### How Combat Systems Connect to SS14:

**Radar Signatures** � `ShuttleComponent`
- Ships automatically detectable by combat radar
- Add `RadarSignatureComponent` to any shuttle

**Missiles** � `ProjectileComponent`
- Missiles ARE projectiles with added guidance
- Use existing damage/collision systems

**Fire Control** � `GunSystem`
- Weapons can be manually OR fire-control operated
- Fire control queries `GunComponent` entities

**Power** � `PowerNetSystem`
- Radar/sensors consume power via `ApcPowerReceiverComponent`
- Combat systems respect power state

**Datalink** � `DeviceNetworkSystem`
- Fleet coordination uses existing wireless network
- Combat channels separate from station channels

**Maps** � Game Modes
- Combat maps in `Resources/Maps/_vacuo/`
- Combat game mode loads combat maps
- Station maps unaffected

---

## Namespace Conventions

```csharp
// In Vacuo namespaces use _vacuo
namespace Content.Shared._vacuo.Sensors.Components;
namespace Content.Server._vacuo.Combat.Systems;
namespace Content.Client._vacuo.FireControl.UI;

// Base SS14 namespaces (don't create new ones here)
namespace Content.Shared.Shuttles;  // Existing
namespace Content.Server.Weapons;   // Existing
```

---

## Git Workflow

### Branching:
- Main development: `master` branch
- Feature branches: `feature/missile-guidance`, `feature/ecm-system`

### Commits:
- Keep In Vacuo changes in separate commits when possible
- Prefix: `[Vacuo]` for combat features, `[SS14]` for base modifications
- Example: `[Vacuo] Add radar signature component`

### Merging Upstream:
Since all code is in `_vacuo/`, merging SS14 updates is straightforward:
```bash
git remote add upstream https://github.com/space-wizards/space-station-14
git fetch upstream
git merge upstream/master
# Conflicts unlikely in _vacuo/ directories
```

---

## Quick Start Checklist

When starting work on In Vacuo:

- [ ] Read this file (CLAUDE.md)
- [ ] Read `vacuo_roadmap.md` for current goals
- [ ] Check current phase/week in roadmap
- [ ] All new files go in `_vacuo/` directories
- [ ] Mark changes to existing files with `// Vacuo -` comments
- [ ] Test build: `dotnet build`
- [ ] Test run: `dotnet run --project Content.Server`

---

## Helpful Resources

**SS14 ECS Patterns:**
- Component registration: `[RegisterComponent]`
- Networking: `[NetworkedComponent]` + `[AutoNetworkedField]`
- Events: `SubscribeLocalEvent<TComp, TEvent>(Handler)`
- Queries: `EntityQueryEnumerator<TComp1, TComp2>()`

**YAML Prototype Syntax:**
- Entity definition: `- type: entity`
- Component: `- type: ComponentName`
- Data fields: Use camelCase for field names
- Inheritance: `parent: BasePrototype` or `parent: [Multi, Ple]`

**File Structure:**
- Components: `Components/NameComponent.cs`
- Systems: `Systems/NameSystem.cs`
- Events: `Events/NameEvents.cs`
- Shared code: Use `partial class` and `sealed` modifier

---

## Current Development Focus

**See**: `vacuo_roadmap.md` for detailed timeline

**Current Phase**: Foundation (Weeks 1-2)
**Next Milestone**: Radar signature system functional

**Immediate Tasks**:
1. Create `Content.Shared/_vacuo/Sensors/Components/RadarSignatureComponent.cs`
2. Create `Content.Server/_vacuo/Sensors/Systems/AdvancedRadarSystem.cs`
3. Add signature detection to existing radar console
4. Create test ship prototypes with signatures

---

## Important Reminders

1. **`_vacuo/` for ALL custom code** - This is the most important rule
2. **Don't delete SS14 systems** - Add alongside, don't remove
3. **Document base system changes** - Use comments or `.vacuo.md` files
4. **Test frequently** - Build after every component/system
5. **Keep it modular** - Combat systems should be independent
6. **Leverage existing** - Don't reinvent shuttle movement, damage, etc.

---

## Questions & Troubleshooting

**Q: Where does X go?**
A: If it's In Vacuo-specific, it goes in a `_vacuo/` directory.

**Q: Can I modify existing SS14 files?**
A: Yes, but mark changes with `// Vacuo -` comments and document in `.vacuo.md` for major changes.

**Q: Should I remove chemistry/botany/etc.?**
A: No. Build combat systems alongside them. Optionally disable later via config.

**Q: How do I add a new component?**
A: Create in `Content.Shared/_vacuo/Category/Components/`, register with `[RegisterComponent]`, create corresponding system.

**Q: Build errors after adding component?**
A: Ensure `[RegisterComponent]` attribute is present and file is in correct namespace.

**Q: Prototype not found?**
A: Check YAML syntax, ensure prototype ID is unique, verify file path is correct.

---

## Summary

**In Vacuo = SS14 + Modular Combat Systems**

- All custom code in `_vacuo/` directories
- Build features additively, don't remove base systems
- Mark base system modifications clearly
- Follow ECS patterns (Components = data, Systems = logic)
- Test early and often
- Document non-obvious changes

Keep this file updated as development progresses and patterns emerge.
