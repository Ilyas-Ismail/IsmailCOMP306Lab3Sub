using System;
using Amazon.DynamoDBv2.DataModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _301168447_Ismail__COMP306_Lab3.Models
{
    [DynamoDBTable("Movies")]
    public class Movie
    {
        [DynamoDBHashKey]
        public int MovieId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public string Director { get; set; }
        public string Cast { get; set; }
        public string Description { get; set; }
        public string Duration { get; set; }
        public string ReleaseDate { get; set; }
        public string UploadDate { get; set; }
        public string FileName { get; set; }
        public double Rating { get; set; }
    }
}
