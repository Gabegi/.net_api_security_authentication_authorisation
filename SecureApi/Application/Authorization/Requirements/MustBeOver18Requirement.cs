using Microsoft.AspNetCore.Authorization;

namespace SecureApi.Application.Authorization.Requirements;

/// <summary>
/// Authorization requirement that the user must be 18 years or older.
/// Used for age-restricted content and features.
/// </summary>
public class MustBeOver18Requirement : IAuthorizationRequirement
{
    // Empty - logic in handler
}
