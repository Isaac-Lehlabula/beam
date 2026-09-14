namespace Beam.Shared.Exceptions;

/// <summary>
/// Thrown when the current user is authenticated and the resource exists, but an explicit
/// business rule (role permission, locked template element) forbids the action.
/// </summary>
public sealed class ForbiddenAccessException(string message) : Exception(message);
