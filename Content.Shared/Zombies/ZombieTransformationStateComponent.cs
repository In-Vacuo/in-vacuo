using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Zombies;

/// <summary>
/// Tracks the state of an entity's ongoing zombification process.
/// Automatically added when transformation begins and removed when completed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ZombieTransformationStateComponent : Component
{
    /// <summary>
    /// Current phase of the transformation
    /// </summary>
    [DataField]
    public TransformationPhase Phase = TransformationPhase.NotStarted;

    /// <summary>
    /// Handlers that have completed their transformation logic.
    /// Used to prevent duplicate handling.
    /// </summary>
    [DataField]
    public HashSet<string> CompletedHandlers = new();

    /// <summary>
    /// The configuration entity used for this transformation.
    /// If null, uses default configuration from ZombieComponent.
    /// </summary>
    [DataField]
    public EntityUid? ConfigurationEntity;

    /// <summary>
    /// Whether this transformation can still be cancelled.
    /// Set to false once transformation actually begins.
    /// </summary>
    [DataField]
    public bool Cancellable = true;

    /// <summary>
    /// Optional reason for cancellation if transformation was cancelled.
    /// </summary>
    [DataField]
    public string? CancellationReason;

    /// <summary>
    /// Whether the entity had a player mind at transformation start.
    /// Used for conditional logic in handlers.
    /// </summary>
    [DataField]
    public bool HadMind;

    /// <summary>
    /// The player session's UserID if entity had a mind.
    /// Used for sending messages and playing sounds.
    /// </summary>
    [DataField]
    public NetUserId? UserId;
}

/// <summary>
/// Phases of zombie transformation
/// </summary>
[Serializable, NetSerializable]
public enum TransformationPhase : byte
{
    /// <summary>
    /// Transformation has not started
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Pre-transformation checks and setup
    /// </summary>
    PreTransformation = 1,

    /// <summary>
    /// Active transformation - handlers are executing
    /// </summary>
    Transforming = 2,

    /// <summary>
    /// Post-transformation cleanup and finalization
    /// </summary>
    PostTransformation = 3,

    /// <summary>
    /// Transformation completed successfully
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Transformation was cancelled
    /// </summary>
    Cancelled = 5
}
