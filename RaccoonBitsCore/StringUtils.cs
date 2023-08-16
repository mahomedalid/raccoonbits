using HtmlAgilityPack;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RaccoonBitsCore
{
    public class StringUtils
    {
        public static string StripHtmlTags(string htmlContent)
        {
            string decodedHtml = WebUtility.HtmlDecode(htmlContent);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(decodedHtml);

            var sb = new StringBuilder();
            foreach (HtmlNode node in doc.DocumentNode.DescendantsAndSelf())
            {
                if (!node.HasChildNodes)
                {
                    string text = node.InnerText.Trim();
                    if (!string.IsNullOrEmpty(text))
                        sb.AppendLine(text);
                }
            }

            return sb.ToString().Trim();
        }

        public static string RemoveStopwords(string input)
        {
            // List of common English stopwords

            string[] stopwords = {
                "m", "com", "like", "new", "us", "one", "see", "re", "get", "en", "use",
                "i", "me", "my", "myself", "we", "our", "ours", "ourselves", "you", "your", "yours", "yourself", "yourselves",
                "he", "him", "his", "himself", "she", "her", "hers", "herself", "it", "its", "itself", "they", "them", "their", "theirs", "themselves",
                "what", "which", "who", "whom", "this", "that", "these", "those", "am", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "having",
                "do", "does", "did", "doing", "a", "an", "the", "and", "but", "if", "or", "because", "as", "until", "while", "of", "at", "by", "for", "with", "about",
                "against", "between", "into", "through", "during", "before", "after", "above", "below", "to", "from", "up", "down", "in", "out", "on", "off", "over", "under",
                "again", "further", "then", "once", "here", "there", "when", "where", "why", "how", "all", "any", "both", "each", "few", "more", "most", "other", "some",
                "such", "no", "nor", "not", "only", "own", "same", "so", "than", "too", "very", "s", "t", "can", "will", "just", "don", "should", "now"
            };

            // Split the input text into words
            string[] words = Regex.Split(input, @"\W+");

            // Remove stopwords
            string[] filteredWords = words.Where(word => word.Length > 2 && !stopwords.Contains(word.ToLower())).ToArray();

            // Reconstruct the cleaned text
            string cleanedText = string.Join(" ", filteredWords);

            return cleanedText;
        }

        public static string CleanWord(string word)
        {
            return word.TrimEnd('.', ',', '!', '?', ';', ':', '\"', '\'');
        }
    }
}