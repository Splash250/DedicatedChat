using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    class AsyncServer
    {
        //creating global private variables for the program
        private Socket serverSocket;
        private List<User> users;
        private byte[] buffer;
        private string passPhrase = "messageprogram";
        public AsyncServer()
        {
            //CONSTRUCTOR
            try
            {
                //here we are creating a new socket which will be our main server later
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (!File.Exists("Accounts.db"))
                {
                    File.Create("Accounts.db");
                }
            }
            catch (SocketException)
            {
                //if it fails
                Console.WriteLine("Failed to create server!");
                return;
            }
        }
        public void Start(int port, int backlog, int bufferLength)
        {
            //here we start the server (the one we created earlier)
            try
            {
                //bind the server to all available local network interfaces
                serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                //listen with the provided backlog
                serverSocket.Listen(backlog);
                //cretate a new list that stores the connected clients
                users = new List<User>();
                //create the buffer
                buffer = new byte[bufferLength];
                StartAccepting();
            }
            catch (SocketException)
            {
                //if it fails
                Console.WriteLine("Failed to create server on port" + port);
            }
        }
        public void Kick(string remoteEndPoint, string reason)
        {
            string username = "";
            string remoteEP = "";
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].RemoteInfo == remoteEndPoint)
                {
                    username = users[i].UserName;
                    remoteEP = users[i].RemoteInfo;
                    users[i].UserSocket.Send(Encode("kick" + reason));
                    DisconnectClient(users[i].UserName, users[i].UserSocket);
                    //wrte that we kicked the guy and send it to the othersú
                    Console.WriteLine(username + " has been kicked from " + remoteEP + "!");
                    SendDataToAllClients(username + " has been kicked by the server!");
                    return;
                }
            }
            Console.WriteLine("There is no user with remote endpoint: " + remoteEndPoint + "!");
        }
        public bool Ban(string username)
        {
            //at register and login check if banned if so then return false
            if (!File.Exists("Blacklist.db"))
            {
                File.Create("Blacklist.db");
            }
            foreach (string line in File.ReadAllLines("Blacklist.db"))
            {
                if (line.Contains(username))
                {
                    return false;
                }
            }
            File.AppendAllText("Blacklist.db", username + "\r\n");
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].UserName == username)
                {
                    Kick(users[i].RemoteInfo, "You have been banned!");
                }
            }
            return true;
        }
        public void ShowClients()
        {
            if (users.Count > 0)
            {
                for (int i = 0; i < users.Count; i++)
                {
                    Console.WriteLine(i + ") " + users[i].RemoteInfo + " | USERNAME: " + users[i].UserName);
                }
            }
            else
            {
                Console.WriteLine("No connected clients to show!");
            }
        }
        private void StartAccepting()
        {
            Console.WriteLine("Server started...");
            //we begin accepting here
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback),  null);
        }
        private void AcceptCallback(IAsyncResult AR)
        {
            //here is the core of the multi client server
            Socket handler = serverSocket.EndAccept(AR);
            User user;
            string userName = "";
            string credentials = "";
            //registering the handler as a user
            while (true)
            {
                string data = Receive(handler, 1024);
                if (data != null)
                {
                    if (data.StartsWith("REGISTER"))
                    {
                        credentials = data.Remove(0, 9);
                        //check if the accounts.db countains the provided name if so then send a fail else send ok then add the account to the accounts.db
                        if (CanRegister(credentials))
                        {
                            Register(credentials);
                            handler.Send(Encode("OK"));
                            break;
                        }
                        else
                        {
                            handler.Send(Encode("FAIL"));
                        }
                    }
                    else if (data.StartsWith("LOGIN"))
                    {
                        credentials = data.Remove(0, 6);
                        //check if the credentials match or not
                        if (!IsBanned(credentials))
                        {
                            if (!IsAlreadyLoggenIn(credentials))
                            {
                                if (CanLogin(credentials))
                                {
                                    handler.Send(Encode("OK"));
                                    break;
                                }
                                else
                                {
                                    handler.Send(Encode("FAIL"));
                                }
                            }
                            else
                            {
                                handler.Send(Encode("LOGGEDIN"));
                            }
                        }
                        else
                        {
                            handler.Send(Encode("BANNED"));
                        }
                    }
                }
                else
                {
                    DisconnectClient(GetUserName(handler.RemoteEndPoint.ToString()), handler);
                    serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
                    return;
                }
            }
            userName = credentials.Split(';')[0];
            user = new User(userName, handler);
            users.Add(user);

            //add the client that has connected
            Console.WriteLine(handler.RemoteEndPoint.ToString().Split(':')[0] + " has connected on port " + handler.RemoteEndPoint.ToString().Split(':')[1] + "!");
            SendDataToAllClients(userName + " has connected to the chat!");
            //begin handling it
            handler.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), handler);
            
            //string userName = RegisterUser(handler);
            //if (userName != null)
            //{

            //}
            //start receiving again for other clients
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }
        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket client = (Socket)AR.AsyncState;
            string userName = "";
            string userIP = "";
            int userPort = 0;
            try
            {
                string clientInfoRaw = client.RemoteEndPoint.ToString();
                string[] clientInfo = clientInfoRaw.Split(':');
                userIP = clientInfo[0];
                userPort = int.Parse(clientInfo[1]);
                userName = GetUserName(clientInfoRaw);
                int received = client.EndReceive(AR);
                byte[] dataBuf = new byte[received];
                Array.Copy(buffer, dataBuf, received);
                string data = Decode(dataBuf);
                Console.WriteLine(userName + ": " + data);
                SendDataToAllClients(userName + ": " + data);
                client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), client);
            }
            catch (SocketException)
            {
                DisconnectClient(userName, client);
                SendDataToAllClients(userName + " has left the chat!");
                Console.WriteLine(userIP + " has disconnected from port " + userPort + "!");
            }
            catch (Exception)
            {
                return;
            }
        }
        private void SendCallback(IAsyncResult AR)
        {
            Socket sender = (Socket)AR.AsyncState;
            sender.EndSend(AR);
        }
        private void SendDataToAllClients(string toSend)
        {
            byte[] bytesToSend = Encode(toSend);
            for (int i = 0; i < users.Count; i++)
            {
                users[i].UserSocket.BeginSend(bytesToSend, 0, bytesToSend.Length, SocketFlags.None, new AsyncCallback(SendCallback), users[i].UserSocket);
            }
        }
        private string Decode(byte[] toBeDecoded)
        {
            try
            {
                //decoding
                StringBuilder sb = new StringBuilder();
                //decode the bytes until it reaches the end or the first null
                foreach (byte b in toBeDecoded)
                {
                    if (b == 00)
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(Convert.ToChar(b));
                    }
                }
                string encodedText = sb.ToString();
                //get the string out of the stream
                string decodedText = StringCipher.Decrypt(encodedText, passPhrase);
                return decodedText;
            }
            catch (Exception)
            {
                return null;
            }
        }
        private byte[] Encode(string toBeEncoded)
        {
            //simple encode and return function
            string cipher = StringCipher.Encrypt(toBeEncoded, passPhrase);
            byte[] encoder = Encoding.UTF8.GetBytes(cipher);
            return encoder;

        }
        private bool CanRegister(string credentials)
        {
            //if banned return false
            string[] credArray = credentials.Split(';');
            foreach (string line in File.ReadAllLines("Accounts.db"))
            {
                if (line.Contains(credArray[0]))
                {
                    return false;
                }
            }
            return true;
        }
        private bool IsBanned(string credentials)
        {
            string[] credArray = credentials.Split(';');
            foreach (string line in File.ReadAllLines("Blacklist.db"))
            {
                if (line.Contains(credArray[0]))
                {
                    return true;
                }
            }
            return false;
        }
        private void Register(string credentials)
        {
            File.AppendAllText("Accounts.db", credentials + Environment.NewLine);
        }
        private bool CanLogin(string credentials)
        {
            //if banned return false
            foreach (string line in File.ReadAllLines("Accounts.db"))
            {
                if (line.Contains(credentials))
                {
                    return true;
                }
            }
            return false;
        }
        private string GetUserName(string remoteEP)
        {
            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].RemoteInfo == remoteEP)
                {
                    return users[i].UserName;
                }
            }
            return null;
        }
        private string Receive(Socket socket, int bufferLength)
        {
            try
            {
                byte[] buffer = new byte[bufferLength];
                int size = socket.Receive(buffer);
                string data = Decode(buffer);
                return data;
            }
            catch (SocketException)
            {
                DisconnectClient(GetUserName(socket.RemoteEndPoint.ToString()), socket);
                return null;
            }

        }
        private bool IsAlreadyLoggenIn(string credentials)
        {
            foreach (User user in users)
            {
                if (user.UserName == credentials.Split(';')[0])
                {
                    return true;
                }
            }
            return false;
        }

        private void DisconnectClient(string userName, Socket client)
        {
            string remoteEndPoint = client.RemoteEndPoint.ToString();
            try
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception)
            {
                client.Close();
            }
            if (userName != null)
            {
                for (int i = 0; i < users.Count; i++)
                {
                    if (users[i].UserName == userName)
                    {
                        users.Remove(users[i]);
                    }
                }
                Console.WriteLine(userName + " has disconnected from " + remoteEndPoint);
            }
            else
            {
                Console.WriteLine(remoteEndPoint + " has disconnected!");
            }
        }
    }
}
