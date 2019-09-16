using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    class User
    {
        public string UserName { get; set; }
        public Socket UserSocket { get; set; }
        public string RemoteInfo { get;}
        public User(string userName, Socket userSocket)
        {
            this.UserName = userName;
            this.UserSocket = userSocket;
            RemoteInfo = userSocket.RemoteEndPoint.ToString();
        }
        public string GetUserName()
        {
            return UserName;
        }
        public string GetUserIP()
        {
            return UserSocket.RemoteEndPoint.ToString().Split(':')[0];
        }
    }
}
