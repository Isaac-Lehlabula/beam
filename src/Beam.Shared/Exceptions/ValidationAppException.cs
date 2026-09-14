namespace Beam.Shared.Exceptions;

/// <summary>
/// A domain-level validation failure that isn't naturally expressed as a FluentValidation rule
/// on a request DTO (e.g. "file too large", "unsupported format" discovered while processing).
/// Mapped to 400 by GlobalExceptionHandler, same as FluentValidation.ValidationException.
/// </summary>
public sealed class ValidationAppException(string message) : Exception(message);
