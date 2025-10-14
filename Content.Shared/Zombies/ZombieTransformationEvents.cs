namespace Content.Shared.Zombies;

/// <summary>
/// Raised when zombification is requested for an entity.
/// Can be cancelled by setting Cancelled = true.
/// Subscribe with high priority to prevent zombification.
/// </summary>
public sealed class ZombificationRequestedEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The entity being zombified
    /// </summary>
    public EntityUid Target { get; }

    /// <summary>
    /// Optional configuration entity override. If null, uses defaults from ZombieComponent.
    /// </summary>
    public EntityUid? ConfigurationEntity { get; }

    /// <summary>
    /// Reason for cancellation, if cancelled
    /// </summary>
    public string? CancellationReason { get; set; }

    public ZombificationRequestedEvent(EntityUid target, EntityUid? configurationEntity = null)
    {
        Target = target;
        ConfigurationEntity = configurationEntity;
    }
}

/// <summary>
/// Raised when zombification begins, after all pre-checks have passed.
/// Systems should subscribe to this event to perform their part of the transformation.
/// This is a directed event - only raised on the target entity.
/// </summary>
[ByRefEvent]
public readonly record struct ZombificationStartedEvent(EntityUid Target, EntityUid? ConfigurationEntity);

/// <summary>
/// Raised after all transformation handlers have completed their work.
/// Used for final cleanup and post-transformation logic.
/// This is a directed event - only raised on the target entity.
/// </summary>
[ByRefEvent]
public readonly record struct ZombificationCompletedEvent(EntityUid Target, EntityUid? ConfigurationEntity);

/// <summary>
/// Raised when zombification is cancelled before completion.
/// This is a directed event - only raised on the target entity.
/// </summary>
[ByRefEvent]
public readonly record struct ZombificationCancelledEvent(EntityUid Target, string Reason);

// Note: EntityZombifiedEvent already exists in ZombieEvents.cs
// It will be raised after ZombificationCompletedEvent for backward compatibility
