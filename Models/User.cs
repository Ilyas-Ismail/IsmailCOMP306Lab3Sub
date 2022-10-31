using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _301168447_Ismail__COMP306_Lab3.Models
{
    [DynamoDBTable("Users")]
    public class User
    {
        [DynamoDBHashKey]
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public User()
        {

        }
    }
}
