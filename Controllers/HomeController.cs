using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using klug_ausgeben.Models;
using klug_ausgeben.Data;

namespace klug_ausgeben.Controllers;

public class HomeController : Controller
{
    // ASP.NET injects AppDbContext here automatically — no "new AppDbContext()" needed.
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

    public IActionResult CreateEditExpense()
    {
        return View();
    }

    // Receives the submitted form, saves the expense, then redirects back to the list.
    [HttpPost]
    public IActionResult CreateEditExpenseForm(Expense model)
    {
        if (!ModelState.IsValid)
            return View("CreateEditExpense", model);

        // Id == 0 means it's a new expense (no existing ID assigned yet).
        if (model.Id == 0)
            _db.Expenses.Add(model);
        else
            _db.Expenses.Update(model);

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
