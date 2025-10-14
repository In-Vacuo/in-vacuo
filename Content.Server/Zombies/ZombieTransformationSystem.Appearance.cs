using Content.Shared.Humanoid;
using Content.Shared.Zombies;

namespace Content.Server.Zombies;

/// <summary>
/// Handles appearance changes during zombification.
/// </summary>
public sealed partial class ZombieTransformationSystem
{
    private void TransformAppearance(EntityUid uid, ZombieComponent zombie, ZombieTransformationConfigComponent config)
    {
        // Only humanoids get appearance changes
        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        // Store original appearance for cloning restoration
        zombie.BeforeZombifiedSkinColor = humanoid.SkinColor;
        zombie.BeforeZombifiedEyeColor = humanoid.EyeColor;
        zombie.BeforeZombifiedCustomBaseLayers = new(humanoid.CustomBaseLayers);

        // Apply zombie appearance
        _humanoidAppearance.SetSkinColor(uid, zombie.SkinColor, verify: false, humanoid: humanoid);
        humanoid.EyeColor = zombie.EyeColor;

        // Modify visual layers
        _humanoidAppearance.SetBaseLayerId(uid, HumanoidVisualLayers.Tail, zombie.BaseLayerExternal, humanoid: humanoid);
        _humanoidAppearance.SetBaseLayerId(uid, HumanoidVisualLayers.HeadSide, zombie.BaseLayerExternal, humanoid: humanoid);
        _humanoidAppearance.SetBaseLayerId(uid, HumanoidVisualLayers.HeadTop, zombie.BaseLayerExternal, humanoid: humanoid);
        _humanoidAppearance.SetBaseLayerId(uid, HumanoidVisualLayers.Snout, zombie.BaseLayerExternal, humanoid: humanoid);

        // Update identity
        _nameMod.RefreshNameModifiers(uid);
        _identity.QueueIdentityUpdate(uid);

        Dirty(uid, zombie);
        Dirty(uid, humanoid);
    }
}
