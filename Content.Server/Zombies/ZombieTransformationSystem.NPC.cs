using Content.Server.Mind;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Zombies;

namespace Content.Server.Zombies;

/// <summary>
/// Handles NPC and AI configuration during zombification.
/// </summary>
public sealed partial class ZombieTransformationSystem
{
    private void TransformNPC(EntityUid uid, ZombieComponent zombie, ZombieTransformationConfigComponent config)
    {
        // Clear existing factions if configured
        if (config.ClearExistingFactions)
        {
            _faction.ClearFactions(uid);
        }

        // Add zombie faction
        _faction.AddFaction(uid, config.Faction);

        // Configure HTN (Hierarchical Task Network) AI (server-only component, no need to dirty)
        var htn = EnsureComp<HTNComponent>(uid);
        htn.RootTask = new HTNCompoundTask() { Task = config.HtnTask };
        htn.Blackboard.SetValue(NPCBlackboard.Owner, uid);

        // Put NPC to sleep initially
        _npc.SleepNPC(uid);

        // Wake if no player mind (AI-controlled zombie)
        if (!_mind.TryGetMind(uid, out _, out _))
        {
            _npc.WakeNPC(uid, htn);
        }
    }
}
