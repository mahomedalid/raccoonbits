using System.Text.RegularExpressions;

namespace RaccoonBitsCore
{
    public class MastodonApiUtils
    {
        public static Dictionary<string, string> ParseLinkHeader(string linkHeader)
        {
            var links = new Dictionary<string, string>();
            var matches = Regex.Matches(linkHeader, @"<([^>]+)>; rel=""([^""]+)""");

            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    string url = match.Groups[1].Value;
                    string rel = match.Groups[2].Value;
                    links[rel] = url;
                }
            }

            return links;
        }
    }
}