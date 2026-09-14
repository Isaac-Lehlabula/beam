using Beam.Modules.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace Beam.Api.Controllers;

public record RegisterRequest(string Email, string Password, string DisplayName);

public record LoginRequest(string Email, string Password);

public record UserResponse(Guid Id, string Email, string DisplayName);

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<BeamUser> userManager,
    SignInManager<BeamUser> signInManager,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("Auth")]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var user = new BeamUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return ValidationProblem(BuildIdentityErrors(result));
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        logger.LogInformation("New user registered: {UserId}", user.Id);
        return Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
    }

    [HttpPost("login")]
    [EnableRateLimiting("Auth")]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var result = await signInManager.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        return Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
    }

    /// <summary>
    /// Reissues the auth cookie with fresh claims (notably "org" membership claims -- see
    /// BeamUserClaimsPrincipalFactory) without requiring a logout/login. Call this right after
    /// an action that changes the current user's own organization memberships.
    /// </summary>
    [HttpPost("refresh-claims")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> RefreshClaims()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        await signInManager.RefreshSignInAsync(user);
        return Ok(new UserResponse(user.Id, user.Email!, user.DisplayName));
    }

    private static ModelStateDictionary BuildIdentityErrors(IdentityResult result)
    {
        var modelState = new ModelStateDictionary();
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(error.Code, error.Description);
        }

        return modelState;
    }
}
