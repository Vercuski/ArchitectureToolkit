namespace ArchitectureToolkit.Presentation.API.Controllers.Requests;

public sealed record SetPasswordRequest(string Email, string Token, string NewPassword, string ConfirmPassword);
