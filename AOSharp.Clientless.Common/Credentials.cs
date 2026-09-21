using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AOSharp.Clientless.Common
{
    public class Credentials
    {
        public string Username { get; }
        public string Password { get; }

        public Credentials(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}
