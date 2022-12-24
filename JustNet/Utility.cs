using System.Net;
using System.Net.Sockets;

namespace JustNet
{
    internal static class Utility
    {
        public static IPAddress GetLocalIPAddress()
        {
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (IPAddress ipAddress in host.AddressList)
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ipAddress;
                }
            }

            return null;
        }

        public class UniqueQueue<T>
        {
            private readonly Queue<T> queue;

            public int Count { get => queue.Count; }

            public bool Contains(T data) => queue.Contains(data);

            public UniqueQueue()
            {
                queue = new Queue<T>();
            }

            public void Enqueue(T data)
            {
                if (queue.Contains(data))
                {
                    return;
                }

                queue.Enqueue(data);
            }

            public T Dequeue()
            {
                return queue.Dequeue();
            }

            public void OrderByAscending()
            {
                Queue<T> temp = new Queue<T>();

                foreach (T item in queue.OrderBy(x => x))
                {
                    temp.Enqueue(item);
                }

                queue.Clear();

                foreach (T item in temp)
                {
                    queue.Enqueue(item);
                }
            }
        }
    }
}

