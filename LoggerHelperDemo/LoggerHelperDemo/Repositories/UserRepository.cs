using LoggerHelperDemo.Entities;
using LoggerHelperDemo.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LoggerHelperDemo.Repositories;

public interface IUserRepository {
    Task AddAsync(User user);
    Task SaveChangesAsync();
}

public class UserRepository : IUserRepository {
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(User user)
        => await _db.Users.AddAsync(user);

    public async Task SaveChangesAsync()
        => await _db.SaveChangesAsync();

    public async Task<List<User>> getUserSavedOnLastMinutes(int minutes) => await _db.Users.Where(a => a.CreatedAt >= DateTime.UtcNow.Date.AddMinutes(-1)).ToListAsync();
}
