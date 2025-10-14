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
