#nullable enable

namespace Afrowave.AJIS.Core.Events;

public abstract record AjisEvent(DateTimeOffset Timestamp);

public sealed record AjisProgressEvent(
    DateTimeOffset Timestamp,
    string Operation,
    int Percent,
    long? ProcessedBytes,
    long? TotalBytes) : AjisEvent(Timestamp);

public sealed record AjisMilestoneEvent(
    DateTimeOffset Timestamp,
    string Name,
    string? Detail = null) : AjisEvent(Timestamp);