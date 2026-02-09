#nullable enable

namespace Afrowave.AJIS.Core.Events;

/// <summary>
/// Base event emitted by AJIS components.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
public abstract record AjisEvent(DateTimeOffset Timestamp);

/// <summary>
/// Reports progress for a long-running operation.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Operation">The name of the operation in progress.</param>
/// <param name="Percent">Completion percentage from 0 to 100.</param>
/// <param name="ProcessedBytes">The number of processed bytes, when known.</param>
/// <param name="TotalBytes">The total bytes for the operation, when known.</param>
public sealed record AjisProgressEvent(
    DateTimeOffset Timestamp,
    string Operation,
    int Percent,
    long? ProcessedBytes,
    long? TotalBytes) : AjisEvent(Timestamp);

/// <summary>
/// Marks a named milestone during processing.
/// </summary>
/// <param name="Timestamp">The time the event occurred.</param>
/// <param name="Name">The milestone name.</param>
/// <param name="Detail">Optional details about the milestone.</param>
public sealed record AjisMilestoneEvent(
    DateTimeOffset Timestamp,
    string Name,
    string? Detail = null) : AjisEvent(Timestamp);