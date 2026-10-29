# MVC Explained — Model, View, Controller

## The Core Idea

MVC splits your app into three jobs so each piece only has **one reason to exist**:

| Layer | Job | Asks itself |
|-------|-----|-------------|
| **Model** | Data + business rules | "What is the data and is it valid?" |
| **View** | Display | "How does it look?" |
| **Controller** | Traffic cop | "What should happen next?" |

---

## Controller — The Entry Point

The controller is the **first thing that runs** when a URL is hit.

```
User types: /Expenses/Create
              ↓
     ExpensesController.Create()
```

**Use a controller when you need to:**
- Receive an HTTP request (GET, POST, etc.)
- Decide which action to take
- Call services or the database
- Pass data to a view, or redirect somewhere

**It should NOT:**
- Contain business logic ("is this expense over budget?") — that belongs in a Model or service
- Build HTML — that belongs in a View

```csharp
// GOOD — controller is thin, just coordinates
public IActionResult Create(ExpenseViewModel vm)
{
    if (!ModelState.IsValid) return View(vm);
    _expenseService.Save(vm);
    return RedirectToAction("Index");
}
```

---

## Model — The Data and Its Rules

The model represents **what your app works with** and **what makes it valid**.

There are two flavors in ASP.NET MVC:

### 1. Domain Model (your actual data)
```csharp
public class Expense
{
    public int Id { get; set; }
    public string Title { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}
```

### 2. ViewModel (shaped for a specific view)
```csharp
public class ExpenseViewModel
{
    [Required]
    public string Title { get; set; }

    [Range(0.01, 100000)]
    public decimal Amount { get; set; }
}
```

**Use a Model when you need to:**
- Define what a "thing" looks like (properties)
- Add validation rules (`[Required]`, `[Range]`, etc.)
- Shape data specifically for one screen (ViewModel)

**It should NOT:**
- Know about HTTP requests
- Render HTML
- Decide what page to show next

---

## View — The Display

The view gets data from the controller and **only renders HTML**. That's it.

```html
<!-- Views/Expenses/Create.cshtml -->
@model ExpenseViewModel

<form asp-action="Create">
    <input asp-for="Title" />
    <input asp-for="Amount" />
    <button type="submit">Save</button>
</form>
```

**Use a View when you need to:**
- Display data to the user
- Build a form
- Show a list, a page, a partial component

**It should NOT:**
- Query the database directly
- Contain if-heavy business logic
- Do calculations — format at most, never compute

---

## The Flow, Visualized

```
[Browser] → GET /Expenses/Index
                 ↓
         [Controller] — fetches data, builds ViewModel
                 ↓
           [Model/ViewModel] — carries the data
                 ↓
             [View] — renders HTML using the ViewModel
                 ↓
         [Browser] ← HTML response
```

For a POST (form submit):
```
[Browser] → POST /Expenses/Create (form data)
                 ↓
         [Controller] — receives ViewModel, checks ModelState
                 ↓
           [Model] — validates via data annotations
                 ↓
         [Controller] — saves or returns with errors
                 ↓
         [Browser] ← redirect or re-render form
```

---

## One-Line Decision Rule

> **Does it receive a request?** → Controller  
> **Does it describe data or validate it?** → Model  
> **Does it show something?** → View  

If your controller is long, you have business logic that belongs in a service or model.  
If your view has C# logic beyond simple loops/ifs, you have logic that belongs in the controller or a ViewModel property.
