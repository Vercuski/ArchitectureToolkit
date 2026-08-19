namespace ArchitectureToolkit.Infrastructure.Correlation;

/// <summary>
/// Ambient accessor for the current operation's correlation ID, backed by AsyncLocal so the
/// value flows implicitly through the entire async call chain of a single request — every
/// downstream await, MediatR handler, and repository call sees it without it being passed
/// as a parameter anywhere.
/// </summary>
public sealed class CorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string CorrelationId => Current.Value ?? string.Empty;

    public void Set(string correlationId)
    {
        Current.Value = correlationId;
    }
}
