using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProiectASP.Data;
using ProiectASP.Models;
using System.Data;

namespace ProiectASP.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext db;

        public CategoriesController(ApplicationDbContext context)
        {
            db = context;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            var categories  = db.Categories.ToList(); 

            ViewBag.Categories = categories;   
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Show(int id)
        {
            Category category = db.Categories.Where(category => category.Id == id)
                                             .First();

            //SetAccessRights();

            return View(category);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult New()
        {
            Category category = new Category();

            return View(category);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult New(Category category) 
        {
            var sanitizer = new HtmlSanitizer();

            if (ModelState.IsValid)
            {
                category.CategoryName = sanitizer.Sanitize(category.CategoryName);

                db.Categories.Add(category);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost adaugata";
                return RedirectToAction("Index");
            } else
            {
                return View(category);
            }
        }



        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {

            Category category = db.Categories.Where(category => category.Id == id)
                                             .First();

            //article.Categ = GetAllCategories();

            //if (article.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            //{
                return View(category);
            //}

            //else
            //{
            //    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
            //    return RedirectToAction("Index");
            //}

        }


        // Se adauga articolul modificat in baza de date
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, Category requestCategory)
        {
            Category category = db.Categories.Find(id);


            if (ModelState.IsValid)
            {
                //if (article.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
                //{
                    category.CategoryName = requestCategory.CategoryName;
                    TempData["message"] = "Categoria a fost modificat";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                //}
                //else
                //{
                //    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
                //    return RedirectToAction("Index");
                //}
            }
            else
            {
            //    requestArticle.Categ = GetAllCategories();
                return View(requestCategory);
            }
        }


        // Se sterge un articol din baza de date 
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            Category category = db.Categories.Where(category => category.Id == id)
                                             .First();

            //if (category.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            //{
            if (db.Articles.Where(article => article.CategoryId == id).Count() == 0)
            {
                db.Categories.Remove(category);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost stearsa";
                return RedirectToAction("Index");
            }
            //}

            else
            {
                TempData["message"] = "Categorie este folosita in articole existente";
                return RedirectToAction("Index");
            }
        }

    }
}
