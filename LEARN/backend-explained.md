# Backend Explained

## The flow in one sentence

A request hits `Program.cs`'s pipeline → gets routed to a controller action → ASP.NET injects `AppDbContext` into the controller constructor → the action queries/mutates the SQLite file through EF Core → returns a view or redirect.

---

## 1. `Program.cs` — entry point and service wiring

Two jobs: **register services**, then **configure the middleware pipeline**.

### Registering services

```csharp
builder.Services.AddControllersWithViews();         // registers MVC
builder.Services.AddDbContext<AppDbContext>(...);   // registers the database
```

`AddDbContext` is a convenience wrapper — it registers `AppDbContext` with **scoped** lifetime under the hood (one instance per HTTP request, then disposed).

The three lifetimes:

| Method | Lifetime | When to use |
|---|---|---|
| `AddSingleton` | One instance for the whole app | Config readers, caches |
| `AddScoped` | One per HTTP request | DbContext, unit-of-work |
| `AddTransient` | New instance every time it's requested | Lightweight stateless services |

### Middleware pipeline

```
Request comes in
  → UseHttpsRedirection  (force https)
  → UseRouting           (match the URL to a controller)
  → UseAuthorization     (check permissions)
  → MapControllerRoute   (send to the right controller action)
```

The route pattern `"{controller=Home}/{action=Index}/{id?}"` means:

| URL | Resolves to |
|---|---|
| `/` | `HomeController.Index()` |
| `/Home/Expenses` | `HomeController.Expenses()` |
| `/Home/CreateEditExpense/5` | `HomeController.CreateEditExpense(5)` |

---

## 2. `AppDbContext.cs` — the database bridge

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Expense> Expenses { get; set; }
}
```

- `DbContext` is Entity Framework Core's class that manages the DB connection and change tracking.
- The constructor takes `DbContextOptions` — ASP.NET sees that parameter, looks up what was registered in `Program.cs`, and passes it in automatically. You never call `new AppDbContext(...)` yourself.
- `DbSet<Expense> Expenses` = the `Expenses` table. EF maps `Expense` properties → columns.

---

## 3. `HomeController.cs` — receives DI, talks to the DB

```csharp
public HomeController(AppDbContext db)  // ASP.NET injects this automatically
{
    _db = db;
}
```

Because `AppDbContext` was registered in `Program.cs`, ASP.NET constructs the controller per request and injects the same `AppDbContext` instance throughout.

### Common EF Core operations used here

| Call | SQL equivalent |
|---|---|
| `_db.Expenses.ToList()` | `SELECT * FROM Expenses` |
| `_db.Expenses.Find(id)` | `SELECT * FROM Expenses WHERE Id = id` |
| `_db.Expenses.Add(model)` | stages an `INSERT` |
| `_db.Expenses.Remove(expense)` | stages a `DELETE` |
| `_db.SaveChanges()` | flushes all staged changes to SQLite |

---

## 4. `Expense.cs` — the model / table shape

```csharp
public int Id { get; set; }           // primary key (EF detects "Id" by convention)
public decimal Value { get; set; }    // money amount
public string? Description { get; set; }
```

- `[Required]` triggers both server-side validation (`ModelState.IsValid` check in the controller) and EF schema constraints (NOT NULL column in SQLite).
- `Id` is auto-incremented by EF — when you do `_db.Expenses.Add(model)` with `Id == 0`, the database assigns the real Id on `SaveChanges()`.
