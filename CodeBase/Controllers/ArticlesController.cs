using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using CodeBase.Models;
using System.Linq;
using System.Web.Security;
using CodeBase.Helper;
using System.Net;
using System.Globalization;
using System.ServiceModel.Syndication;
using CodeBase.Helper;
using Rotativa;

namespace CodeBase.Controllers
{
    public class ArticlesController : Controller
    {
        public CodeBaseContext context = new CodeBaseContext();
        public ICodeBaseMembership membership = new CodeBaseMembership();

        [HttpPost, ActionName("Rate")]
        [Authorize]
        public ActionResult Rate(int id, float value)
        {
            String currentUser = membership.LoggedInUser();
            User u = context.Users.Where(x => x.Username == currentUser).First();
            Rating r = context.Ratings.Where(x => x.ArticleId == id && x.UserId == u.UserId).FirstOrDefault();
            if (r == null)
                context.Ratings.Add(new Rating { ArticleId = id, UserId = u.UserId, Date = DateTime.Now, Value = (int)value });
            else
            {
                r.Value = (int)value;
            }
            context.SaveChanges();

            return Json(new { data = AverageRating(id) });
        }

        public ActionResult Feed()
        {
            var articles = context.Articles.OrderBy(pub => pub.Date).Take(15).ToList().Select(p => new SyndicationItem(p.Title, BBCodeHelper.Format(p.Content), new Uri(Url.Action("Details","Articles", new { id = p.ArticleId }, "http")).SetPort(80)));

            var feed = new SyndicationFeed("CodeBase", "Your source to knowledge", new Uri(Url.Action("Index", "Home", new { }, "http")).SetPort(80), articles);

            return new FeedResult(new Rss20FeedFormatter(feed));
        
        
        }

        [HttpPost, Authorize]
        public ActionResult AddComment(Comment c)
        {
            String currentUser = membership.LoggedInUser();
            c.UserId = context.Users.Where(x => x.Username == currentUser).Single().UserId;
            c.Date = DateTime.Now;
            if (TryValidateModel(c))
            {
                context.Comments.Add(c);
                context.SaveChanges();

                return Redirect(Request.UrlReferrer.ToString());
            }
            return Redirect(Request.UrlReferrer.ToString());
        }

        //
        // GET: /Articles/

        public ViewResult Index()
        {   
            return View(context.Articles.Include(article => article.Category).Include(article => article.Ratings).Include(article => article.Comments).Include(article => article.Files).ToList());
        }


        public ViewResult IndexAll()
        {
            return View("Index",context.ArticlesAll.Include(article => article.Category).Include(article => article.Ratings).Include(article => article.Comments).Include(article => article.Files).ToList());
        }

        [Authorize]
        [ValidateInput(false)]
        public ActionResult Preview(String data)
        {
            return Content(BBCodeHelper.Format(data));
        }

        public ActionResult Pdf(int id)
        {            
            return new ActionAsPdf("Details", new { id = id });
        }


        private float AverageRating(int id)
        {
            IEnumerable<Rating> ratings = context.Ratings.Where(x => x.ArticleId == id);
            float average = (float)ratings.Sum(x => x.Value) / ratings.Count();
            return average;
        }

        //
        // GET: /Articles/Details/5

        public ActionResult Details(int id, String title)
        {
            Article article = context.Articles.Include(x => x.Comments).Single(x => x.ArticleId == id);

            string realTitle = UrlEncoder.ToFriendlyUrl(article.Title);
            string urlTitle = (title ?? "").Trim().ToLower();
            if (realTitle != urlTitle)
            {
                string url = "/Articles/" + article.ArticleId + "/" + realTitle;
                return Redirect(url);
            }
            ViewBag.Rating = AverageRating(article.ArticleId);
            

            return View(article);
        }

        //
        // GET: /Articles/Create


        [Authorize]
        public ActionResult Create()
        {
            ViewBag.PossibleUsers = context.Users;
            ViewBag.PossibleCategories = context.Categories;
            return View();
        }

        //
        // POST: /Articles/Create



        [HttpPost, ValidateInput(false)]
        [Authorize]
        public ActionResult Create(Article article)
        {
            article.Date = DateTime.Now;
            String currentUser = membership.LoggedInUser();
            article.Approved = canApprove(context.Users.Where(x => x.Username == currentUser).FirstOrDefault());
            article.UserId = context.Users.FirstOrDefault(x => x.Username == currentUser).UserId;

            if (ModelState.IsValid)
            {
                context.ArticlesAll.Add(article);
                context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.PossibleUsers = context.Users;
            ViewBag.PossibleCategories = context.Categories;
            return View(article);
        }

        //
        // GET: /Articles/Edit/5


        [Authorize]
        public ActionResult Edit(int id)
        {
            Article article = context.Articles.Single(x => x.ArticleId == id);
            if (ModelHelpers.canEdit(article))
            {
                ViewBag.PossibleCategories = context.Categories;
                return View(article);
            }
            TempData["Message"] = "Not authorized";
            return Redirect(Request.UrlReferrer.ToString());
        }

        [Authorize]
        public ActionResult DeleteComment(int id)
        {
            Comment c = context.Comments.Find(id);
            if (ModelHelpers.canEdit(c))
            {
                context.Comments.Remove(c);
                context.SaveChanges();
                return Redirect(Request.UrlReferrer.ToString());
            }
            TempData["Message"] = "Not authorized";
            return Redirect(Request.UrlReferrer.ToString());
        }

        //
        // POST: /Articles/Edit/5




        [HttpPost, ValidateInput(false)]
        [Authorize]
        public ActionResult Edit(Article article)
        {
            var a = context.ArticlesAll.Find(article.ArticleId);
            if (ModelHelpers.canEdit(a))
            {
                if (ModelState.IsValid)
                {

                    article.UserId = a.UserId;
                    article.Date = a.Date;
                    context.Entry(a).State = EntityState.Detached;

                    context.Entry(article).State = EntityState.Modified;
                    context.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.PossibleUsers = context.Users;
                ViewBag.PossibleCategories = context.Categories;
                return View(article);
            }

            TempData["Message"] = "Not authorized";
            return Redirect(Request.UrlReferrer.ToString());
        }


        //
        // GET: /Articles/Delete/5
        [Authorize]
        public ActionResult Delete(int id)
        {

            Article article = context.Articles.Single(x => x.ArticleId == id);
            if (ModelHelpers.canEdit(article))
            {
                return View(article);
            }

            TempData["Message"] = "Not authorized";
            return Redirect(Request.UrlReferrer.ToString());
        }

        //
        // POST: /Articles/Delete/5

        [HttpPost, ActionName("Delete")]
        [Authorize]
        public ActionResult DeleteConfirmed(int id)
        {

            Article article = context.Articles.Single(x => x.ArticleId == id);
            if (ModelHelpers.canEdit(article))
            {
                foreach(Rating r in context.Ratings.Where(x => x.ArticleId==article.ArticleId)){
                    context.Ratings.Remove(r);
                }
                foreach(File f in context.Files.Where(x => x.ArticleId==article.ArticleId)){
                    context.Files.Remove(f);
                }
                foreach(Comment c in context.Comments.Where(x=> x.ArticleId==article.ArticleId)){
                    context.Comments.Remove(c);
                }
                context.ArticlesAll.Remove(article);
                context.SaveChanges();
                return RedirectToAction("Index");
            }
            TempData["Message"] = "Not authorized";
            return Redirect(Request.UrlReferrer.ToString());
        }

        private bool canApprove(User u)
        {
            //Editor status or higher
            if(Roles.GetRolesForUser().Intersect(new String[]{"admin","editor"}).Count()>0){
                return true;
            }
            if(u.Articles.Count()>=5){
                return true;
            }
            return false;
        }
    }
}