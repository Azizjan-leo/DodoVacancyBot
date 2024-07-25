using DAL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Admin.Controllers;
public class VacancyController(ApplicationDbContext _context) : Controller
{
    // GET: VacancyController
    public ActionResult Index()
    {
        return View();
    }

    // GET: VacancyController/Details/5
    public ActionResult Details(int id)
    {
        return View();
    }

    // GET: VacancyController/Create
    public ActionResult Create()
    {
        return View();
    }

    // POST: VacancyController/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Create(IFormCollection collection)
    {
        try
        {
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View();
        }
    }

    // GET: VacancyController/Edit/5
    public ActionResult Edit(int id)
    {
        return View();
    }

    // POST: VacancyController/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Edit(int id, IFormCollection collection)
    {
        try
        {
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View();
        }
    }

    // GET: VacancyController/Delete/5
    public ActionResult Delete(int id)
    {
        return View();
    }

    // POST: VacancyController/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult Delete(int id, IFormCollection collection)
    {
        try
        {
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            return View();
        }
    }
}
