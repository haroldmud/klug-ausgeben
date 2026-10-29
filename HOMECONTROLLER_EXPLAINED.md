# HomeController.cs — Explained From the Inside Out

This document walks through every method in `HomeController.cs`, what it does,
what it connects to, and why it's wired up that way.

---

## The Big Picture First

```
Program.cs  ──registers──►  AppDbContext (the database bridge)
                                  ↓
                         HomeController  (receives it via constructor)
                           /    |    \
                      Index  Expenses  CreateEditExpense  ...
                        ↓       ↓            ↓
                    View()   View(list)   View(expense)
                        ↓       ↓            ↓
               Index.cshtml  Expenses.cshtml  CreateEditExpense.cshtml
```

ASP.NET's routing rule (from `Program.cs`) is:
```
/{controller}/{action}/{id?}
```
So `/Home/Expenses` → `HomeController` → `Expenses()` method.
No controller in the URL means `Home`. No action means `Index`.

---

## The Constructor — Dependency Injection

```csharp
private readonly AppDbContext _db;

public HomeController(AppDbContext db)
{
    _db = db;
}
```

`HomeController` needs the database but it doesn't create it — **ASP.NET creates it
and hands it in**. This is called *dependency injection*.

Why? Because `Program.cs` registered it:
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=expenses.db"));
```

That line says: "whenever someone needs an `AppDbContext`, build one pointed at
`expenses.db` and hand it over." The controller never needs to know where the
database file lives.

`AppDbContext` lives in `Data/AppDbContext.cs`. It has one property:
```csharp
public DbSet<Expense> Expenses { get; set; }
```
`_db.Expenses` = the `Expenses` table. `.ToList()`, `.Find()`, `.Add()`,
`.Remove()` are all operations on that table.

---

## `Index()` — The Home Page

```csharp
public IActionResult Index()
{
    return View();
}
```

**URL:** `/` or `/Home/Index`  
**Connected view:** `Views/Home/Index.cshtml`

The simplest action possible — no data needed, just render the page.
`View()` with no argument tells ASP.NET: "find the view that matches this
controller name + action name", which resolves to `Views/Home/Index.cshtml`.

`Index.cshtml` has two links that point back into the controller:
```html
<a asp-controller="Home" asp-action="Expenses">Overview</a>
<a asp-controller="Home" asp-action="CreateEditExpense">Create New</a>
```
`asp-controller` and `asp-action` are Razor tag helpers — they generate proper
`<a href="...">` links at render time. No hardcoded URLs.

---

## `Expenses()` — The List

```csharp
public IActionResult Expenses()
{
    var expenses = _db.Expenses.ToList();
    return View(expenses);
}
```

**URL:** `/Home/Expenses`  
**Connected view:** `Views/Home/Expenses.cshtml`

Queries the database for every expense row and passes the result **into the view**
as the model. `View(expenses)` is what makes the view's `@model` declaration work:

```html
@model List<klug_ausgeben.Models.Expense>
```

The view then loops over `Model` (the list the controller sent) and renders a table row per expense.

Each row has two actions wired back to the controller:

**Edit button:**
```html
<a asp-action="CreateEditExpense" asp-route-id="@expense.Id">
```
Generates a link like `/Home/CreateEditExpense?id=3` — hits the GET version of
`CreateEditExpense` with that id.

**Delete button:**
```html
<form asp-action="DeleteExpense" method="post">
    <input type="hidden" name="id" value="@expense.Id" />
```
A tiny form that POSTs to `DeleteExpense`. The hidden input carries the `id`
so the controller knows which row to delete.

---

## `CreateEditExpense(int id = 0)` — Create or Edit Form (GET)

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

**URL (create):** `/Home/CreateEditExpense`  
**URL (edit):** `/Home/CreateEditExpense?id=3`  
**Connected view:** `Views/Home/CreateEditExpense.cshtml`

`id = 0` is the default — no `id` in the URL means "new expense". The view
receives an empty `Expense` object and all fields are blank.

If an `id` is passed, the controller loads that row from the database and sends
it to the view. The form will be **pre-filled** because the view uses `asp-for`:

```html
<input asp-for="Value" />
<input asp-for="Description" />
```

`asp-for="Value"` binds to `Model.Value`, so the input gets the current value
automatically. The hidden `Id` field carries the id back on submit so the POST
handler knows whether this is a create or an update.

---

## `CreateEditExpenseForm(Expense model)` — Save (POST)

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

**URL:** POST to `/Home/CreateEditExpenseForm`  
**Connected view (on error):** `Views/Home/CreateEditExpense.cshtml`

This is what runs when the user hits **Save** in the form. ASP.NET reads the
submitted form fields and fills an `Expense` object automatically — this is called
*model binding*. You never read `Request.Form["Value"]` yourself.

**Step 1 — Validation.**  
`ModelState.IsValid` checks the rules declared on `Expense`:
```csharp
[Required] public decimal Value { get; set; }
[Required] public string? Description { get; set; }
```
If validation fails, the controller returns the same form view with the model
(and its errors) intact. The view shows error messages via `asp-validation-for`.

**Step 2 — Create or Update.**  
`model.Id == 0` means the database hasn't assigned an ID yet → new row.  
Otherwise the controller loads the existing row and updates only the fields the
form can change. It does NOT call `_db.Update(model)` directly — that would risk
overwriting fields that weren't in the form.

**Step 3 — Save and Redirect.**  
`_db.SaveChanges()` writes everything to `expenses.db`.  
`RedirectToAction("Expenses")` sends the browser to the list page (GET).
This prevents a form re-submission if the user hits refresh.

---

## `DeleteExpense(int id)` — Delete (POST)

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

**URL:** POST to `/Home/DeleteExpense`

`[HttpPost]` means this only responds to POST requests — you can't delete a row
by pasting a URL in the browser (that would be a GET). The form in
`Expenses.cshtml` is what triggers this.

Loads the row, removes it, saves, redirects back to the list. If the row doesn't
exist it does nothing — no crash.

---

## `Privacy()` and `Error()` — Boilerplate

```csharp
public IActionResult Privacy() => View();

[ResponseCache(...)]
public IActionResult Error()
{
    return View(new ErrorViewModel { RequestId = ... });
}
```

Generated by the ASP.NET template. `Privacy` just renders a static page.
`Error` is called by the exception handler middleware (configured in `Program.cs`)
when an unhandled error occurs anywhere in the app.

---

## Connection Map — Everything at a Glance

```
File                              Role
─────────────────────────────────────────────────────────
Program.cs                        Registers services, sets up routing
Data/AppDbContext.cs              EF Core bridge to expenses.db
Models/Expense.cs                 Data shape + validation rules
Controllers/HomeController.cs     Routes → database → view
Views/Home/Index.cshtml           Home page (no model)
Views/Home/Expenses.cshtml        Reads List<Expense> from controller
Views/Home/CreateEditExpense.cshtml  Reads/writes a single Expense
```

The `Expense` model is the thread connecting everything: the database table is
built from it, the form fields bind to it, and the controller passes it between
the database and the views.
