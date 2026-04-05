using Microsoft.EntityFrameworkCore;
using SkyBooker.Auth.Data;
using SkyBooker.Auth.Entities;

namespace SkyBooker.Auth.Repositories;

/// <summary>
/// EF Core implementation of IUserRepository.
/// Uses UsersDbContext for all database operations.
/// Registered as Scoped in Program.cs DI container.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(UsersDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<User?> FindByEmail(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User?> FindByUserId(int userId)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);
    }

    public async Task<bool> ExistsByEmail(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<IList<User>> FindAllByRole(string role)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.Role == role && u.IsActive)
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    public async Task<User?> FindByPhone(string phone)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Phone == phone);
    }

    public async Task<User?> FindByPassportNumber(string passportNumber)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PassportNumber == passportNumber);
    }

    public async Task<User?> FindByGoogleId(string googleId)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.GoogleId == googleId);
    }

    public async Task<IList<User>> FindAll()
    {
        return await _context.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<User> Save(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("User saved: {Email} (UserId={UserId})", user.Email, user.UserId);
        return user;
    }

    public async Task<User> Update(User user)
    {
        // Use FindAsync (tracked) so EF Core does not blindly overwrite all columns
        // when the entity was previously loaded with AsNoTracking.
        var tracked = await _context.Users.FindAsync(user.UserId);
        if (tracked == null)
            throw new KeyNotFoundException($"User {user.UserId} not found for update.");

        // Copy only the fields we allow to be updated
        _context.Entry(tracked).CurrentValues.SetValues(user);

        await _context.SaveChangesAsync();
        _logger.LogInformation("User updated: UserId={UserId}", user.UserId);
        return tracked;
    }

    public async Task DeleteByUserId(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogWarning("User DELETED: UserId={UserId}", userId);
        }
    }

    public async Task<int> CountAll()
    {
        return await _context.Users.CountAsync();
    }

    public async Task<int> CountByRole(string role)
    {
        return await _context.Users.CountAsync(u => u.Role == role && u.IsActive);
    }
}
