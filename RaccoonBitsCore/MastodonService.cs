using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace RaccoonBitsCore
{
    public partial class MastodonService
    {
        private readonly string? accessToken;

        private readonly string host;

        public ILogger? Logger { get; set; }

        public MastodonService(string host)
        {
            this.host = host;
        }

        public MastodonService(string host, string accessToken)
        {
            this.host = host;
            this.accessToken = accessToken;
        }

        private string GetApiUrl(string endpoint)
        {
            return $"https://{host}/{endpoint}";
        }

        public static Dictionary<string, string> ParseLinkHeader(string linkHeader)
        {
            var links = new Dictionary<string, string>();
            var matches = RelExtractor().Matches(linkHeader);

            foreach (Match match in matches.Cast<Match>())
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

        public async Task FetchHashtags(IMastodonApiCallback callback)
        {
            using HttpClient httpClient = new();

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            string? currentUrl = GetApiUrl("api/v1/followed_tags?limit=40");

            while (!string.IsNullOrEmpty(currentUrl))
            {
                // avoid issues to overload server, we are not in a hurry
                Thread.Sleep(5000);

                Logger?.LogInformation($"Retrieving {currentUrl} ...");

                HttpResponseMessage response = await httpClient.GetAsync(currentUrl);

                if (response.IsSuccessStatusCode)
                {
                    await callback.ProcessResponse(response);

                    // Parse the Link header to extract next and prev URLs
                    if (response.Headers.TryGetValues("Link", out var linkHeaderValues))
                    {
                        string? linkHeader = linkHeaderValues.FirstOrDefault();
                        
                        if (linkHeader != null)
                        {
                            var links = ParseLinkHeader(linkHeader);

                            if (links.TryGetValue("next", out var prevUrl))
                            {
                                currentUrl = prevUrl;
                            }
                            else
                            {
                                currentUrl = null; // No more pagination
                            }
                        }
                    }
                    else
                    {
                        currentUrl = null; // No Link header, stop pagination
                    }
                }
                else
                {
                    Logger?.LogInformation($"Error: {response.StatusCode}");
                    currentUrl = null; // Error occurred, stop pagination
                }
            }
        }

        public async Task FetchFavorites(IMastodonApiCallback callback)
        {
            using HttpClient httpClient = new();

            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            string? currentUrl = GetApiUrl("api/v1/favourites?limit=40");

            while (!string.IsNullOrEmpty(currentUrl))
            {
                Thread.Sleep(5000);

                Logger?.LogInformation($"Retrieving {currentUrl} ...");

                HttpResponseMessage response = await httpClient.GetAsync(currentUrl);

                if (response.IsSuccessStatusCode)
                {
                    await callback.ProcessResponse(response);

                    // Parse the Link header to extract next and prev URLs
                    if (response.Headers.TryGetValues("Link", out var linkHeaderValues))
                    {
                        string? linkHeader = linkHeaderValues?.FirstOrDefault();

                        if (linkHeader != null)
                        {
                            var links = ParseLinkHeader(linkHeader);

                            if (links.TryGetValue("next", out var prevUrl))
                            {
                                currentUrl = prevUrl;
                            }
                            else
                            {
                                currentUrl = null; // No more pagination
                            }
                        }
                    }
                    else
                    {
                        currentUrl = null; // No Link header, stop pagination
                    }
                }
                else
                {
                    Logger?.LogInformation($"Error: {response.StatusCode}");
                    currentUrl = null; // Error occurred, stop pagination
                }
            }

        }

        [GeneratedRegex("<([^>]+)>; rel=\"([^\"]+)\"")]
        private static partial Regex RelExtractor();
    }
}