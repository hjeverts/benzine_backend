namespace Benzine.Api.DTOs;

public record RegisterRequest(string Email, string Password, string Name);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Email, string Name, string? AvatarDataUrl);
public record ProfileResponse(string Email, string Name, string? AvatarDataUrl);
public record UpdateProfileRequest(string Email, string Name);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record RequestPasswordResetRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
