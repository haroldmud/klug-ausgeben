# Razor Views Explained

## What is Razor?

Razor is a **templating syntax** built into ASP.NET Core. It lets you mix C# code directly into HTML files. Those files use the `.cshtml` extension (C# + HTML). The server processes them, runs the C# parts, and sends back plain HTML to the browser — the browser never sees any C#.

The `@` symbol is the entry point into C# territory. Everything before `@` is HTML, everything after is C#.

---

## The Files in This Project

```
Views/
├── _ViewImports.cshtml       ← global imports for all views
├── _ViewStart.cshtml         ← tells every view which layout to use
├── Shared/
│   ├── _Layout.cshtml        ← the master HTML shell (nav, footer, scripts)
│   └── _ValidationScriptsPartial.cshtml
└── Home/
    ├── Index.cshtml          ← home page (no model)
    ├── Expenses.cshtml       ← list view (List<Expense> model)
    ├── CreateEditExpense.cshtml  ← form view (single Expense model)
    └── Privacy.cshtml
```

---

## How They're Connected

### 1. `_ViewStart.cshtml` — the automatic wrapper

Every view in the project inherits this file silently. It says:

```csharp
@{
    Layout = "_Layout";
}
```

That one line tells Razor: *before rendering any view, wrap it inside `_Layout.cshtml`*. You don't have to write this in each view — it applies automatically.

### 2. `_Layout.cshtml` — the master shell

This is the outer HTML frame that every page shares: the `<html>`, `<head>`, navbar, footer, and script tags. The key call inside it is:

```html
@RenderBody()
```

This is a **render slot** — it's where the content of the actual view (e.g. `Expenses.cshtml`) gets injected at runtime. Think of `_Layout` as a picture frame and `@RenderBody()` as the hole where the picture goes.

There's also:

```csharp
@await RenderSectionAsync("Scripts", required: false)
```

This is a **named section slot**. Views can optionally push extra `<script>` tags up into the layout using `@section Scripts { ... }`. `CreateEditExpense.cshtml` uses this to inject the client-side validation scripts only on that page.

### 3. `_ViewImports.cshtml` — shared namespaces and Tag Helpers

This file runs before every view and sets up two things:

```csharp
@using klug_ausgeben
@using klug_ausgeben.Models
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

- The `@using` lines make your models available without needing to fully qualify them in each view.
- `@addTagHelper` activates **Tag Helpers** — the `asp-*` attributes you see throughout the views (`asp-action`, `asp-for`, `asp-controller`). Without this line those attributes would just be inert HTML.

---

## The `@model` Directive — Strongly Typed Views

At the top of some views you'll see:

```csharp
@model List<klug_ausgeben.Models.Expense>   // Expenses.cshtml
@model klug_ausgeben.Models.Expense          // CreateEditExpense.cshtml
```

This declares what type of data the controller is expected to hand this view. Once declared, `Model` is available as a typed C# object inside the view — no casting needed.

The controller passes the data via `return View(data)`:

```csharp
// HomeController.cs
public IActionResult Expenses()
{
    var expenses = _db.Expenses.ToList();
    return View(expenses);           // hands List<Expense> to Expenses.cshtml
}
```

The view then uses it:

```html
@foreach (var expense in Model)
{
    <td>@expense.Value</td>
}
```

If a view has no `@model` directive (like `Index.cshtml`), it's an **untyped view** — it still renders but can't access a strongly typed `Model`.

---

## Tag Helpers — HTML That Knows About Your App

Tag Helpers are the `asp-*` attributes on HTML elements. They're Razor's way of generating correct, safe URLs and form bindings without hardcoding strings.

| Tag Helper | What it does |
|---|---|
| `asp-controller="Home"` | Targets the `HomeController` |
| `asp-action="Expenses"` | Targets the `Expenses()` method |
| `asp-route-id="@expense.Id"` | Appends `?id=3` (or `/3`) to the URL |
| `asp-for="Value"` | Binds a field to the `Value` property on the model — generates the `name`, `id`, and current value automatically |
| `asp-validation-for="Value"` | Renders the validation error message for that field |
| `asp-validation-summary="All"` | Renders all validation errors at the top of the form |
| `asp-append-version="true"` | Adds a cache-busting hash to the CSS/JS URL |

---

## `@ViewData` — Passing Small Values Without a Model

`ViewData` is a dictionary you can use to pass small pieces of data between the controller/view and the layout. In this project it's used for the page title:

```csharp
// inside a view
@{ ViewData["Title"] = "Expenses"; }

// inside _Layout.cshtml
<title>@ViewData["Title"] - klug_ausgeben</title>
```

The layout reads the title that each view set, so every tab in the browser shows the right page name.

---

## The Full Request Flow

```
Browser request
      ↓
HomeController.Expenses()
      ↓ queries database
      ↓ return View(expenses)
      ↓
Razor engine picks up Expenses.cshtml
      ↓ wraps it in _Layout.cshtml (via _ViewStart)
      ↓ injects namespaces (_ViewImports)
      ↓ executes all C# / Tag Helpers
      ↓
Plain HTML response sent to browser
```

No C# ever leaves the server. The browser only receives the final rendered HTML.
