namespace SkyBooker.Auth.Entities;

/// <summary>
/// Role constants — used in [Authorize(Roles = "...")] attributes
/// </summary>
public static class UserRoles
{
    public const string Passenger    = "PASSENGER";
    public const string AirlineStaff = "AIRLINE_STAFF";
    public const string Admin        = "ADMIN";
}