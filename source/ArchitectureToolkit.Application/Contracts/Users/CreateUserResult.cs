namespace ArchitectureToolkit.Application.Contracts.Users;

/// <summary>
/// CreateUserCommand's result (ADR-0018) — the created row (same shape
/// the User Management tab's list already uses) plus how the invite
/// went, so the New User dialog can show either "invite sent" or
/// "share this link" without a second round trip.
/// </summary>
public sealed record CreateUserResult(UserManagementDto User, bool EmailSent, string? InviteLink);
