using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProiectASP.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ProiectASP.Controllers;
using ProiectASP.Models;
using System;
using System.Linq;
using Ganss.Xss;

namespace ProiectASP.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;



        public ArticlesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Index()
        {
            var articles = db.Articles.Include("Category")
                                      .Include("User").OrderBy(a => a.Date);

            var search = "";

            // MOTOR DE CAUTARE

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

                // Cautare in articol (Title si Content)

                List<int> articleIds = db.Articles.Where
                                        (
                                         at => at.Title.Contains(search)
                                         || at.Content.Contains(search)
                                        ).Select(a => a.Id).ToList();

                // Se formeaza o singura lista formata din toate id-urile selectate anterior
                List<int> mergedIds = articleIds.ToList();


                // Lista articolelor care contin cuvantul cautat
                // fie in articol -> Title si Content
                // fie in comentarii -> Content
                articles = db.Articles.Where(article => mergedIds.Contains(article.Id))
                                      .Include("Category")
                                      .Include("User")
                                      .OrderBy(a => a.Date);

            }

            ViewBag.SearchString = search;

            // AFISARE PAGINATA

            // Alegem sa afisam 3 articole pe pagina
            int _perPage = 3;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }


            // Fiind un numar variabil de articole, verificam de fiecare data utilizand 
            // metoda Count()
            int totalItems = 0;

            if (articles != null)
            {
                totalItems = articles.Count();
            }
            


            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Articles/Index?page=valoare

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3 
            // Asadar offsetul este egal cu numarul de articole care au fost deja afisate pe paginile anterioare
            var offset = 0;

            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            // Se preiau articolele corespunzatoare pentru fiecare pagina la care ne aflam 
            // in functie de offset
            var paginatedArticles = articles.Skip(offset).Take(_perPage);


            // Preluam numarul ultimei pagini

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            // Trimitem articolele cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Articles = paginatedArticles;

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Articles/Index/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Articles/Index/?page";
            }


            return View();
        }


        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show(int id)
        {
            Article article = db.Articles.Include("Category")
                                         .Include("User")
                                         .Where(art => art.Id == id)
                                         .First();


            SetAccessRights();

            return View(article);
        }


        [Authorize(Roles = "Editor,Admin")]
        public IActionResult New()
        {
            Article article = new Article();

            // Se preia lista de categorii din metoda GetAllCategories()
            article.Categ = GetAllCategories();


            return View(article);
        }

        // Se adauga articolul in baza de date
        // Doar utilizatorii cu rolul de Editor sau Admin pot adauga articole in platforma

        [Authorize(Roles = "Editor,Admin")]
        [HttpPost]

        public IActionResult New(Article article)
        {
            var sanitizer = new HtmlSanitizer();

            article.Date = DateTime.Now;
            article.UserId = _userManager.GetUserId(User);


            if (ModelState.IsValid)
            {
                article.Content = sanitizer.Sanitize(article.Content);

                db.Articles.Add(article);
                db.SaveChanges();
                TempData["message"] = "Articolul a fost adaugat";
                return RedirectToAction("Index");
            }
            else
            {
                article.Categ = GetAllCategories();
                return View(article);
            }
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllCategories()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var categories = from cat in db.Categories
                             select cat;

            // iteram prin categorii
            foreach (var category in categories)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = category.CategoryName.ToString()
                });
            }
            /* Sau se poate implementa astfel: 
             * 
            foreach (var category in categories)
            {
                var listItem = new SelectListItem();
                listItem.Value = category.Id.ToString();
                listItem.Text = category.CategoryName.ToString();

                selectList.Add(listItem);
             }*/


            // returnam lista de categorii
            return selectList;
        }

        [Authorize(Roles = "Editor,Admin")]
        public IActionResult Edit(int id)
        {

            Article article = db.Articles.Include("Category")
                                        .Where(art => art.Id == id)
                                        .First();

            article.Categ = GetAllCategories();

            if ((article.UserId == _userManager.GetUserId(User) && !article.Restricted) || User.IsInRole("Admin"))
            {
                return View(article);
            }

            else
            {
                if (article.Restricted && article.UserId == _userManager.GetUserId(User))
                    TempData["message"] = "Nu aveti dreptul sa modificati , acest articol este restrictionat";
                else
                    TempData["message"] = "Nu aveti dreptul sa modificati un articol care nu va apartine";
                return RedirectToAction("Index");
            }

        }


        // Se adauga articolul modificat in baza de date
        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public IActionResult Edit(int id, Article requestArticle)
        {
            Article article = db.Articles.Find(id);


            if (ModelState.IsValid)
            {
                if ((article.UserId == _userManager.GetUserId(User) && !article.Restricted )|| User.IsInRole("Admin"))
                {
                    article.Title = requestArticle.Title;
                    article.Content = requestArticle.Content;
                    article.CategoryId = requestArticle.CategoryId;
                    TempData["message"] = "Articolul a fost modificat";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    if (article.Restricted && article.UserId == _userManager.GetUserId(User))
                        TempData["message"] = "Nu aveti dreptul sa modificati , acest articol este restrictionat";
                    else
                        TempData["message"] = "Nu aveti dreptul sa modificati un articol care nu va apartine";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                requestArticle.Categ = GetAllCategories();
                return View(requestArticle);
            }
        }


        // Se sterge un articol din baza de date 
        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult Delete(int id)
        {
            Article article = db.Articles.Where(art => art.Id == id)
                                         .First();

            if ((article.UserId == _userManager.GetUserId(User) && !article.Restricted) || User.IsInRole("Admin"))
            {
                db.Articles.Remove(article);
                db.SaveChanges();
                TempData["message"] = "Articolul a fost sters";
                return RedirectToAction("Index");
            }

            else
            {
                if(article.Restricted && article.UserId == _userManager.GetUserId(User))
                    TempData["message"] = "Nu aveti dreptul sa stergeti , acest articol este restrictionat";
                else
                    TempData["message"] = "Nu aveti dreptul sa stergeti un articol care nu va apartine";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult Restriction(int id)
        {
            Article article = db.Articles.Where(art => art.Id == id)
                                         .First();

            //if ((article.UserId == _userManager.GetUserId(User) && !article.Restricted) || User.IsInRole("Admin"))
            //{
                article.Restricted=!article.Restricted;
                db.SaveChanges();
                TempData["message"] = "Restrictia a fost schimbata";
                return RedirectToAction("Index");
            //}

            //else
            //{
            //    TempData["message"] = "Nu aveti dreptul sa stergeti un articol care nu va apartine";
            //    return RedirectToAction("Index");
            //}
        }

        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Editor"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }
    }
}
