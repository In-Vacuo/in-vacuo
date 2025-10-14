using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Inventory;
using Content.Server.Speech.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Traits.Assorted;
using Content.Shared.Interaction.Components;
using Content.Shared.Zombies;

namespace Content.Server.Zombies;

/// <summary>
/// Handles physiological changes during zombification.
/// </summary>
public sealed partial class ZombieTransformationSystem
{
    private void TransformPhysiology(EntityUid uid, ZombieComponent zombie, ZombieTransformationConfigComponent config)
    {
        // Remove biological needs
        RemComp<RespiratorComponent>(uid);
        RemComp<BarotraumaComponent>(uid);
        RemComp<HungerComponent>(uid);
        RemComp<ThirstComponent>(uid);
        RemComp<ReproductiveComponent>(uid);
        RemComp<ReproductivePartnerComponent>(uid);
        RemComp<LegsParalyzedComponent>(uid);
        RemComp<ComplexInteractionComponent>(uid);
        RemComp<SentienceTargetComponent>(uid);

        // Add zombie accent (server-only component, no need to dirty)
        // Check for accent override component first (e.g., moths have "zombieMoth")
        var accent = config.Accent;
        if (TryComp<ZombieAccentOverrideComponent>(uid, out var accentOverride))
        {
            accent = accentOverride.Accent;
        }

        var accentComp = EnsureComp<ReplacementAccentComponent>(uid);
        accentComp.Accent = accent;

        // Bloodstream changes
        if (TryComp<BloodstreamComponent>(uid, out var bloodstream))
        {
            zombie.BeforeZombifiedBloodReagent = bloodstream.BloodReagent;

            if (config.DisableBloodLoss)
            {
                _bloodstream.SetBloodLossThreshold(uid, 0f);
            }

            _bloodstream.ChangeBloodReagent(uid, config.BloodReagent);
        }

        // Damage modifiers
        _damageable.SetDamageModifierSetId(uid, config.DamageModifierSet);

        // Temperature immunity (not networked, no need to dirty)
        if (TryComp<TemperatureComponent>(uid, out var temp))
        {
            temp.ColdDamage.ClampMax(0);
        }

        // Unequip items
        foreach (var slot in config.RemoveSlots)
        {
            _inventory.TryUnequip(uid, slot, true, true);
        }

        // Heal and revive
        if (config.HealOnTransformation)
        {
            if (TryComp<DamageableComponent>(uid, out var damageable))
            {
                _damageable.SetAllDamage(uid, damageable, 0);
            }

            _mobState.ChangeMobState(uid, MobState.Alive);
        }

        // Remove puller
        if (config.RemovePuller)
        {
            RemComp<PullerComponent>(uid);
        }

        // Refresh movement speed (applies zombie slowdown)
        _movementSpeed.RefreshMovementSpeedModifiers(uid);

        Dirty(uid, zombie);
    }
}
