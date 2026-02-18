using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AutoBoostBot
{
    /// <summary>
    /// Minimal Mastodon API client for lookup, fetching statuses, and boosting.
    /// </summary>
    public class MastodonClient
    {
        private readonly string host;

        private readonly string accessToken;

        public ILogger? Logger { get; set; }

        public MastodonClient(string host, string accessToken)
        {
            this.host = host;
            this.accessToken = accessToken;
        }

        private string ApiUrl(string endpoint) => $"https://{host}/{endpoint}";

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            return client;
        }

        /// <summary>
        /// Looks up an account by handle (e.g. user@instance.tld) and returns the local account ID.
        /// </summary>
        public async Task<string?> LookupAccount(string acct)
        {
            using var httpClient = CreateHttpClient();

            var encodedAcct = System.Web.HttpUtility.UrlEncode(acct);
            var url = ApiUrl($"api/v1/accounts/lookup?acct={encodedAcct}");

            Logger?.LogInformation("Looking up account {Acct} at {Url}", acct, url);

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                Logger?.LogError("Account lookup failed for {Acct}: {StatusCode}", acct, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            dynamic account = JsonConvert.DeserializeObject(content)!;

            return account.id?.ToString();
        }

        /// <summary>
        /// Fetches recent statuses for an account, excluding replies and reblogs.
        /// </summary>
        public async Task<IList<dynamic>> GetAccountStatuses(string accountId, int limit = 40)
        {
            using var httpClient = CreateHttpClient();

            var url = ApiUrl(
                $"api/v1/accounts/{accountId}/statuses?limit={limit}&exclude_replies=true&exclude_reblogs=true");

            Logger?.LogInformation("Fetching statuses from {Url}", url);

            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var statuses = JsonConvert.DeserializeObject<IList<dynamic>>(content)!;

            return statuses;
        }

        /// <summary>
        /// Boosts a status by its local numeric ID.
        /// </summary>
        public async Task BoostStatusById(string statusId)
        {
            using var httpClient = CreateHttpClient();

            var boostUrl = ApiUrl($"api/v1/statuses/{statusId}/reblog");

            Logger?.LogInformation("Reblogging {Url}", boostUrl);

            var response = await httpClient.PostAsync(boostUrl, null);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Resolves a remote URI via search, then boosts the found status.
        /// Returns true if the boost succeeded.
        /// </summary>
        public async Task<bool> ResolveAndBoost(string statusUri)
        {
            using var httpClient = CreateHttpClient();

            var encodedUri = System.Web.HttpUtility.UrlEncode(statusUri);
            var searchUrl = ApiUrl($"api/v2/search?resolve=true&q={encodedUri}");

            Logger?.LogInformation("Resolving {Uri}", statusUri);

            var searchResponse = await httpClient.GetAsync(searchUrl);
            searchResponse.EnsureSuccessStatusCode();

            var searchContent = await searchResponse.Content.ReadAsStringAsync();
            dynamic searchResult = JsonConvert.DeserializeObject(searchContent)!;

            if (searchResult.statuses.Count > 0)
            {
                string statusId = searchResult.statuses[0].id.ToString();
                await BoostStatusById(statusId);

                return true;
            }

            Logger?.LogError("Could not resolve status {Uri}", statusUri);
            return false;
        }
    }
}
