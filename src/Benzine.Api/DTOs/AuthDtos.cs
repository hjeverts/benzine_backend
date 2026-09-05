namespace Benzine.Api.DTOs;

public record RegisterRequest(string Email, string Password, string Name);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Email, string Name, string? AvatarDataUrl, bool IsAdmin);
public record ProfileResponse(string Email, string Name, string? AvatarDataUrl, bool IsAdmin);
public record UpdateProfileRequest(string Email, string Name);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record RequestPasswordResetRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record AdminUserResponse(Guid Id, string Email, string Name, bool IsAdmin, DateTime CreatedAt);
public record UpdateAdminUserRequest(string Email, string Name, bool IsAdmin);
