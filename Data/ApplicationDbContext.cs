using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using _301168447_Ismail__COMP306_Lab3.Models;

namespace _301168447_Ismail__COMP306_Lab3.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<_301168447_Ismail__COMP306_Lab3.Models.Movie> Movie { get; set; }
        public DbSet<_301168447_Ismail__COMP306_Lab3.Models.Comment> Comment { get; set; }
        public DbSet<_301168447_Ismail__COMP306_Lab3.Models.User> User { get; set; }
    }
}
