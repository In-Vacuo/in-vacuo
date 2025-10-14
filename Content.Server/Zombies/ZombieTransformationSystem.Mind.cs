using Content.Server.Chat.Managers;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Zombies;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Zombies;

/// <summary>
/// Handles mind and role changes during zombification.
/// </summary>
public sealed partial class ZombieTransformationSystem
{
    private void TransformMind(EntityUid uid, ZombieComponent zombie, ZombieTransformationConfigComponent config)
    {
        // Make sentient if configured
        if (config.MakeSentient)
        {
            _mind.MakeSentient(uid);
        }

        // Check for existing player mind
        var hasMind = _mind.TryGetMind(uid, out var mindId, out var mind);

        if (hasMind && mind != null && _player.TryGetSessionById(mind.UserId, out var session))
        {
            // Has player mind - give them zombie role
            _role.MindAddRole(mindId, config.MindRole, mind: null, silent: true);

            // Send greeting message
            _chatMan.DispatchServerMessage(session, Loc.GetString("zombie-infection-greeting"));

            // Play transformation sound
            _audio.PlayGlobal(zombie.GreetSoundNotification, session);
        }
        else
        {
            // No player - setup ghost role for potential takeover
            SetupGhostRole(uid, zombie, config, hasMind);
        }
    }

    private void SetupGhostRole(EntityUid uid, ZombieComponent zombie, ZombieTransformationConfigComponent config, bool hasMind)
    {
        // Setup for ghost role takeover (server-only component, no need to dirty)
        if (!HasComp<GhostRoleMobSpawnerComponent>(uid) && !hasMind)
        {
            var ghostRole = EnsureComp<GhostRoleComponent>(uid);
            EnsureComp<GhostTakeoverAvailableComponent>(uid);

            ghostRole.RoleName = Loc.GetString("zombie-generic");
            ghostRole.RoleDescription = Loc.GetString("zombie-role-desc");
            ghostRole.RoleRules = Loc.GetString("zombie-role-rules");
            ghostRole.MindRoles.Add(config.MindRole);
        }
    }
}
