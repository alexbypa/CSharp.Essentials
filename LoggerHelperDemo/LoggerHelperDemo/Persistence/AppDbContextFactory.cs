using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LoggerHelperDemo.Persistence;
public class AppDbContextFactory
    : IDesignTimeDbContextFactory<AppDbContext> {
    public AppDbContext CreateDbContext(string[] args) {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Qui metti la tua connection string di test/DEV
        // Puoi anche leggerla da file JSON o da variabili d'ambiente
        var conn = "Data Source=10.0.1.111;Initial Catalog=PragmaticCasino;Persist Security Info=True;User ID=sa;Password=demo!100;Encrypt=False;";
        optionsBuilder.UseNpgsql(conn);

        return new AppDbContext(optionsBuilder.Options);
    }
}