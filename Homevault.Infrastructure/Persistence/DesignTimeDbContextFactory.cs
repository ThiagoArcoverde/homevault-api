using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Homevault.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HomeDbContext>
{
    public HomeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<HomeDbContext>();
        optionsBuilder.UseSqlite("Data Source=homevault.db");
        return new HomeDbContext(optionsBuilder.Options);
    }
}
