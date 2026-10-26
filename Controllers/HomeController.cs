using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using klug_ausgeben.Models;
using klug_ausgeben.Data;

namespace klug_ausgeben.Controllers;

public class HomeController : Controller
{
    // ASP.NET injects AppDbContext here automatically — no "new AppDbContext()" needed
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        return View();
    }

    // Load all expenses from the database and pass them to the view.
    public IActionResult Expenses()
    {
        var expenses = _db.Expenses.ToList();   
        return View(expenses);
    }

    // id is optional — no id means create, an id means edit.
    public IActionResult CreateEditExpense(int id = 0)
    {
        if (id == 0)
            return View(new Expense());

        var existing = _db.Expenses.Find(id);
        if (existing == null)
            return NotFound();

        return View(existing);
    }

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

    // Receives the submitted form, saves the expense, then redirects back to the list.
    [HttpPost]
    public IActionResult CreateEditExpenseForm(Expense model)
    {
        if (!ModelState.IsValid)
            return View("CreateEditExpense", model);

        // Id == 0 means it's a new expense — let the database assign the Id.
        if (model.Id == 0)
        {
            _db.Expenses.Add(model);
        }
        else
        {
            // Load the existing row first, then apply changes.
            // Calling Update() on an untracked entity with a non-existent Id causes a concurrency error.
            var existing = _db.Expenses.Find(model.Id);
            if (existing == null)
                return NotFound();

            existing.Value = model.Value;
            existing.Description = model.Description;
        }

        // Persist changes to the database file.
        _db.SaveChanges();

        return RedirectToAction("Expenses");
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
