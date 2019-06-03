

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace http_file_server
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new Http_server();
            Task.Run(() => server.Start());
            Console.ReadLine();
        }
    }
}
