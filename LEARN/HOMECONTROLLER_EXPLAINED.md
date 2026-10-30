# HomeController Explained

## What it is

`HomeController` is the single controller in this app. It sits between the database and the views — it receives a request, talks to the database if needed, and hands data to a view to render.

Every public method in it is an **action** — a URL maps to it, it runs, and it returns something (a view, a redirect, a 404).
An action is simply a public method inside a controller that handles a request.

---

## How a request reaches it

`Program.cs` registers this route pattern:

```
/{controller}/{action}/{id?}
```

So `/Home/Expenses` → `HomeController` → `Expenses()`. No controller segment defaults to `Home`, no action defaults to `Index`. The `?` on `id` means it's optional.

which means:
- ASP.NET Core conventionally removes the Controller suffix from a controller class name. For example, `HomeController → Home`.
- `{controller=Home}/{action=Index}/{id?}` in Program.cs defines the default route: when no controller or action is specified in the URL, ASP.NET Core uses Home and Index.
- `public class HomeController : Controller` inherits functionality from the Controller base class due to the `: Controller`.
- ASP.NET Core discovers the class as a controller because its name ends in Controller and it follows the controller discovery conventions.

---

## The Constructor — Dependency Injection

```csharp
private readonly AppDbContext _db;

public HomeController(AppDbContext db)
{
    _db = db;
}
```

The controller doesn't create its own database connection — ASP.NET builds one and passes it in when constructing the controller. This is **dependency injection**. It works because `Program.cs` registered `AppDbContext` as a service.

`_db.Expenses` is the `Expenses` table. Every database operation in the controller goes through it.

---

## The Actions

### `Index()` — Home page

```csharp
public IActionResult Index()
{
    return View();
}
```

No data needed. `View()` with no argument resolves to `Views/Home/Index.cshtml` by convention (controller name + action name).

---

### `Expenses()` — The list

```csharp
public IActionResult Expenses()
{
    var expenses = _db.Expenses.ToList();
    return View(expenses);
}
```

Fetches every row from the database and passes the list to `Views/Home/Expenses.cshtml` as its **model**. The view's `@model List<Expense>` declaration matches what the controller sends.

---

### `CreateEditExpense(int id = 0)` — The form (GET)

```csharp
public IActionResult CreateEditExpense(int id = 0)
{
    if (id == 0)
        return View(new Expense());

    var existing = _db.Expenses.Find(id);
    if (existing == null)
        return NotFound();

    return View(existing);
}
```

One action serves two purposes:

- **No `id`** → creates an empty `Expense` and sends it to the form. Fields are blank.
- **With `id`** → loads that row from the database and pre-fills the form.

The view uses `asp-for` bindings, so it renders whatever is in the model automatically — no conditional logic needed in the view.

---

### `CreateEditExpenseForm(Expense model)` — Save (POST)

```csharp
[HttpPost]
public IActionResult CreateEditExpenseForm(Expense model)
{
    if (!ModelState.IsValid)
        return View("CreateEditExpense", model);

    if (model.Id == 0)
    {
        _db.Expenses.Add(model);
    }
    else
    {
        var existing = _db.Expenses.Find(model.Id);
        if (existing == null) return NotFound();

        existing.Value = model.Value;
        existing.Description = model.Description;
    }

    _db.SaveChanges();
    return RedirectToAction("Expenses");
}
```

This is what runs when the user hits **Save**. ASP.NET reads the submitted form fields and populates the `Expense` parameter automatically — this is called **model binding**.

Three steps happen in order:

**1. Validate.** `ModelState.IsValid` checks the rules on the `Expense` model (`[Required]`, etc.). If invalid, it returns the form again with errors — the user sees what went wrong.

**2. Create or update.** `model.Id == 0` means no database ID yet → new row. Otherwise it loads the existing row and updates only the fields the form submitted. Updating directly on an untracked object can cause EF Core tracking conflicts, so the pattern here is load-then-modify.

**3. Save and redirect.** `_db.SaveChanges()` writes to the database. `RedirectToAction("Expenses")` sends the browser to the list via a new GET request — this prevents a duplicate submission if the user hits refresh.

---

### `DeleteExpense(int id)` — Delete (POST)

```csharp
[HttpPost]
public IActionResult DeleteExpense(int id)
{
    var expense = _db.Expenses.Find(id);
    if (expense != null)
    {
        _db.Expenses.Remove(expense);
        _db.SaveChanges();
    }

    return RedirectToAction("Expenses");
}
```

`[HttpPost]` means this action only responds to POST requests — a URL typed in the browser (GET) won't trigger it. The delete button in `Expenses.cshtml` is a small form that POSTs the `id` here.

If the row doesn't exist, it does nothing and redirects cleanly rather than crashing.

---

### `Privacy()` and `Error()`

`Privacy()` just renders a static view — no data involved.

`Error()` is called by the exception handler middleware (set up in `Program.cs`) when an unhandled error occurs anywhere in the app. It passes a `RequestId` to the view so the error can be identified in logs.

---

## Can There Be More Than One Controller?

Yes — and in most real apps there are many. `HomeController` is the only one here because the app only manages one resource (expenses). As the app grows, you'd add more controllers to keep things organised.

Each controller owns a **slice of the app** — its own URLs, its own views folder, its own logic.

If you added an `AuthController` for login/logout, the structure would look like:

```
Controllers/
├── HomeController.cs       → /Home/...
└── AuthController.cs       → /Auth/...

Views/
├── Home/
│   ├── Index.cshtml
│   └── Expenses.cshtml
└── Auth/
    ├── Login.cshtml
    └── Register.cshtml
```

The routing rule from `Program.cs` handles this automatically:

```
/{controller}/{action}/{id?}
```

`/Auth/Login` would route to `AuthController` → `Login()` with no extra configuration needed. ASP.NET discovers controllers by scanning for classes that inherit from `Controller`.

A few conventions to know when you add a second controller:

- The class name **must** end in `Controller` (e.g. `AuthController`) — ASP.NET uses this to find it
- Views go in `Views/{ControllerNameWithoutSuffix}/` — so `AuthController` views live in `Views/Auth/`
- Each controller gets its own `AppDbContext` injected independently — the DI container handles it, nothing extra to wire up
- `_Layout.cshtml` and `_ViewImports.cshtml` are shared across all controllers automatically

So right now the app has one controller because it has one concern. That's correct — not a limitation.

---

## Connection Map

```
URL arrives
    ↓
Route matches HomeController + action name
    ↓
Constructor receives AppDbContext via DI
    ↓
Action queries / mutates _db
    ↓
return View(data)  →  Razor renders the .cshtml with the model
    ↓
HTML response sent to browser
```

| Action | HTTP | View | Reads DB | Writes DB |
|---|---|---|---|---|
| `Index` | GET | Index.cshtml | No | No |
| `Expenses` | GET | Expenses.cshtml | Yes | No |
| `CreateEditExpense` | GET | CreateEditExpense.cshtml | Sometimes | No |
| `CreateEditExpenseForm` | POST | — (redirects) | Sometimes | Yes |
| `DeleteExpense` | POST | — (redirects) | Yes | Yes |
| `Privacy` | GET | Privacy.cshtml | No | No |
| `Error` | GET | Error.cshtml | No | No |
