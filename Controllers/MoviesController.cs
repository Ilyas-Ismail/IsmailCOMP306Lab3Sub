using System;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using _301168447_Ismail__COMP306_Lab3.Data;
using _301168447_Ismail__COMP306_Lab3.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Amazon;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using System.Collections.ObjectModel;
using System.IO;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace _301168447_Ismail__COMP306_Lab3.Controllers
{
    public class MoviesController : Controller
    {
        string bucketName = "stazismailcomp306lab3";
        public MoviesController()
        {
        }

        // GET: Movies
        public async Task<IActionResult> Index(string genre, double rating)
        {
            List<ScanCondition> conditions = new List<ScanCondition>();

            //Run different queries based on user inputs
            if (String.IsNullOrEmpty(genre) && (double.IsNaN(rating) || rating == 0))
            {
                return View(await DynamoClient.context.ScanAsync<Movie>(conditions).GetRemainingAsync());
            }

            if (String.IsNullOrEmpty(genre) && !double.IsNaN(rating))
            {
                ScanCondition condition2 = new ScanCondition("Rating", ScanOperator.GreaterThanOrEqual, rating);
                conditions.Add(condition2);
                return View(await DynamoClient.context.ScanAsync<Movie>(conditions).GetRemainingAsync());
            }

            if (!String.IsNullOrEmpty(genre) && (double.IsNaN(rating) || rating == 0))
            {
                ScanCondition condition2 = new ScanCondition("Genre", ScanOperator.Contains, genre);
                conditions.Add(condition2);
                return View(await DynamoClient.context.ScanAsync<Movie>(conditions).GetRemainingAsync());
            }

            ScanCondition conditionGenre = new ScanCondition("Genre", ScanOperator.Contains, genre);
            ScanCondition conditionRating = new ScanCondition("Rating", ScanOperator.GreaterThanOrEqual, rating);
            conditions.Add(conditionGenre);
            conditions.Add(conditionRating);

            return View(await DynamoClient.context.ScanAsync<Movie>(conditions).GetRemainingAsync());
        }

        // GET: Movies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            //Display movie details along with all comments made on the movie
            List<ScanCondition> conditions = new List<ScanCondition>();
            ScanCondition condition = new ScanCondition("MovieId", ScanOperator.Equal, id);
            conditions.Add(condition);

            var movie = await DynamoClient.context.LoadAsync<Movie>(id);
            List<Comment> comments = await DynamoClient.context.ScanAsync<Comment>(conditions).GetRemainingAsync();
            comments = comments.OrderByDescending(c => c.CommentTime).ToList();

            Combined combined = new Combined();
            combined.Movie = movie;
            combined.Comments = comments;

            if (movie == null)
            {
                return NotFound();
            }

            return View(combined);
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            Movie movie = new Movie();

            var cookie = Request.Cookies.Where(c => c.Key == "UserId");
            if (String.IsNullOrEmpty(cookie.FirstOrDefault().Value))
            {
                return Redirect("/Users/Login");
            }

            return View(movie);
        }

        // POST: Movies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MovieId,UserId,Title,Genre,Director,Cast,Description,Duration,ReleaseDate,UploadDate,Rating")] Movie movie, [FromForm] IFormFile file)
        {
            if (ModelState.IsValid)
            {
                //Create a movie, upload the files onto S3 bucket, save metadata to Dynamodb
                var request = new PutObjectRequest()
                {
                    BucketName = bucketName,
                    Key = file.FileName,
                    InputStream = file.OpenReadStream(),
                    ContentType = file.ContentType
                };
                var result = await Client.s3Client.PutObjectAsync(request);

                var cookieId = Request.Cookies.Where(c => c.Key == "UserId");
                var cookieName = Request.Cookies.Where(c => c.Key == "Username");
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int epoch = (int)t.TotalSeconds;
                int uuid = epoch - 1666859900;
                movie.MovieId = uuid;
                movie.FileName = file.FileName;
                movie.UploadDate = DateTime.Now.ToString();
                movie.UserId = int.Parse(cookieId.FirstOrDefault().Value);
                movie.Username = cookieName.FirstOrDefault().Value;

                await DynamoClient.context.SaveAsync(movie);
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var cookieId = Request.Cookies.Where(c => c.Key == "UserId");

            if (String.IsNullOrEmpty(cookieId.FirstOrDefault().Value))
            {
                return Redirect("/Users/Login");
            }

            var movie = await DynamoClient.context.LoadAsync<Movie>(id);
            

            if (movie == null)
            {
                return NotFound();
            }
            

            if (int.Parse(cookieId.FirstOrDefault().Value) != movie.UserId)
            {
                return Redirect("/Movies");
            }
            return View(movie);
        }

        // POST: Movies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MovieId,UserId,Title,Genre,Director,Cast,Description,Duration,ReleaseDate,UploadDate,Rating")] Movie movie, [FromForm] IFormFile file)
        {
            //Edit anything asides from Movie File and Duration. At that point it is a new movie
            if (id != movie.MovieId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    movie.UploadDate = DateTime.Now.ToString();
                    await DynamoClient.context.SaveAsync(movie);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await DynamoClient.context.LoadAsync<Movie>(id) == null)
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
            return View(movie);
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
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

            var movie = await DynamoClient.context.LoadAsync<Movie>(id);
            
            if (movie == null)
            {
                return NotFound();
            }

            if (int.Parse(cookie.FirstOrDefault().Value) != movie.UserId)
            {
                return Redirect("/Movies/Details/" + movie.MovieId);
            }

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string fileName)
        {
            //Delete the movie, any comment directly related to it, and the file from the bucket
            await DynamoClient.context.DeleteAsync<Movie>(id);

            List<ScanCondition> conditions = new List<ScanCondition>();
            ScanCondition conditionMovie = new ScanCondition("MovieId", ScanOperator.Equal, id);
            conditions.Add(conditionMovie);
            List<Comment> comments = await DynamoClient.context.ScanAsync<Comment>(conditions).GetRemainingAsync();

            foreach (Comment c in comments)
            {
                await DynamoClient.context.DeleteAsync<Comment>(c.CommentId);
            }

            var request = new DeleteObjectRequest()
            {
                BucketName = bucketName,
                Key = fileName
            };

            var response = await Client.s3Client.DeleteObjectAsync(request);

            return RedirectToAction(nameof(Index));
        }

        // POST: Movies/Delete/5
        [HttpGet, ActionName("Download")]
        public async Task<IActionResult> Download(int id, string fileName)
        {   
            //Download directly from the bucket based on file name

            var request = new GetObjectRequest()
            {
                BucketName = bucketName,
                Key = fileName
            };

            GetObjectResponse response = await Client.s3Client.GetObjectAsync(request);
            Stream responseStream = response.ResponseStream;
            var stream = new MemoryStream();
            await responseStream.CopyToAsync(stream);
            stream.Position = 0;

            return new FileStreamResult(stream, response.Headers["Content-Type"])
            {
                FileDownloadName = fileName
            };
        }

        private async Task<bool> MovieExists(int id)
        {
            bool exists = false;
            var movie = await DynamoClient.context.LoadAsync<Movie>(id);

            if(movie != null)
            {
                exists = true;
            }

            return exists;
        }
    }
}
