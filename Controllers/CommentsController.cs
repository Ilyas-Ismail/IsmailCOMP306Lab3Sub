using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using _301168447_Ismail__COMP306_Lab3.Data;
using _301168447_Ismail__COMP306_Lab3.Models;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using System.Security.Claims;

namespace _301168447_Ismail__COMP306_Lab3.Controllers
{
    public class CommentsController : Controller
    {

        public CommentsController()
        {
        }

        // GET: Comments
        public async Task<IActionResult> Index()
        {
            List<ScanCondition> conditions = new List<ScanCondition>();

            return View(await DynamoClient.context.ScanAsync<Comment>(conditions).GetRemainingAsync());
        }

        // GET: Comments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await DynamoClient.context.LoadAsync<Comment>(id);

            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // POST: Comments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CommentId,MovieId,UserId,MovieTitle,CommentContent,Rating,CommentTime")] Comment comment)
        {
            if (ModelState.IsValid)
            {
                var cookieId = Request.Cookies.Where(c => c.Key == "UserId");
                var cookieUser = Request.Cookies.Where(c => c.Key == "Username");

                if (String.IsNullOrEmpty(cookieId.FirstOrDefault().Value))
                {
                    return Redirect("/Users/Login");
                }

                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int epoch = (int)t.TotalSeconds;
                int uuid = epoch - 1666859900;
                comment.CommentId = uuid;
                comment.CommentTime = DateTime.Now.ToString();
                comment.UserId = int.Parse(cookieId.FirstOrDefault().Value);
                comment.Username = cookieUser.FirstOrDefault().Value;
                await DynamoClient.context.SaveAsync(comment);

                List<ScanCondition> conditions = new List<ScanCondition>();
                ScanCondition conditionMovie = new ScanCondition("MovieId", ScanOperator.Equal, comment.MovieId);
                conditions.Add(conditionMovie);
                List<Comment> comments = await DynamoClient.context.ScanAsync<Comment>(conditions).GetRemainingAsync();
                Movie movie = await DynamoClient.context.LoadAsync<Movie>(comment.MovieId);
                double rating = 0;

                foreach(Comment c in comments)
                {
                    rating += c.Rating;
                }

                if(comments.Count > 0)
                {
                    rating /= comments.Count;
                }

                movie.Rating = Math.Round(rating, 2);

                await DynamoClient.context.SaveAsync(movie);
                return Redirect("/Movies/Details/" + comment.MovieId);
            }
            return View(comment);
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            //if the comment is made by the user, and was made within 24 hours, allow them to edit it
            if (id == null)
            {
                return NotFound();
            }

            var cookie = Request.Cookies.Where(c => c.Key == "UserId");

            if (String.IsNullOrEmpty(cookie.FirstOrDefault().Value))
            {
                return Redirect("/Users/Login");
            }

            var comment = await DynamoClient.context.LoadAsync<Comment>(id);
            
            DateTime now = DateTime.Now;
            DateTime commentTime = DateTime.Parse(comment.CommentTime);
            TimeSpan ts = now - commentTime;

            if (comment == null)
            {
                return NotFound();
            }

            if (ts.TotalHours > 24 || int.Parse(cookie.FirstOrDefault().Value) != comment.UserId)
            {
                return Redirect("/Movies/Details/" + comment.MovieId);
            }

            return View(comment);
        }

        // POST: Comments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CommentId,MovieId,UserId,MovieTitle,CommentContent,Rating,CommentTime")] Comment comment)
        {
            if (id != comment.CommentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                DateTime now = DateTime.Now;
                try
                {
                    comment.Username = Request.Cookies.Where(c => c.Key == "Username").FirstOrDefault().Value;
                    comment.CommentTime = now.ToString();

                    List<ScanCondition> conditions = new List<ScanCondition>();
                    ScanCondition conditionMovie = new ScanCondition("MovieId", ScanOperator.Equal, comment.MovieId);
                    conditions.Add(conditionMovie);
                    List<Comment> comments = await DynamoClient.context.ScanAsync<Comment>(conditions).GetRemainingAsync();
                    Movie movie = await DynamoClient.context.LoadAsync<Movie>(comment.MovieId);
                    double rating = 0;

                    foreach (Comment c in comments)
                    {
                        rating += c.Rating;
                    }

                    if (comments.Count > 0)
                    {
                        rating /= comments.Count;
                    }

                    movie.Rating = Math.Round(rating, 2);

                    await DynamoClient.context.SaveAsync(movie);
                    await DynamoClient.context.SaveAsync(comment);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await DynamoClient.context.LoadAsync<Comment>(id) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(comment);
        }

        // GET: Comments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            //if the comment was posted within 24 hours and is made by the users, allow them to delete it
            if (id == null)
            {
                return NotFound();
            }

            var comment = await DynamoClient.context.LoadAsync<Comment>(id);

            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            //delete the comment and adjust the movie's rating
            Comment comment = await DynamoClient.context.LoadAsync<Comment>(id);
            await DynamoClient.context.DeleteAsync<Comment>(id);

            List<ScanCondition> conditions = new List<ScanCondition>();
            ScanCondition conditionMovie = new ScanCondition("MovieId", ScanOperator.Equal, comment.MovieId);
            conditions.Add(conditionMovie);
            List<Comment> comments = await DynamoClient.context.ScanAsync<Comment>(conditions).GetRemainingAsync();
            Movie movie = await DynamoClient.context.LoadAsync<Movie>(comment.MovieId);
            double rating = 0;

            foreach (Comment c in comments)
            {
                rating += c.Rating;
            }

            if (comments.Count > 0)
            {
                rating /= comments.Count;
            }

            movie.Rating = Math.Round(rating, 2);

            await DynamoClient.context.SaveAsync(movie);

            return Redirect("/Movies/Details/" + comment.MovieId);
        }

        private async Task<bool> CommentExists(int id)
        {
            bool exists = false;
            var comment = await DynamoClient.context.LoadAsync<Comment>(id);

            if (comment != null)
            {
                exists = true;
            }

            return exists;
        }

        public async Task<IActionResult> DeleteComment(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cookie = Request.Cookies.Where(c => c.Key == "UserId");

            if (String.IsNullOrEmpty(cookie.FirstOrDefault().Value))
            {
                return Redirect("/Users/Login");
            }

            var comment = await DynamoClient.context.LoadAsync<Comment>(id);
            
            DateTime now = DateTime.Now;
            DateTime commentTime = DateTime.Parse(comment.CommentTime);
            TimeSpan ts = now - commentTime;

            if (comment == null)
            {
                return NotFound();
            }

            if (ts.TotalHours > 24 || int.Parse(cookie.FirstOrDefault().Value) != comment.UserId)
            {
                return Redirect("/Movies/Details/" + comment.MovieId);
            }

            return View(comment);
        }

        [HttpPost, ActionName("DeleteComment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int id)
        {
            Comment comment = await DynamoClient.context.LoadAsync<Comment>(id);
            await DynamoClient.context.DeleteAsync<Comment>(id);

            List<ScanCondition> conditions = new List<ScanCondition>();
            ScanCondition conditionMovie = new ScanCondition("MovieId", ScanOperator.Equal, comment.MovieId);
            conditions.Add(conditionMovie);
            List<Comment> comments = await DynamoClient.context.ScanAsync<Comment>(conditions).GetRemainingAsync();
            Movie movie = await DynamoClient.context.LoadAsync<Movie>(comment.MovieId);
            double rating = 0;

            foreach (Comment c in comments)
            {
                rating += c.Rating;
            }

            if (comments.Count > 0)
            {
                rating /= comments.Count;
            }

            movie.Rating = Math.Round(rating, 2);

            await DynamoClient.context.SaveAsync(movie);
            return Redirect("/Movies/Details/" + comment.MovieId);
        }
    }
}
