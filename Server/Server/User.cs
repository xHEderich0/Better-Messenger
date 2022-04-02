using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace Server
{
    public class User
    {
        private Socket socket;
        private string login;
        private bool loggedIn;
        private bool registered;
        private string password;
        private bool added;
        private List<string> inbox = new List<string>();

        public User(Socket s)
        {
            socket = s;
            login = null;
            loggedIn = false;
            registered = false;
            added = false;
            //inbox = null;
        }

        public void AddToInbox(string msg)
        {
            inbox.Add(msg);
        }
        public void AddInbox(List<string> newInbox)
        {
            inbox = newInbox;
        }
        public List<string> GetInbox()
        {
            return inbox;
        }
        public void SetLogin(string l)
        {
            login = l;
        }
        public string GetLogin()
        {
            return login;
        }
        public void SetPassword(string p)
        {
            password = p;
        }
        public string GetPassword()
        {
            return password;
        }
        public bool IsLogged()
        {
            return loggedIn;
        }
        public bool IsRegistered()
        {
            return registered;
        }
        public void SetLogged(bool l)
        {
            loggedIn = l;
        }
        public void SetRegistered(bool r)
        {
            registered = r;
        }
        public Socket GetSocket()
        {
            return socket;
        }
    }
}
