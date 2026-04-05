using SkyBooker.Auth.Entities;

namespace SkyBooker.Auth.Repositories;

/// <summary>
/// Repository interface for User data access operations.
/// All methods are async (Task[T]) following EF Core best practices.
/// Implemented by UserRepository using UsersDbContext.
/// </summary>
public interface IUserRepository
{
    Task<User?> FindByEmail(string email);

    /// <summary>Find a user by their primary key UserId. Returns null if not found.</summary>
    Task<User?> FindByUserId(int userId);

    /// <summary>Check if a user with the given email already exists.</summary>
    Task<bool> ExistsByEmail(string email);

    /// <summary>Get all users with a specific role (PASSENGER / AIRLINE_STAFF / ADMIN).</summary>
    Task<IList<User>> FindAllByRole(string role);

    /// <summary>Find a user by phone number. Returns null if not found.</summary>
    Task<User?> FindByPhone(string phone);

    /// <summary>Find a user by passport number. Returns null if not found.</summary>
    Task<User?> FindByPassportNumber(string passportNumber);

    /// <summary>Find a user by Google OAuth sub claim (GoogleId).</summary>
    Task<User?> FindByGoogleId(string googleId);

    /// <summary>Get all users (Admin use only).</summary>
    Task<IList<User>> FindAll();

    /// <summary>Save a new user to the database. Returns the saved user with generated UserId.</summary>
    Task<User> Save(User user);

    /// <summary>Update an existing user. Returns the updated user.</summary>
    Task<User> Update(User user);

    /// <summary>Hard-delete a user by UserId. Use with caution — prefer deactivation.</summary>
    Task DeleteByUserId(int userId);

    /// <summary>Count total users. Used for admin analytics.</summary>
    Task<int> CountAll();

    /// <summary>Count active users by role.</summary>
    Task<int> CountByRole(string role);
}
