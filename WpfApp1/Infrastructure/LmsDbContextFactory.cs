using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WpfApp1.Infrastructure;

public class LmsDbContextFactory : IDesignTimeDbContextFactory<LmsDbContext>
{
    public LmsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LmsDbContext>();
        
        var dir = AppDomain.CurrentDomain.BaseDirectory.Remove(
            AppDomain.CurrentDomain.BaseDirectory.LastIndexOf("bin", StringComparison.Ordinal));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(dir)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        
        optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

        return new LmsDbContext(optionsBuilder.Options);
    }
}