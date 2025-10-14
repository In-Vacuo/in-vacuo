using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Zombies;

/// <summary>
/// Configuration for zombie transformation. All hardcoded values externalized here.
/// Can be overridden per-entity or use defaults from ZombieComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ZombieTransformationConfigComponent : Component
{
    /// <summary>
    /// The faction to assign to the zombie
    /// </summary>
    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "Zombie";

    /// <summary>
    /// The mind role to assign to zombified players
    /// </summary>
    [DataField]
    public string MindRole = "MindRoleZombie";

    /// <summary>
    /// The HTN task for NPC zombies
    /// </summary>
    [DataField]
    public string HtnTask = "SimpleHostileCompound";

    /// <summary>
    /// The accent to apply to zombie speech
    /// </summary>
    [DataField]
    public string Accent = "zombie";

    /// <summary>
    /// Inventory slots to forcibly unequip during zombification
    /// </summary>
    [DataField]
    public List<string> RemoveSlots = new()
    {
        "gloves",
        "ears"
    };

    /// <summary>
    /// Tags to add to the zombie entity
    /// </summary>
    [DataField]
    public List<string> AddTags = new()
    {
        "InvalidForGlobalSpawnSpell",
        "CannotSuicide"
    };

    /// <summary>
    /// Emotes to add when zombie is alive
    /// </summary>
    [DataField]
    public List<string> EmotesOnAlive = new()
    {
        "Scream",      // When damaged
        "ZombieGroan"  // Random groaning
    };

    /// <summary>
    /// Whether humanoid zombies can pry open doors
    /// </summary>
    [DataField]
    public bool CanPryDoors = true;

    /// <summary>
    /// Prying speed modifier for zombies
    /// </summary>
    [DataField]
    public float PrySpeedModifier = 0.75f;

    /// <summary>
    /// Whether zombies can pry powered doors
    /// </summary>
    [DataField]
    public bool PryPowered = true;

    /// <summary>
    /// Whether zombies pry with force (always succeed)
    /// </summary>
    [DataField]
    public bool PryForce = true;

    /// <summary>
    /// The blood reagent to give zombies
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> BloodReagent = "ZombieBlood";

    /// <summary>
    /// The damage modifier set to apply
    /// </summary>
    [DataField]
    public string DamageModifierSet = "Zombie";

    /// <summary>
    /// Whether to make the entity sentient if it isn't already
    /// </summary>
    [DataField]
    public bool MakeSentient = true;

    /// <summary>
    /// Whether to heal all damage and set to alive state
    /// </summary>
    [DataField]
    public bool HealOnTransformation = true;

    /// <summary>
    /// Whether to remove hands during zombification
    /// </summary>
    [DataField]
    public bool RemoveHands = true;

    /// <summary>
    /// Whether to remove puller component
    /// </summary>
    [DataField]
    public bool RemovePuller = true;

    /// <summary>
    /// Whether to clear all existing factions before adding zombie faction
    /// </summary>
    [DataField]
    public bool ClearExistingFactions = true;

    /// <summary>
    /// Whether to set blood loss threshold to zero (zombies don't die from blood loss)
    /// </summary>
    [DataField]
    public bool DisableBloodLoss = true;

    /// <summary>
    /// The melee weapon range for humanoid zombies
    /// </summary>
    [DataField]
    public float HumanoidMeleeRange = 1.2f;

    /// <summary>
    /// The melee weapon angle for zombies
    /// </summary>
    [DataField]
    public float MeleeAngle = 0.0f;

    /// <summary>
    /// Whether to disable combat disarm for zombies
    /// </summary>
    [DataField]
    public bool DisableCombatDisarm = true;

    /// <summary>
    /// Whether to force zombies into combat mode
    /// </summary>
    [DataField]
    public bool ForceInCombatMode = true;
}
