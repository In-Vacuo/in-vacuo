using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Prying.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Zombies;

namespace Content.Server.Zombies;

/// <summary>
/// Handles combat configuration during zombification.
/// </summary>
public sealed partial class ZombieTransformationSystem
{
    private void TransformCombat(EntityUid uid, ZombieComponent zombie, ZombieTransformationConfigComponent config)
    {
        // Combat mode
        var combat = EnsureComp<CombatModeComponent>(uid);
        RemComp<PacifiedComponent>(uid);

        if (config.DisableCombatDisarm)
        {
            _combat.SetCanDisarm(uid, false, combat);
        }

        if (config.ForceInCombatMode)
        {
            _combat.SetInCombatMode(uid, true, combat);
        }

        Dirty(uid, combat);

        // Melee weapon
        var melee = EnsureComp<MeleeWeaponComponent>(uid);
        melee.Animation = zombie.AttackAnimation;
        melee.WideAnimation = zombie.AttackAnimation;
        melee.AltDisarm = false;
        melee.Angle = config.MeleeAngle;
        melee.HitSound = zombie.BiteSound;

        // Humanoids get full damage and prying
        if (TryComp<HumanoidAppearanceComponent>(uid, out _))
        {
            melee.Damage = zombie.DamageOnBite;
            melee.Range = config.HumanoidMeleeRange;

            if (config.CanPryDoors)
            {
                var pry = EnsureComp<PryingComponent>(uid);
                pry.SpeedModifier = config.PrySpeedModifier;
                pry.PryPowered = config.PryPowered;
                pry.Force = config.PryForce;
                Dirty(uid, pry);
            }
        }

        DirtyFields(uid, melee, null, fields:
        [
            nameof(MeleeWeaponComponent.Animation),
            nameof(MeleeWeaponComponent.WideAnimation),
            nameof(MeleeWeaponComponent.AltDisarm),
            nameof(MeleeWeaponComponent.Range),
            nameof(MeleeWeaponComponent.Angle),
            nameof(MeleeWeaponComponent.HitSound),
        ]);

        Dirty(uid, melee);

        // Remove hands
        if (config.RemoveHands && TryComp<HandsComponent>(uid, out var hands))
        {
            _hands.RemoveHands(uid);
            RemComp(uid, hands);
        }
    }
}
