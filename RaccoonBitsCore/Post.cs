using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaccoonBitsCore
{
    public class Post
    {
        public string Uri { get; set; }

        public string Body { get; set; }

        public double FameScore { get; set; }

        public double BuzzScore { get; set; }

        public double WordsScore { get; set; }

        public double HostScore { get; set; }

        public double Score { get; set; }

        public Post(string uri, string body)
        {
            this.Uri = uri;
            this.Body = body;
        }
    }
}
