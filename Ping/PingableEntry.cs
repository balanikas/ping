using System;
using System.Net;
using System.Net.Http;

namespace Ping
{
    class PingableEntry
    {
        public Uri Host { get; }

        public HttpStatusCode StatusCode { get; set; }

        public long ResponseTime { get; set; }

        public string Name { get; }

        internal HttpClient Client { get; }

        public PingableEntry(Uri host, string name)
        {
            Host = host;
            Name = name;
            Client = new HttpClient {BaseAddress = Host};
        }
    }
}
