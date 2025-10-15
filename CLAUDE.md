# In Vacuo - Development Guide

> **For AI Assistants**: This guide is optimized for LLM consumption. Critical rules first, details later.

---

## CRITICAL RULES (Read These First)

### 🔴 Rule 1: ALL Custom Code in `_vacuo/` Directories

```
✅ Content.Shared/_vacuo/Sensors/Components/RadarSignatureComponent.cs
✅ Resources/Prototypes/_vacuo/Entities/Ships/combat_ships.yml
❌ Content.Shared/Sensors/Components/RadarSignatureComponent.cs  (WRONG - missing _vacuo)
```

**Why**: Isolates In Vacuo code from base SS14, enables clean upstream merges, prevents conflicts.

### 🔴 Rule 2: NEVER Delete Base SS14 Systems

**DON'T**: Remove chemistry, botany, jobs, prototypes, maps from base SS14
**WHY**: Cascading dependency errors, broken maps, YAML inheritance failures, impossible merges
**DO**: Build alongside existing systems, create combat maps in `_vacuo/`, use game modes to toggle

### 🔴 Rule 3: Test After EVERY Change

```bash
# After each component/system:
dotnet build                          # Must pass
dotnet run --project Content.Server   # Must run without fatal errors
```

**Why**: Prevents accumulating untested changes that fail catastrophically (lesson learned from failed janitorial removal)

### 🔴 Rule 4: Mark Base System Changes

**Minor (<10 lines)**: `// Vacuo - description`
**Major (>20 lines)**: Create `SystemName.vacuo.md` in same directory
**Complete rewrite**: Also create `SYSTEM_OVERVIEW.md`

### 🔴 Rule 5: No Timelines/Deadlines

`vacuo_roadmap.md` phases are **organizational only**, not deadlines. This is a passion project - quality > speed.

---

## Project Context

**In Vacuo** = Space Station 14 fork → Asymmetric spaceship combat game

**Focus**: BVR missile combat, electronic warfare, radar/sensors, logistics, combined arms

**Approach**: Additive development - build combat systems ON TOP of SS14, don't remove existing

**Tech Stack**: C# (.NET 9), ECS architecture, Robust Toolbox engine, YAML prototypes

---

## Quick Start (Get Productive Fast)

### Implementing a New Feature

**Order** (always follow this):
1. Define "done" criteria (testable checklist)
2. Write prototype YAML (desired end state)
3. Create minimal component → build → test
4. Add system logic incrementally → build after each change
5. Test in-game continuously
6. Document when working

**File Structure**:
```
Content.Shared/_vacuo/FeatureName/
├── Components/FeatureComponent.cs
└── Events/FeatureEvents.cs

Content.Server/_vacuo/FeatureName/
├── Systems/FeatureSystem.cs
└── SYSTEM_OVERVIEW.md (if complex)

Resources/Prototypes/_vacuo/FeatureName/
└── feature_entities.yml
```

### Modifying Existing System

1. Check if `SystemName.vacuo.md` exists → read it
2. Create `.vacuo.md` if making major changes (>20 lines)
3. Mark changes: `// Vacuo - description`
4. Test compatibility with base SS14
5. Update `.vacuo.md` with changes

---

## ECS Essentials

**Component** (data):
```csharp
[RegisterComponent, NetworkedComponent]  // Required attributes
public sealed partial class MyComponent : Component  // Required modifiers
{
    [DataField] public float Value = 1.0f;              // Serializable
    [DataField, AutoNetworkedField] public int Synced;  // Auto-sync to client
}
```

**System** (logic):
```csharp
public sealed class MySystem : EntitySystem
{
    [Dependency] private readonly OtherSystem _other = default!;
    public override void Initialize() => SubscribeLocalEvent<MyComponent, SomeEvent>(OnEvent);
    public override void Update(float frameTime)
    {
        var q = EntityQueryEnumerator<MyComponent, TransformComponent>();
        while (q.MoveNext(out var uid, out var comp, out var xform)) { }
    }
}
```

**Prototype** (YAML):
```yaml
- type: entity
  id: MyEntity
  parent: BaseEntity
  components:
  - type: MyComponent
    value: 2.0
```

---

## Key Systems to Leverage (Don't Reinvent)

| System | Location | Use For | Modify? |
|--------|----------|---------|---------|
| **ShuttleSystem** | `Content.Server/Shuttles/Systems/` | Ship physics/movement | ❌ No - perfect as-is |
| **GunSystem** | `Content.Server/Weapons/Ranged/Systems/` | Weapon firing | ✅ Extend with `// Vacuo -` |
| **ProjectileComponent** | `Content.Shared/Projectiles/` | Missiles (add guidance) | ✅ Add companion component |
| **DeviceNetworkSystem** | `Content.Shared/DeviceNetwork/` | Ship datalinks/ECM | ❌ Use as-is |
| **RadarConsoleSystem** | `Content.Server/Shuttles/Systems/` | Basic radar | ✅ Create Advanced version |
| **DamageableSystem** | `Content.Shared/Damage/Systems/` | Combat damage | ❌ No changes needed |
| **PowerNetSystem** | `Content.Server/Power/EntitySystems/` | Power consumption | ✅ Add consumption tracking |

---

## Common Patterns

### Adding New Component
```csharp
// 1. Component (Shared)
[RegisterComponent, NetworkedComponent]
public sealed partial class RadarSignatureComponent : Component
{
    [DataField] public float RadarCrossSection = 100f;
}

// 2. System (Server)
public sealed class RadarSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var q = EntityQueryEnumerator<RadarSignatureComponent, TransformComponent>();
        while (q.MoveNext(out var uid, out var sig, out var xform)) { /* Detect */ }
    }
}

// 3. Prototype (YAML)
- type: entity
  id: VacuoDestroyer
  components:
  - type: RadarSignature
    radarCrossSection: 500
```

### Networking
- **Auto-sync field**: `[DataField, AutoNetworkedField] public float Value;`
- **Manual event**: `RaiseNetworkEvent(new MyEvent());`
- **Prediction**: Shared base class used by Client/Server

### Performance
- **Efficient query**: `EntityQueryEnumerator<Comp1, Comp2>()`
- **Throttle updates**: Use timer pattern (see full example in Performance section below)

---

## Documentation Standards

### File Placement Hierarchy

**Priority**: Server > Shared > Client (place at highest applicable level)

**SYSTEM_OVERVIEW.md** - Complex systems (3+ components):
- Place in `Content.Server/SystemName/` if server logic exists
- Otherwise `Content.Shared/SystemName/`
- Otherwise `Content.Client/SystemName/`
- Contains: architecture, components, integration examples

**SystemName.vacuo.md** - Modified base systems (>20 line changes):
- Place alongside the modified file
- Contains: what changed, why, migration guide, file list

**Example**:
```
Content.Server/Zombies/
├── ZombieSystem.cs
├── ZombieTransformationSystem.cs
├── SYSTEM_OVERVIEW.md          ← System docs
└── ZombieSystem.vacuo.md       ← Refactor notes

Content.Server/_vacuo/Sensors/
├── AdvancedRadarSystem.cs
└── SYSTEM_OVERVIEW.md          ← New system docs
```

### When to Document

**SYSTEM_OVERVIEW.md**: 3+ components, complex interactions, external integration needed
**SystemName.vacuo.md**: >20 line modifications, architecture changes, refactors
**Inline comments**: <10 line changes (`// Vacuo - description`)

**Document undocumented systems** as you encounter them - create SYSTEM_OVERVIEW.md

---

## Integration Points

Combat systems hook into SS14 via:
- **Radar** → Add `RadarSignatureComponent` to shuttles
- **Missiles** → Extend `ProjectileComponent` with `MissileGuidanceComponent`
- **Fire Control** → Query `GunComponent` entities, add targeting logic
- **Power** → Use `ApcPowerReceiverComponent` for consumption
- **Datalink** → Use `DeviceNetworkSystem` for fleet coordination
- **Maps** → Combat maps in `Resources/Maps/_vacuo/`

---

## Development Best Practices

### Incremental Development (Critical!)

```
❌ Create 10 components → build → discover issues
✅ Create 1 component → build → test → next component
```

**Example**:
1. `RadarSignatureComponent` (20 lines) → build → verify
2. Add to ONE ship prototype → spawn in-game → verify works
3. Detection logic (50 lines) → build → test detection
4. Expand to more ships → iterate

**Why**: Failed janitorial removal taught us this - test continuously or fail catastrophically

### Define "Done" Before Implementing

```markdown
## Feature X - Definition of Done
- [ ] Compiles without errors
- [ ] Works in prototype YAML
- [ ] Tested in-game visually
- [ ] No performance issues (50+ entities)
- [ ] Documented in SYSTEM_OVERVIEW.md
```

Prevents scope creep, provides clear completion target.

### Prototype-First Design

1. Write desired YAML first (what should the API look like?)
2. Create minimal component to support it
3. Test prototype spawns
4. Add system logic
5. Iterate

Validates design before heavy implementation.

---

## Quick Reference

### Common Pitfalls
- **ID conflicts**: Use unique IDs (`VacuoDestroyer`, not `BaseShuttle`)
- **Missing `[RegisterComponent]`**: Component won't be found
- **Networking**: Use `[AutoNetworkedField]` for sync
- **Paths**: Sprites relative to `Resources/Textures/`

### Cheat Sheet
- **Attributes**: `[RegisterComponent]`, `[NetworkedComponent]`, `[DataField]`, `[AutoNetworkedField]`
- **Events**: `SubscribeLocalEvent<TComp, TEvent>(Handler)`
- **Queries**: `EntityQueryEnumerator<Comp1, Comp2>()`
- **YAML**: `- type: entity`, `parent: Base` or `parent: [Multi, Ple]`
- **Modifiers**: Always `sealed partial class`

### Testing Commands
```bash
dotnet build                                    # Compile
dotnet run --project Content.Server             # Run
dotnet run --project Content.Server --cvar game.map=_vacuo/arena  # Specific map
```

---

## Detailed Reference

### Prototype Inheritance
```yaml
- type: entity
  id: BaseMissile
  abstract: true
  components:
  - type: Projectile
    damage: {types: {Explosive: 100}}

- type: entity
  id: HeavyMissile
  parent: BaseMissile  # Inherits from above
  components:
  - type: Projectile
    damage: {types: {Explosive: 200}}  # Override

- type: entity
  id: Ship
  parent: [BaseShuttle, BaseCombat]  # Multiple inheritance
```

### Performance Patterns

**Efficient queries**:
```csharp
var q = EntityQueryEnumerator<MyComponent, TransformComponent>();
while (q.MoveNext(out var uid, out var comp, out var xform)) { }
```

**Throttle expensive work**:
```csharp
private float _timer = 0f;
public override void Update(float frameTime)
{
    _timer += frameTime;
    if (_timer < 1.0f) return;  // Once per second
    _timer = 0f;
    // Expensive operations here
}
```

### Namespace Conventions
```csharp
namespace Content.Shared._vacuo.Sensors.Components;  // In Vacuo code
namespace Content.Shared.Shuttles;                   // Base SS14 (existing)
```

### Git Workflow
```bash
git remote add upstream https://github.com/space-wizards/space-station-14
git fetch upstream
git merge upstream/master  # Conflicts unlikely in _vacuo/
```

**Commit prefixes**: `[Vacuo]` for features, `[SS14]` for base modifications

---

## Key Locations

### In Vacuo Code
- Custom code: `Content.*/_vacuo/`
- Prototypes: `Resources/Prototypes/_vacuo/`
- Maps: `Resources/Maps/_vacuo/`
- Textures: `Resources/Textures/_vacuo/`

### Base SS14 Systems (Study These)
- Shuttles: `Content.Server/Shuttles/Systems/`
- Weapons: `Content.Server/Weapons/Ranged/Systems/GunSystem.cs`
- Radar: `Content.Server/Shuttles/Systems/RadarConsoleSystem.cs`
- Turrets: `Content.Server/Turrets/`
- DeviceNetwork: `Content.Shared/DeviceNetwork/`
- Damage: `Content.Shared/Damage/`
- Power: `Content.Server/Power/`

### Documentation
- **This file**: `CLAUDE.md` (start here)
- **Roadmap**: `vacuo_roadmap.md` (feature plans)
- **System docs**: `*/SYSTEM_OVERVIEW.md` (per-system)
- **Modification docs**: `*.vacuo.md` (changes to base SS14)

---

## Questions & Answers

**Q: Where does new code go?**
A: In `_vacuo/` subdirectory of appropriate project (Shared/Server/Client)

**Q: Can I modify existing SS14 files?**
A: Yes - mark with `// Vacuo -` comments. If >20 lines, create `.vacuo.md` documenting changes

**Q: Can I remove SS14 systems?**
A: NO - causes cascading failures. Build alongside, disable via config later if needed

**Q: Where do prototypes go?**
A: `Resources/Prototypes/_vacuo/Category/`

**Q: Where do maps go?**
A: `Resources/Maps/_vacuo/`

**Q: How do I add a component?**
A: Create in `Content.Shared/_vacuo/Cat/Components/`, add `[RegisterComponent]`, create system

**Q: Build errors after adding component?**
A: Check `[RegisterComponent]` attribute, namespace, file location

**Q: Should I follow roadmap timelines?**
A: NO - timelines are organizational only, not deadlines. Work at sustainable pace.

**Q: Where should documentation go?**
A: Server > Shared > Client priority. `SYSTEM_OVERVIEW.md` for complex systems, `.vacuo.md` for modifications

---

## Development Workflow Summary

### Adding New Feature
1. Define "done" criteria (testable checklist)
2. Write prototype YAML first
3. Create component (minimal) → `dotnet build` → verify
4. Add to ONE test entity → spawn in-game → verify
5. Create system logic (incremental) → build after each method
6. Test continuously in-game
7. Document in SYSTEM_OVERVIEW.md when complete
8. Next feature

### Modifying Existing System
1. Read existing `.vacuo.md` if present
2. Create `.vacuo.md` if making >20 line changes
3. Mark changes: `// Vacuo - description`
4. Test after each change
5. Verify base SS14 functionality still works
6. Update `.vacuo.md` with final changes

---

## ECS Architecture Reference

### Component (Data Container)
```csharp
[RegisterComponent, NetworkedComponent]
public sealed partial class RadarSignatureComponent : Component
{
    [DataField] public float RadarCrossSection = 100f;
    [DataField, AutoNetworkedField] public float HeatSignature = 50f;
}
```

### System (Logic Processor)
```csharp
public sealed class RadarSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RadarSignatureComponent, ComponentStartup>(OnStartup);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RadarSignatureComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var radar, out var xform))
        {
            // Process each entity with both components
        }
    }

    private void OnStartup(EntityUid uid, RadarSignatureComponent comp, ComponentStartup args)
    {
        // Handle component initialization
    }
}
```

### Prototype (YAML Data)
```yaml
- type: entity
  id: VacuoDestroyerShip
  parent: BaseShuttle  # Inheritance
  components:
  - type: Shuttle
  - type: RadarSignature
    radarCrossSection: 800
    heatSignature: 400
  - type: IFF
    flags: Fleet
```

### Events
```csharp
// Define event
public sealed class MyEvent : EntityEventArgs
{
    public EntityUid Target;
}

// Subscribe
SubscribeLocalEvent<MyComponent, MyEvent>(OnMyEvent);

// Raise
RaiseLocalEvent(uid, new MyEvent { Target = uid });
```

---

## Important Patterns

### Networking
```csharp
// Automatic sync
[DataField, AutoNetworkedField] public float Value;

// Manual sync
RaiseNetworkEvent(new MyDataChangedEvent(data));

// Client prediction (shared system)
public abstract class SharedRadarSystem : EntitySystem { }
public sealed class RadarSystem : SharedRadarSystem { }  // In both Client/Server
```

### Performance
```csharp
// Efficient iteration
var query = EntityQueryEnumerator<Comp1, Comp2>();
while (query.MoveNext(out var uid, out var c1, out var c2)) { }

// Throttle updates
private float _timer = 0f;
public override void Update(float frameTime)
{
    _timer += frameTime;
    if (_timer < 1.0f) return;
    _timer = 0f;
    // Expensive work once per second
}
```

### Prototype Inheritance
```yaml
# Single parent
parent: BaseEntity

# Multiple parents
parent: [BaseShuttle, BaseCombat, BaseWeapon]

# Abstract base
- type: entity
  id: BaseMissile
  abstract: true  # Can't be spawned directly
```

---

## Example: Complete Feature Implementation

### Radar Signature System (Minimal Implementation)

**1. Define "Done"**:
```markdown
- [ ] Component compiles
- [ ] Detects ships at range based on RCS
- [ ] No crashes with 50+ ships
```

**2. Prototype First**:
```yaml
# Resources/Prototypes/_vacuo/Entities/Ships/test_ship.yml
- type: entity
  id: VacuoTestShip
  parent: BaseShuttle
  components:
  - type: RadarSignature  # Doesn't exist yet
    radarCrossSection: 500
```

**3. Component**:
```csharp
// Content.Shared/_vacuo/Sensors/Components/RadarSignatureComponent.cs
namespace Content.Shared._vacuo.Sensors.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RadarSignatureComponent : Component
{
    [DataField] public float RadarCrossSection = 100f;
}
```

**4. Build & Test**:
```bash
dotnet build  # Must pass
# Spawn VacuoTestShip in-game, verify it has component
```

**5. Detection System**:
```csharp
// Content.Server/_vacuo/Sensors/Systems/RadarSignatureSystem.cs
namespace Content.Server._vacuo.Sensors.Systems;

public sealed class RadarSignatureSystem : EntitySystem
{
    public bool CanDetect(float sensorPower, float range, RadarSignatureComponent target)
    {
        var detectionRange = sensorPower * target.RadarCrossSection / 100f;
        return range <= detectionRange;
    }
}
```

**6. Build & Test**:
```bash
dotnet build  # Must pass
# Test detection logic in-game
```

**7. Document**:
```markdown
// Content.Server/_vacuo/Sensors/SYSTEM_OVERVIEW.md
Created after system is working
```

---

## Lessons Learned

### Failed Janitorial Removal Attempt

**What happened**: Tried to delete janitorial/bartending systems → cascading errors:
- Missing prototype references in 50+ files
- Broken YAML inheritance chains
- Map loading failures
- Runtime crashes

**Lesson**: **Never remove base SS14 systems** - dependency graphs are too complex

**Solution**: Reverted all changes, adopted additive approach instead

**Takeaway**: Build alongside, don't remove. Test after each change, not after batch changes.

---

## Full Directory Structure

```
In-Vacuo/
├── Content.Shared/
│   ├── _vacuo/              ← All custom shared code here
│   │   ├── Sensors/
│   │   ├── Missiles/
│   │   ├── Combat/
│   │   └── ElectronicWarfare/
│   └── (base SS14 systems)
│
├── Content.Server/
│   ├── _vacuo/              ← All custom server code here
│   │   ├── Sensors/Systems/
│   │   ├── Missiles/Systems/
│   │   └── FireControl/
│   └── (base SS14 systems)
│
├── Content.Client/
│   ├── _vacuo/              ← All custom client code here
│   │   ├── Sensors/UI/
│   │   └── FireControl/UI/
│   └── (base SS14 systems)
│
├── Resources/
│   ├── Prototypes/_vacuo/   ← All custom prototypes
│   ├── Maps/_vacuo/         ← Combat maps
│   └── Textures/_vacuo/     ← Custom textures
│
├── CLAUDE.md                ← This file
├── vacuo_roadmap.md         ← Feature roadmap (no deadlines)
└── README.md
```

---

## Summary (Core Principles)

**The Three Absolutes**:
1. Custom code in `_vacuo/` directories
2. Never delete base SS14 systems
3. Test after every change

**Development Rhythm**:
1. Define "done"
2. Write YAML prototype
3. Minimal component → build → test
4. Incremental system → build → test
5. Document when working
6. Next feature

**Philosophy**: Additive, incremental, tested, documented. Quality over speed. No deadlines.

**Integration**: Extend existing SS14 systems (DeviceNetwork, GunSystem, etc.), don't replace them.

**When Lost**: Read this file from top, check `vacuo_roadmap.md` for feature context, check system `SYSTEM_OVERVIEW.md` for specifics.

---

## Current Development Status

**See**: `vacuo_roadmap.md` Phase 1 for current focus

**Next Immediate Task**: Implement `RadarSignatureComponent` following incremental pattern above

**Documentation Status**:
- ✅ Zombie system fully documented (SYSTEM_OVERVIEW.md + .vacuo.md)
- ⏳ Combat systems to be documented as implemented

Keep this file updated as patterns emerge.
