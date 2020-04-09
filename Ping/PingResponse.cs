using System.Net;

namespace Ping
{
    public class PingResponse
    {
        public HttpStatusCode StatusCode { get; set; }

        public long ResponseTime { get; set; }
    }
}