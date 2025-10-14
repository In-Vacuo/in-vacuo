using Content.Server.Administration.Managers;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Ghost;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Humanoid;
using Content.Server.Inventory;
using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Speech.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Body.Components;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Roles;
using Content.Shared.Tag;
using Content.Shared.Temperature.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Interaction.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Zombies;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Zombies;

/// <summary>
/// Handles zombie transformation in a clean, centralized way.
/// All transformation logic is here, organized into partial classes.
/// External systems can hook into zombification via events.
/// </summary>
public sealed partial class ZombieTransformationSystem : EntitySystem
{
    // Core services
    [Dependency] private readonly IBanManager _ban = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    // Appearance
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly NameModifierSystem _nameMod = default!;

    // Physiology
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;

    // Combat
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    // Mind/Role
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    // NPC/AI
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    private static readonly List<ProtoId<AntagPrototype>> BannableZombiePrototypes = ["Zombie"];

    public override void Initialize()
    {
        base.Initialize();

        // Subscribe to death events that should trigger zombification
        SubscribeLocalEvent<ZombifyOnDeathComponent, MobStateChangedEvent>(OnZombifyOnDeath);
    }

    private void OnZombifyOnDeath(EntityUid uid, ZombifyOnDeathComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            TryZombifyEntity(uid);
        }
    }

    /// <summary>
    /// Main entry point for zombie transformation.
    /// Handles the entire transformation process in a clean, organized way.
    /// </summary>
    public bool TryZombifyEntity(EntityUid target, EntityUid? configEntity = null)
    {
        // Pre-checks
        if (HasComp<ZombieComponent>(target) || HasComp<ZombieImmuneComponent>(target))
            return false;

        if (!TryComp<MobStateComponent>(target, out var mobState))
            return false;

        // Handle role bans
        HandleRoleBans(target);

        // Allow external systems to cancel
        var requestEv = new ZombificationRequestedEvent(target, configEntity);
        RaiseLocalEvent(target, requestEv);

        if (requestEv.Cancelled)
        {
            Log.Debug($"Zombification of {ToPrettyString(target)} cancelled: {requestEv.CancellationReason}");
            return false;
        }

        // Add zombie component
        var zombie = EnsureComp<ZombieComponent>(target);
        var config = EnsureComp<ZombieTransformationConfigComponent>(target);

        // Show popup
        _popup.PopupEntity(Loc.GetString("zombie-transform", ("target", target)), target, PopupType.LargeCaution);

        // Execute transformation (organized into partial methods)
        TransformAppearance(target, zombie, config);
        TransformPhysiology(target, zombie, config);
        TransformCombat(target, zombie, config);
        TransformMind(target, zombie, config);
        TransformNPC(target, zombie, config);

        // Final cleanup
        foreach (var tagId in config.AddTags)
        {
            _tag.AddTag(target, tagId);
        }

        RemCompDeferred<PendingZombieComponent>(target);

        // Notify external systems
        var legacyEv = new EntityZombifiedEvent(target);
        RaiseLocalEvent(target, ref legacyEv, true);

        Log.Debug($"Successfully zombified {ToPrettyString(target)}");
        return true;
    }

    private void HandleRoleBans(EntityUid target)
    {
        if (TryComp<ActorComponent>(target, out var actor) &&
            _ban.IsRoleBanned(actor.PlayerSession, BannableZombiePrototypes))
        {
            var sess = actor.PlayerSession;
            var message = Loc.GetString("zombie-roleban-ghosted");

            if (_mind.TryGetMind(sess, out var playerMindEnt, out var playerMind))
            {
                _ghost.SpawnGhost((playerMindEnt, playerMind), target);
                _chatMan.DispatchServerMessage(sess, message);
            }
            else
            {
                Log.Error($"Mind for session '{sess}' could not be found during zombie ban check");
            }
        }
    }
}
