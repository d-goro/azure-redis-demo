using System;
using System.Threading;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace RedisChat
{
    class Program
    {
        private static string connectionString = "<specify here connection string to azure redis cache>";
        private static ISubscriber subscriber;
        private static ConnectionMultiplexer connection;
        static readonly object _locker = new object();
        static bool _go;

        static void Main(string[] args)
        {
            Console.WriteLine("Please, enter your username to connect to the chat system:");
            var username = Console.ReadLine();
            if (!Connect(username))
            {
                Console.WriteLine("Cannot connect, try run again");
                return;
            }

            Console.WriteLine("To end this chat just press Enter");
            Console.WriteLine("Please, enter target username and message to him. For example: @Bob Let's party");
            string msg;
            do
            {
                msg = Console.ReadLine();
                SendMessage(username, msg);

            } while (!string.IsNullOrEmpty(msg));
            subscriber.Unsubscribe(username);
            connection.Close(false);
        }

        private static bool Connect(string username)
        {
            new Thread (WaitToConnect).Start();
            bool result;
            try
            {
                connection = ConnectionMultiplexer.Connect(connectionString);
                subscriber = connection.GetSubscriber();
                subscriber.Subscribe(username, (user, msg) => HandleMessage(user, msg));
                Console.WriteLine ("Connected!");
                result = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                result = false;
            }
            finally
            {
                lock (_locker)
                {// setting _go=true and pulsing.
                    _go = true;
                    Monitor.Pulse (_locker);
                }
            }

            return result;
        }

        static void SendMessage(string sender, string umsg)
        {
            if (!string.IsNullOrEmpty(umsg))
            {
                var parts = umsg.Split(' ',2,  StringSplitOptions.RemoveEmptyEntries);
                var blob = new Message {Sender = sender, Msg = parts[1]};
                subscriber.PublishAsync(parts[0].TrimStart('@'), JsonConvert.SerializeObject(blob));
            }
        }

        static void HandleMessage(string user, string msg)
        {
            var blob = JsonConvert.DeserializeObject<Message>(msg);
            Console.WriteLine($"{DateTime.Now}, {blob.Sender}: {blob.Msg}");
        }

        static void WaitToConnect()
        {
            Console.WriteLine("Waiting to connect...");
            lock (_locker)
            {
                while (!_go)
                {
                    Console.WriteLine("...");
                    Monitor.Wait (_locker, 300); 
                }
            }
        }
    }
}
