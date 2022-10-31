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

namespace _301168447_Ismail__COMP306_Lab3.Controllers
{
    public class UsersController : Controller
    {
        //Registration had to be done with Dynamodb, I did not have access to RDS
        public UsersController()
        {
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserId,Username,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                //create user, add it to dynamodb
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                int epoch = (int)t.TotalSeconds;
                int uuid = epoch - 1666859900;

                user.UserId = uuid;
                await DynamoClient.context.SaveAsync(user);
                return Redirect("/Users/Login");
            }
            return View(user);
        }

        // GET: Users/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([Bind("UserId,Username,Password")] User user)
        {
            if (ModelState.IsValid)
            {
                //authenticate user
                List<ScanCondition> conditions = new List<ScanCondition>();
                ScanCondition conditionMovie = new ScanCondition("Username", ScanOperator.Equal, user.Username);
                conditions.Add(conditionMovie);
                List<User> users = await DynamoClient.context.ScanAsync<User>(conditions).GetRemainingAsync();

                if(users.FirstOrDefault().Username == user.Username && users.FirstOrDefault().Password == user.Password)
                {
                    /*these cookies are unsafe, I wanted to use ASP.Identity with RDS, but I had to use Dynamodb,
                    I'm sure you can integrate Dynamodb with Identity, I did not have enough time to learn the process*/
                    string cookieId = "UserId";
                    string cookieValueId = users.FirstOrDefault().UserId.ToString();
                    string cookieName = "Username";
                    string cookieValueName = user.Username;

                    Response.Cookies.Append(cookieId, cookieValueId);
                    Response.Cookies.Append(cookieName, cookieValueName);
                    var cookie = Request.Cookies.Where(c => c.Key == cookieId);
                    string id = cookie.FirstOrDefault().Value;

                    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                    int epoch = (int)t.TotalSeconds;
                    int uuid = epoch - 1666859900;

                    user.UserId = uuid;
                    return Redirect("/Movies");
                }
                return View(user);
            }
            return View(user);
        }
    }
}
