# Program.cs Explained

## What is it?

`Program.cs` is the **entry point** of the application — it's the first file that runs when you start the server. It has two jobs:

1. **Register services** — tell the app what it needs (controllers, database, etc.)
2. **Configure the pipeline** — define how incoming HTTP requests are handled, in what order

---

## The Two Phases

### Phase 1 — Building (before `var app = builder.Build()`)

```csharp
var builder = WebApplication.CreateBuilder(args);
```

Creates a `WebApplicationBuilder` — a setup object. Nothing is running yet, you're just configuring.

```csharp
builder.Services.AddControllersWithViews();
```

Registers MVC into the **DI container** (Dependency Injection). This is what makes controllers and Razor views work at all. Without this line, the app has no concept of controllers or views.

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=expenses.db"));
```

Registers `AppDbContext` as a service. This is **dependency injection** in action — instead of creating a `new AppDbContext()` yourself, you tell the container how to build one. When `HomeController` asks for an `AppDbContext` in its constructor, the framework creates and hands it one automatically.

```csharp
var app = builder.Build();
```

Seals the configuration and produces a runnable `WebApplication`. After this line you can no longer register services.

---

### Phase 2 — The Middleware Pipeline (after `builder.Build()`)

This is where you define the **middleware pipeline** — a chain of steps every HTTP request passes through, top to bottom, in order.

```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
```

In production only: unhandled exceptions get caught and redirected to the `/Home/Error` page. In development you get the detailed error page instead. HSTS tells browsers to always use HTTPS for this domain.

```csharp
app.UseHttpsRedirection();
```

Redirects any plain `http://` request to `https://`.

```csharp
app.UseRouting();
```

Enables the routing system — makes the app aware of URLs and which controller/action they map to.

```csharp
app.UseAuthorization();
```

Enables authorization checks (e.g. `[Authorize]` attributes on controllers). This project doesn't use login/auth yet, but the hook is in place.

```csharp
app.MapStaticAssets();
```

Serves static files from `wwwroot/` — CSS, JS, images, etc.

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
```

Registers the **default route pattern**. This tells the router how to translate a URL into a controller + action:

| URL | Controller | Action | id |
|---|---|---|---|
| `/` | Home | Index | — |
| `/Home/Expenses` | Home | Expenses | — |
| `/Home/CreateEditExpense/5` | Home | CreateEditExpense | 5 |

The `=Home` and `=Index` are defaults (used when the segment is missing). The `?` on `id` means it's optional.

```csharp
app.Run();
```

Starts the web server and blocks — the app listens for requests until you stop it.

---

## The Big Picture

```
Program.cs starts
      ↓
builder registers services (MVC, DbContext)
      ↓
builder.Build() locks in the config
      ↓
middleware pipeline is assembled (HTTPS → routing → auth → static files → routes)
      ↓
app.Run() — server is live, waiting for requests
      ↓
request comes in → travels through pipeline top to bottom → hits a controller action → returns a response
```

Every feature in the app (controllers, views, the database) is ultimately wired up here first.
