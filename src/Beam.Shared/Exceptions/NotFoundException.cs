namespace Beam.Shared.Exceptions;

/// <summary>
/// Thrown when a resource doesn't exist OR isn't accessible to the current tenant.
/// Deliberately not distinguished from "forbidden" at the throw site — returning 404 either way
/// avoids confirming to a caller that a resource they can't access exists at all.
/// </summary>
public sealed class NotFoundException(string resourceName, object key)
    : Exception($"{resourceName} '{key}' was not found.");
