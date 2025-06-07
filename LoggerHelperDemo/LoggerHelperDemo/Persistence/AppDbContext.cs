using LoggerHelperDemo.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoggerHelperDemo.Persistence;
public class AppDbContext : DbContext {
    public DbSet<User> Users { get; set; } = default!;

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) {
    }
}
