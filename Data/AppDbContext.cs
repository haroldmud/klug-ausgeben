using klug_ausgeben.Models;
using Microsoft.EntityFrameworkCore;

namespace klug_ausgeben.Data;

// DbContext = EF Core's bridge between your C# code and the database.
// Think of it as the "manager" that knows how to read/write your tables.
public class AppDbContext : DbContext
{
    // ASP.NET calls this constructor and passes in the options (e.g. which DB file to use).
    // ": base(options)" forwards them to the parent DbContext class.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSet<Expense> = one table in the database.
    // EF will create an "Expenses" table with columns matching the Expense model.
    public DbSet<Expense> Expenses { get; set; }
}
