using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DedicatedServer
{
    class Program
    {
        static void Main(string[] args)
        {
            AsyncServer server = new AsyncServer();
            server.Start(4444, 5, 1024);
            string commandRaw;
            while (true)
            {
                commandRaw = Console.ReadLine();
                string[] command = commandRaw.Split(' ');
                if (command[0] == "kick")
                {
                    if (command.Length != 2)
                    {
                        server.Kick(command[1], command[2]);
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of arguments for kick!");
                    }
                }
                else if (command[0] == "show" && command[1] == "clients")
                {
                    if (command.Length != 2) 
                    {
                        Console.WriteLine("Wrong number of aruments for show command!");
                    }
                    else
                    {
                        server.ShowClients();
                    }
                }
                else if (command[0] == "ban")
                {
                    if (command.Length != 2)
                    {
                        Console.WriteLine("Wrong number of args for ban command!");
                    }
                    else
                    {
                        if (server.Ban(command[1]))
                        {
                            Console.WriteLine("Succesfully banned " + command[1]);
                        }
                        else
                        {
                            Console.WriteLine(command[1] + " is already banned from this server!");
                        }
                    }
                }
                else if (command[0] == "help")
                {
                    if (command.Length != 1)
                    {
                        //display help
                    }
                    else
                    {
                        Console.WriteLine("Wrong number of args for help command!");
                    }
                }
                else
                {
                    Console.WriteLine("The provided command does not exist!\r\nType help for the list of commands!");
                }
            }
        }
    }
}
