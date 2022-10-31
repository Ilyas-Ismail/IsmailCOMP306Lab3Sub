using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _301168447_Ismail__COMP306_Lab3.Models
{
    [DynamoDBTable("Comments")]
    public class Comment
    {
        [DynamoDBHashKey]
        public int CommentId { get; set; }
        public int MovieId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
        public string MovieTitle { get; set; }
        public string CommentContent { get; set; }
        public int Rating { get; set; }
        public string CommentTime { get; set; }
    }
}
