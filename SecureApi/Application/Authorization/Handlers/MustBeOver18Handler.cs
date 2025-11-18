using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SecureApi.Application.Authorization.Requirements;
using SecureApi.Infrastructure.Persistence;
using System.Security.Claims;

namespace SecureApi.Application.Authorization.Handlers;

/// <summary>
/// Authorization handler that checks if the authenticated user is 18 years or older.
/// Looks up user's birth date from database and calculates age.
/// </summary>
public class MustBeOver18Handler : AuthorizationHandler<MustBeOver18Requirement>
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the MustBeOver18Handler class.
    /// </summary>
    public MustBeOver18Handler(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Handles the authorization requirement by checking user's age.
    /// </summary>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MustBeOver18Requirement requirement)
    {
        // Get user ID from JWT claims (Subject claim stores the user ID)
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            // No valid user ID in claims - authorization fails
            return;
        }

        // Look up user from database
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            // User not found - authorization fails
            return;
        }

        // Calculate age based on birth date
        var today = DateTime.Today;
        var age = today.Year - user.BirthDate.Year;

        // Adjust age if birthday hasn't occurred this year
        if (user.BirthDate.Date > today.AddYears(-age))
        {
            age--;
        }

        // Check if user is 18 or older
        if (age >= 18)
        {
            // Authorization succeeds
            context.Succeed(requirement);
        }

        // If not 18+, authorization fails (implicit - don't call Succeed)
    }
}
