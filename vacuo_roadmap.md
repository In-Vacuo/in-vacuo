# In Vacuo - Space Combat Fork Roadmap

## Vision Statement
Transform Space Station 14 into an asymmetric spaceship combat game focused on logistics, strategy, and intelligence gathering. Core gameplay revolves around beyond visual range (BVR) engagements, electronic warfare, and realistic detection mechanics.

## Core Design Principles
- **Tactical Depth**: Every decision matters - from power distribution to sensor management
- **Information Warfare**: Detection and intelligence are as important as firepower
- **Asymmetric Balance**: Different factions with fundamentally different playstyles
- **Combined Arms**: Ship-to-ship combat supplemented by boarding and ground operations
- **Resource Management**: Limited ammunition and supplies drive strategic decisions

---

## **DEVELOPMENT APPROACH: ADDITIVE ARCHITECTURE**

**Philosophy**: Build combat systems as modular additions that coexist with the base SS14 codebase rather than removing existing systems. This approach:

✅ **Advantages:**
- No breaking changes to existing maps/content
- Gradual testing and iteration possible
- Can create combat-specific maps alongside station maps
- Maintains working game throughout development
- Easy to showcase progress incrementally
- Can disable non-combat systems via game mode configuration

✅ **Strategy:**
- Create new namespaces for combat systems
- Extend existing components with optional combat fields
- Use game modes to toggle between station RP and combat scenarios
- Build combat-specific maps separate from existing station maps
- Leverage existing networking, physics, and UI infrastructure

---

## Phase 1: Core Combat Framework (Weeks 1-2)
*Goal: Establish foundation for tactical combat systems*

### New Directory Structure

**IMPORTANT CONVENTION**: All In Vacuo-specific code goes in `_vacuo/` subdirectories:

```
Content.Shared/_vacuo/Combat/           - Combat-specific shared components
Content.Shared/_vacuo/Sensors/          - Detection and radar systems
Content.Shared/_vacuo/ElectronicWarfare/ - ECM/ECCM systems
Content.Shared/_vacuo/Missiles/         - Guided weapon systems
Content.Shared/_vacuo/Logistics/        - Supply and ammunition

Content.Server/_vacuo/Combat/           - Server combat logic
Content.Server/_vacuo/Sensors/          - Radar processing
Content.Server/_vacuo/ElectronicWarfare/ - EW system logic
Content.Server/_vacuo/Missiles/         - Guidance calculations
Content.Server/_vacuo/FireControl/      - Targeting systems

Content.Client/_vacuo/Combat/           - Combat UI
Content.Client/_vacuo/Sensors/UI/       - Tactical displays
Content.Client/_vacuo/FireControl/UI/   - Weapon control interfaces

Resources/Prototypes/_vacuo/            - All In Vacuo prototypes
Resources/Maps/_vacuo/                  - Combat-specific maps
```

**Code Modification Conventions:**
- **New systems**: Place in `_vacuo/` directories
- **Minor changes**: Mark with `// Vacuo - [description]` comments
- **Major rewrites**: Document in `SystemName.vacuo.md` file in same directory

### Component Architecture

**1. Radar Signature System**
```csharp
// Content.Shared/_vacuo/Sensors/Components/RadarSignatureComponent.cs
[RegisterComponent, NetworkedComponent]
public sealed partial class RadarSignatureComponent : Component
{
    /// <summary>
    /// Radar cross-section in arbitrary units. Higher = easier to detect.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RadarCrossSection = 100f;

    /// <summary>
    /// Heat signature from engines/reactors. Detectable by IR sensors.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HeatSignature = 50f;

    /// <summary>
    /// EM emissions from radar/comms. Detectable by passive sensors.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EMEmission = 30f;

    /// <summary>
    /// Signature reduction multiplier when stealth is active.
    /// </summary>
    [DataField]
    public float StealthMultiplier = 1.0f;

    [DataField]
    public bool StealthActive = false;
}
```

**2. Enhanced Radar Console**
```csharp
// Content.Shared/_vacuo/Sensors/Components/AdvancedRadarConsoleComponent.cs
[RegisterComponent, NetworkedComponent]
public sealed partial class AdvancedRadarConsoleComponent : Component
{
    /// <summary>
    /// Sensor mode: Active (high power, detects everything) or Passive (low power, limited info)
    /// </summary>
    [DataField]
    public SensorMode Mode = SensorMode.Active;

    /// <summary>
    /// Maximum detection range in active mode
    /// </summary>
    [DataField]
    public float ActiveRange = 512f;

    /// <summary>
    /// Maximum detection range in passive mode
    /// </summary>
    [DataField]
    public float PassiveRange = 256f;

    /// <summary>
    /// Power consumption multiplier
    /// </summary>
    [DataField]
    public float PowerDraw = 1.0f;

    /// <summary>
    /// Tracked contacts with classification
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, RadarContact> Contacts = new();
}

public enum SensorMode
{
    Active,
    Passive,
    PoweredDown
}

public record struct RadarContact
{
    public EntityUid Target;
    public ContactClassification Classification;
    public float SignalStrength;
    public float LastUpdateTime;
    public Vector2 EstimatedPosition;
    public Vector2 EstimatedVelocity;
}

public enum ContactClassification
{
    Unknown,
    Friendly,
    Neutral,
    Hostile
}
```

**3. Missile Guidance Component**
```csharp
// Content.Shared/_vacuo/Missiles/Components/MissileGuidanceComponent.cs
[RegisterComponent, NetworkedComponent]
public sealed partial class MissileGuidanceComponent : Component
{
    [DataField]
    public GuidanceType GuidanceType = GuidanceType.Dumbfire;

    [DataField]
    public EntityUid? TargetEntity;

    [DataField]
    public float TrackingStrength = 1.0f;

    [DataField]
    public float FuelRemaining = 100f;

    [DataField]
    public float MaxRange = 500f;

    [DataField]
    public bool IsTracking = false;
}

public enum GuidanceType
{
    Dumbfire,           // No guidance
    ActiveRadar,        // Fire-and-forget radar homing
    SemiActiveRadar,    // Requires continuous illumination from launcher
    InfraredHoming,     // Heat-seeking
    CommandGuided,      // Manual control
    AntiRadiation       // Homes on radar emissions
}
```

**4. Electronic Warfare Component**
```csharp
// Content.Shared/_vacuo/ElectronicWarfare/Components/ElectronicWarfareComponent.cs
[RegisterComponent, NetworkedComponent]
public sealed partial class ElectronicWarfareComponent : Component
{
    /// <summary>
    /// ECM jamming power - reduces enemy radar effectiveness
    /// </summary>
    [DataField]
    public float JammingPower = 0f;

    /// <summary>
    /// ECCM resistance - counters enemy jamming
    /// </summary>
    [DataField]
    public float JammingResistance = 0f;

    /// <summary>
    /// Number of decoys/chaff remaining
    /// </summary>
    [DataField]
    public int DecoyCount = 0;

    /// <summary>
    /// IFF spoofing active
    /// </summary>
    [DataField]
    public bool SpoofingActive = false;

    /// <summary>
    /// Current jamming state
    /// </summary>
    [DataField]
    public bool JammingActive = false;
}
```

**5. Fire Control Component**
```csharp
// Content.Shared/_vacuo/Combat/Components/FireControlComponent.cs
[RegisterComponent, NetworkedComponent]
public sealed partial class FireControlComponent : Component
{
    [DataField]
    public List<EntityUid> TrackedTargets = new();

    [DataField]
    public int MaxSimultaneousTargets = 1;

    [DataField]
    public EntityUid? CurrentTarget;

    [DataField]
    public float TrackingAccuracy = 1.0f;

    [DataField]
    public float PredictionQuality = 0.8f;

    [DataField]
    public bool AutoFire = false;
}
```

### Integration with Existing Systems

**Leverage Existing:**
- `DeviceNetworkSystem` for ship-to-ship datalinks and ECM
- `RadarConsoleSystem` as base for enhanced sensors
- `ShuttleSystem` for ship movement (no changes needed)
- `GunSystem` for weapon firing (add missile support)
- `ProjectileComponent` for missiles (add optional guidance fields)
- `DamageableSystem` for combat damage (no changes needed)
- `PowerNetSystem` for sensor/weapon power (add consumption tracking)

---

## Phase 2: Enhanced Detection (Weeks 3-4)
*Goal: Implement signature-based radar and passive sensors*

### Advanced Radar System

**File**: `Content.Server/_vacuo/Sensors/Systems/AdvancedRadarSystem.cs`

```csharp
public sealed class AdvancedRadarSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<AdvancedRadarConsoleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var radar, out var xform))
        {
            if (radar.Mode == SensorMode.PoweredDown)
                continue;

            UpdateRadarContacts(uid, radar, xform, frameTime);
            ApplyJammingEffects(uid, radar);
        }
    }

    private void UpdateRadarContacts(EntityUid radarUid, AdvancedRadarConsoleComponent radar, TransformComponent xform, float frameTime)
    {
        var range = radar.Mode == SensorMode.Active ? radar.ActiveRange : radar.PassiveRange;
        var radarPos = xform.WorldPosition;

        // Query all entities with radar signatures in range
        var signatureQuery = EntityQueryEnumerator<RadarSignatureComponent, TransformComponent>();
        while (signatureQuery.MoveNext(out var targetUid, out var signature, out var targetXform))
        {
            if (targetUid == radarUid)
                continue;

            var distance = (targetXform.WorldPosition - radarPos).Length();
            if (distance > range)
                continue;

            // Calculate detection probability based on signature and mode
            var detectionChance = CalculateDetectionProbability(signature, distance, radar.Mode);

            if (detectionChance > 0.5f) // Threshold for contact
            {
                UpdateContact(radar, targetUid, signature, targetXform, distance);
            }
        }
    }
}
```

### Signature-Based Detection

- Active radar detects based on RCS
- Passive sensors detect heat and EM emissions
- Detection range scales with signature strength
- Jamming degrades detection probability
- Classification based on IFF transponder responses

### Power Integration

Extend existing `ApcPowerReceiverComponent` usage:
- Active radar: High power draw
- Passive sensors: Low power draw
- ECM jamming: Very high power draw
- Sensor quality degrades with insufficient power

---

## Phase 3: Missile Warfare (Weeks 5-7)
*Goal: Implement guided missiles and countermeasures*

### Missile Guidance System

**File**: `Content.Server/_vacuo/Missiles/Systems/MissileGuidanceSystem.cs`

```csharp
public sealed class MissileGuidanceSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<MissileGuidanceComponent, ProjectileComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var guidance, out var projectile, out var physics))
        {
            if (!guidance.IsTracking || guidance.TargetEntity == null)
                continue;

            switch (guidance.GuidanceType)
            {
                case GuidanceType.ActiveRadar:
                    UpdateActiveRadarGuidance(uid, guidance, physics);
                    break;
                case GuidanceType.InfraredHoming:
                    UpdateHeatSeekingGuidance(uid, guidance, physics);
                    break;
                case GuidanceType.SemiActiveRadar:
                    UpdateSemiActiveGuidance(uid, guidance, physics);
                    break;
            }

            guidance.FuelRemaining -= frameTime;
            if (guidance.FuelRemaining <= 0)
                guidance.IsTracking = false;
        }
    }
}
```

### Missile Types (Prototypes)

```yaml
# Resources/Prototypes/_vacuo/Entities/Weapons/Missiles/base_missile.yml

- type: entity
  id: BaseMissile
  abstract: true
  parent: BaseBullet
  components:
  - type: MissileGuidance
    guidanceType: Dumbfire
    fuelRemaining: 10
    maxRange: 500
  - type: RadarSignature
    radarCrossSection: 5  # Small signature
    heatSignature: 80     # Hot engine
  - type: Projectile
    deleteOnCollide: true
    damage:
      types:
        Explosive: 100
  - type: TimedDespawn
    lifetime: 30

- type: entity
  id: MissileActiveRadar
  name: active radar homing missile
  parent: BaseMissile
  components:
  - type: MissileGuidance
    guidanceType: ActiveRadar
    trackingStrength: 0.8
  - type: Sprite
    sprite: Objects/Weapons/Missiles/missile_active.rsi

- type: entity
  id: MissileHeatSeeking
  name: heat-seeking missile
  parent: BaseMissile
  components:
  - type: MissileGuidance
    guidanceType: InfraredHoming
    trackingStrength: 0.9
  - type: Sprite
    sprite: Objects/Weapons/Missiles/missile_ir.rsi
```

### Countermeasures

```csharp
// Content.Shared/_vacuo/Combat/Components/CountermeasureDispenserComponent.cs
[RegisterComponent]
public sealed partial class CountermeasureDispenserComponent : Component
{
    [DataField]
    public int FlareCount = 20;

    [DataField]
    public int ChaffCount = 20;

    [DataField]
    public float DispenseInterval = 1.0f;
}
```

### Point Defense System

Extend existing `DeployableTurretSystem` to auto-engage missiles:
- Detect incoming missiles via radar
- Calculate intercept trajectories
- Engage within point defense range
- Track kill probability

---

## Phase 4: Electronic Warfare (Weeks 8-9)
*Goal: Implement ECM, ECCM, and information warfare*

### ECM/Jamming System

**File**: `Content.Server/_vacuo/ElectronicWarfare/Systems/ECMSystem.cs`

```csharp
public sealed class ECMSystem : EntitySystem
{
    public override void Update(float frameTime)
    {
        // For each active ECM source
        var ecmQuery = EntityQueryEnumerator<ElectronicWarfareComponent, TransformComponent>();
        while (ecmQuery.MoveNext(out var ecmUid, out var ecm, out var ecmXform))
        {
            if (!ecm.JammingActive || ecm.JammingPower <= 0)
                continue;

            // Affect all radar systems in range
            var radarQuery = EntityQueryEnumerator<AdvancedRadarConsoleComponent, TransformComponent>();
            while (radarQuery.MoveNext(out var radarUid, out var radar, out var radarXform))
            {
                var distance = (radarXform.WorldPosition - ecmXform.WorldPosition).Length();
                var jammingEffect = CalculateJammingEffect(ecm.JammingPower, radar.PowerDraw, distance);

                ApplyJammingToRadar(radarUid, radar, jammingEffect);
            }
        }
    }
}
```

### IFF System Enhancement

Extend existing `IFFComponent`:
```csharp
// Content.Shared/_vacuo/Sensors/Components/IFFExtensionComponent.cs
// Or add to existing IFFComponent with // Vacuo - Enhanced IFF modes
public sealed partial class IFFComponent : Component
{
    [DataField]
    public IFFMode Mode = IFFMode.Normal;

    [DataField]
    public string SpoofedFlags = "";

    [DataField]
    public bool RespondsToInterrogation = true;
}

public enum IFFMode
{
    Normal,      // Standard IFF response
    Silent,      // No IFF response
    Spoofed      // False IFF signal
}
```

### Datalink Network

Use existing `DeviceNetworkSystem` for fleet coordination:
- Shared radar picture between friendly ships
- Encrypted communications
- Target designation handoff
- Formation coordination messages

---

## Phase 5: Fire Control & Targeting (Weeks 10-11)
*Goal: Implement advanced targeting and weapon coordination*

### Fire Control Console

**UI**: `Content.Client/_vacuo/FireControl/UI/FireControlWindow.xaml`

Features:
- Target list with threat assessment
- Weapon assignment interface
- Time-to-impact display
- Kill probability estimates
- Auto-engage rules configuration

### Weapon Integration

Extend `GunSystem` to support fire control:
```csharp
// Add to Content.Server/Weapons/Ranged/Systems/GunSystem.cs
private void UpdateFireControl(EntityUid gunUid, GunComponent gun)
{
    if (!TryComp<FireControlComponent>(gunUid, out var fc))
        return;

    if (fc.AutoFire && fc.CurrentTarget != null)
    {
        // Auto-engage if target in range and weapon ready
        if (CanEngageTarget(gunUid, gun, fc.CurrentTarget.Value))
        {
            AttemptShoot(gunUid, gun, fc.CurrentTarget.Value);
        }
    }
}
```

---

## Phase 6: Ship Classifications & Prototypes (Week 12)
*Goal: Create diverse ship types with distinct roles*

### Ship Prototype Structure

```yaml
# Resources/Prototypes/_vacuo/Entities/Ships/combat_ships.yml

- type: entity
  id: ShipDestroyerFleet
  name: Fleet Destroyer
  parent: BaseShuttle
  description: Missile-focused warship with advanced fire control.
  components:
  - type: Shuttle
  - type: IFF
    flags: Fleet
    color: Blue
  - type: RadarSignature
    radarCrossSection: 800
    heatSignature: 400
    emEmission: 200
  - type: FireControl
    maxSimultaneousTargets: 4
    trackingAccuracy: 0.95
  # Missile launchers would be separate entities docked to the ship
  - type: PowerConsumer
    drawRate: 5000  # High power for sensors

- type: entity
  id: ShipCorvetteRaider
  name: Raider Corvette
  parent: BaseShuttle
  description: Fast, stealthy scout with electronic warfare capabilities.
  components:
  - type: Shuttle
  - type: IFF
    flags: Raiders
    color: Red
  - type: RadarSignature
    radarCrossSection: 200
    heatSignature: 150
    emEmission: 50
    stealthMultiplier: 0.3  # Stealth coating
  - type: ElectronicWarfare
    jammingPower: 500
    jammingResistance: 300
    decoyCount: 10
  - type: MovementSpeedModifier
    baseWalkSpeed: 8
    baseSprintSpeed: 12  # Fast ship
```

### Weapon Emplacements

```yaml
# Resources/Prototypes/_vacuo/Entities/Weapons/missile_launchers.yml

- type: entity
  id: MissileLauncherMk1
  name: Mk1 Missile Launcher
  description: Standard vertical launch system for anti-ship missiles.
  components:
  - type: Sprite
    sprite: Structures/Weapons/missile_vls.rsi
  - type: MagazineAmmoProvider
    magazinePrototype: MissileMagazine8Round
    autoEject: false
  - type: Gun
    fireRate: 0.5  # 2 second reload
    soundGunshot:
      path: /Audio/Weapons/missile_launch.ogg
  - type: Anchorable
  - type: Rotatable
  - type: PowerConsumer
    drawRate: 1000

- type: entity
  id: PointDefenseTurret
  name: Point Defense Cannon
  description: Rapid-fire cannon for intercepting missiles.
  parent: DeployableTurret
  components:
  - type: Gun
    fireRate: 10  # Very fast
    selectedMode: FullAuto
  - type: BallisticAmmoProvider
    whitelist:
      tags:
      - CartridgePDC
  - type: PointDefenseTargeting
    engageRange: 100
    prioritizeMissiles: true
```

---

## Phase 7: Combat Maps & Game Modes (Week 13)
*Goal: Create dedicated combat scenarios*

### Combat-Specific Maps

Create new map directory: `Resources/Maps/_vacuo/`

**Arena Maps:**
- `Resources/Maps/_vacuo/arena_open.yml` - Open space battle
- `Resources/Maps/_vacuo/arena_asteroids.yml` - Asteroid field combat
- `Resources/Maps/_vacuo/convoy_escort.yml` - Convoy protection scenario

**Map Features:**
- Spawn points for different ship classes
- Objectives (capture points, convoy routes)
- Environmental hazards (asteroid fields, nebulae)
- Resupply stations

### Game Mode Configuration

**File**: `Content.Server/_vacuo/GameTicking/GameModes/CombatGameMode.cs`

```csharp
public sealed class CombatGameMode : GameMode
{
    [DataField]
    public string MapPrototype = "CombatArenaOpen";

    [DataField]
    public List<ShipSpawnConfig> FleetShips = new();

    [DataField]
    public List<ShipSpawnConfig> RaiderShips = new();

    [DataField]
    public float MatchDuration = 1800f; // 30 minutes

    public override void Startup()
    {
        // Spawn ships for both factions
        SpawnFleet(FleetShips, "Fleet");
        SpawnFleet(RaiderShips, "Raiders");

        // Initialize objectives
        SetupObjectives();
    }
}
```

### Prototype-Driven Configuration

```yaml
# Resources/Prototypes/_vacuo/GameModes/combat_modes.yml

- type: gameMode
  id: FleetBattle
  name: Fleet Engagement
  description: Large-scale fleet vs fleet battle
  map: CombatArenaOpen
  fleetShips:
  - ship: ShipDestroyerFleet
    count: 2
    spawns: FleetSpawn1
  - ship: ShipFrigateFleet
    count: 4
    spawns: FleetSpawn2
  raiderShips:
  - ship: ShipCorvetteRaider
    count: 6
    spawns: RaiderSpawn1
```

---

## Phase 8: Logistics & Resources (Weeks 14-15)
*Goal: Implement ammunition and supply management*

### Ammunition System

```csharp
// Content.Shared/_vacuo/Logistics/Components/AmmunitionStorageComponent.cs
[RegisterComponent]
public sealed partial class AmmunitionStorageComponent : Component
{
    [DataField]
    public Dictionary<string, int> StoredAmmo = new();

    [DataField]
    public int MaxCapacity = 100;
}
```

### Resupply Mechanics

- Ammunition transfer between ships via docking
- Fabrication of missiles on capital ships (use existing construction system)
- Supply convoys with escort missions
- Resource depletion affects combat effectiveness

### Ship Fuel System

Extend existing `FTLComponent`:
```csharp
public sealed partial class FTLComponent : Component
{
    // Add fuel tracking
    [DataField]
    public float FuelRemaining = 100f;

    [DataField]
    public float FuelConsumptionRate = 1.0f;
}
```

---

## Phase 9: Polish & Game Loop (Week 16)
*Goal: Complete MVP with playable combat scenarios*

### Victory Conditions

- Team elimination
- Objective capture
- Time-based scoring
- Resource depletion

### UI Polish

- Tactical overlay improvements
- Threat assessment visualization
- Damage control interface
- Power distribution display

### Balance Pass

- Weapon ranges and damage
- Sensor effectiveness vs stealth
- Missile vs point defense effectiveness
- Ship speed vs signature tradeoffs

---

## Technical Implementation Priority

### Week-by-Week Breakdown

**Week 1-2:**
1. Create directory structure for new systems
2. Implement `RadarSignatureComponent`
3. Implement `AdvancedRadarConsoleComponent`
4. Basic signature-based detection system

**Week 3-4:**
5. Implement `ElectronicWarfareComponent`
6. ECM/jamming affecting radar
7. Passive sensor modes
8. IFF enhancement

**Week 5-7:**
9. Implement `MissileGuidanceComponent`
10. Guidance system logic
11. Missile prototypes (active radar, IR)
12. Countermeasure system
13. Point defense targeting

**Week 8-9:**
14. Implement `FireControlComponent`
15. Target tracking and prediction
16. Weapon-target assignment
17. Auto-fire modes

**Week 10-11:**
18. Datalink network using DeviceNetwork
19. Fleet coordination features
20. Formation flying helpers

**Week 12-13:**
21. Create combat ship prototypes
22. Build combat maps
23. Implement combat game mode
24. Basic victory conditions

**Week 14-15:**
25. Ammunition storage system
26. Resupply mechanics
27. Fuel consumption
28. Resource management UI

**Week 16:**
29. Balance pass
30. UI polish
31. Performance optimization
32. Bug fixes

---

## MVP Feature Set

### Must-Have for MVP:
- ✅ Signature-based radar detection
- ✅ Active/passive sensor modes
- ✅ Guided missiles (at least 2 guidance types)
- ✅ Basic ECM/jamming
- ✅ Fire control with target tracking
- ✅ 4-6 distinct ship types
- ✅ 2 combat maps
- ✅ Basic combat game mode
- ✅ Ammunition management
- ✅ Victory conditions

### Nice-to-Have (Post-MVP):
- Advanced formation flying
- Persistent campaign mode
- More guidance types (command-guided, anti-radiation)
- Advanced ECM (spoofing, decoys)
- Datalink network visualization
- AI-controlled ships

---

## File Structure

### New Files to Create (Estimated 50-60 files):

**Components:**
- `Content.Shared/_vacuo/Sensors/Components/RadarSignatureComponent.cs`
- `Content.Shared/_vacuo/Sensors/Components/AdvancedRadarConsoleComponent.cs`
- `Content.Shared/_vacuo/Missiles/Components/MissileGuidanceComponent.cs`
- `Content.Shared/_vacuo/ElectronicWarfare/Components/ElectronicWarfareComponent.cs`
- `Content.Shared/_vacuo/Combat/Components/FireControlComponent.cs`
- `Content.Shared/_vacuo/Combat/Components/CountermeasureDispenserComponent.cs`
- `Content.Shared/_vacuo/Logistics/Components/AmmunitionStorageComponent.cs`

**Systems:**
- `Content.Server/_vacuo/Sensors/Systems/AdvancedRadarSystem.cs`
- `Content.Server/_vacuo/Missiles/Systems/MissileGuidanceSystem.cs`
- `Content.Server/_vacuo/ElectronicWarfare/Systems/ECMSystem.cs`
- `Content.Server/_vacuo/Combat/Systems/FireControlSystem.cs`
- `Content.Server/_vacuo/Combat/Systems/CountermeasureSystem.cs`
- `Content.Server/_vacuo/Logistics/Systems/AmmunitionSystem.cs`

**UI:**
- `Content.Client/_vacuo/Sensors/UI/AdvancedRadarWindow.xaml(.cs)`
- `Content.Client/_vacuo/FireControl/UI/FireControlWindow.xaml(.cs)`
- `Content.Client/_vacuo/Combat/UI/TacticalOverlay.xaml(.cs)`

**Prototypes:**
- `Resources/Prototypes/_vacuo/Entities/Ships/combat_ships.yml`
- `Resources/Prototypes/_vacuo/Entities/Weapons/Missiles/` (directory)
- `Resources/Prototypes/_vacuo/Entities/Weapons/launchers.yml`
- `Resources/Prototypes/_vacuo/GameModes/combat_modes.yml`

**Maps:**
- `Resources/Maps/_vacuo/arena_open.yml`
- `Resources/Maps/_vacuo/convoy_escort.yml`

---

## Key Files to Modify (Minimal Changes)

### Existing Files - Minor Extensions:

1. **Content.Shared/Projectiles/ProjectileComponent.cs**
   - Add optional `MissileGuidanceComponent` field
   - No breaking changes to existing projectiles

2. **Content.Server/Shuttles/Systems/RadarConsoleSystem.cs**
   - Add signature-based filtering option
   - Backwards compatible with existing radar

3. **Content.Server/Weapons/Ranged/Systems/GunSystem.cs**
   - Add fire control integration hooks
   - Optional feature, doesn't affect existing guns

4. **Content.Shared/IFF/IFFComponent.cs**
   - Add spoofing modes
   - Backwards compatible

---

## Success Metrics

### Phase 1-2 (Weeks 1-4):
- RadarSignature component on all ships
- Radar detects based on signatures
- Active/passive modes functional

### Phase 3 (Weeks 5-7):
- Missiles track targets
- At least 2 guidance types working
- Countermeasures affect missile tracking

### Phase 4 (Weeks 8-9):
- ECM degrades radar resolution
- IFF spoofing works
- Passive sensors provide bearing-only data

### Phase 5 (Weeks 10-11):
- Fire control tracks multiple targets
- Auto-fire modes functional
- Weapon-target assignment working

### Phase 6 (Week 12-13):
- 4+ ship types playable
- 2+ combat maps functional
- Combat game mode starts/ends properly

### Phase 7 (Weeks 14-15):
- Ammunition consumption tracked
- Resupply mechanics working
- Resource limits affect gameplay

### Phase 8 (Week 16):
- Balanced combat engagement
- Smooth performance with 10+ ships
- All MVP features complete

---

## Long-Term Vision

### Optional System Disabling (Post-MVP)

Once combat systems are stable, can optionally disable non-combat systems via config:
```toml
# Config/combat_mode.toml
[game]
disable_chemistry = true
disable_botany = true
disable_cooking = true
restrict_jobs = ["Captain", "Helm", "Weapons", "Sensors", "Engineering"]
```

### Future Expansions

- Campaign mode with persistent fleet state
- Procedural mission generation
- Advanced AI fleet tactics
- Modding support for custom ships/weapons
- Spectator/replay system
- Tournament matchmaking

---

## Conclusion

This **additive approach** transforms In Vacuo development from a risky refactoring project into a controlled feature addition process. By building combat systems alongside existing SS14 content, we:

1. Maintain a working game at all times
2. Can test incrementally
3. Avoid breaking existing content
4. Enable parallel development
5. Reduce technical risk substantially

The modular architecture ensures combat systems are cleanly separated, making them easy to maintain, test, and eventually extract if needed. Development timeline remains 12-16 weeks to MVP, but with significantly lower risk and more frequent testable milestones.
