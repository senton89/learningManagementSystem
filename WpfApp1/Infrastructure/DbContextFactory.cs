using System;
using Microsoft.EntityFrameworkCore;

namespace WpfApp1.Infrastructure;

public class DbContextFactory<TContext> : IDbContextFactory<TContext> where TContext : DbContext
{
    private readonly Func<TContext> _contextFactory;

    public DbContextFactory(Func<TContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    public TContext CreateDbContext()
    {
        return _contextFactory();
    }
}