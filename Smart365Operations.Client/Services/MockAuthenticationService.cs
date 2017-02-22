using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Smart365Operations.Common.Infrastructure.Interfaces;
using Smart365Operations.Common.Infrastructure.Models;

namespace Smart365Operations.Client.Services
{
    public class MockAuthenticationService : IAuthenticationService
    {
        internal class InternalUserData
        {
            public InternalUserData(string id,string username, string email, string hashedPassword, string[] roles)
            {
                Id = id;
                Username = username;
                Email = email;
                HashedPassword = hashedPassword;
                Roles = roles;
            }

            public string Id { get; private set; }
            public string Username
            {
                get;
                private set;
            }

            public string Email
            {
                get;
                private set;
            }

            public string HashedPassword
            {
                get;
                private set;
            }

            public string[] Roles
            {
                get;
                private set;
            }
        }

        private static readonly List<InternalUserData> Users = new List<InternalUserData>()
        {
            new InternalUserData("1",
                                 "admin",
                                 "admin@126.com",
                                 "gqefEbSstSpkLvfjOd/OSqkv9l7S56twLXmNvhDsoLg=",
                                 new string[] { "Administrators" })
        };
        public User AuthenticateUser(string username, string password)
        {
            InternalUserData userData = Users.FirstOrDefault(u => u.Username.Equals(username) && u.HashedPassword.Equals(CalculateHash(password, u.Username)));
            if (userData == null)
                throw new UnauthorizedAccessException("Access denied. Please provide some valid credentials(证书).");

            return new User(userData.Id,userData.Username, userData.Email, userData.Roles);
        }

        private string CalculateHash(string clearTextPassword, string salt)
        {
            // Convert the salted password to a byte array
            byte[] saltedHashBytes = Encoding.UTF8.GetBytes(clearTextPassword + salt);
            // Use the hash algorithm to calculate the hash
            HashAlgorithm algorithm = new SHA256Managed();
            byte[] hash = algorithm.ComputeHash(saltedHashBytes);
            // Return the hash as a base64 encoded string to be compared to the stored password
            return Convert.ToBase64String(hash);
        }
    }
}
