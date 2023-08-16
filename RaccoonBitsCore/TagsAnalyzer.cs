using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace RaccoonBitsCore
{
    public class TagsAnalyzer : IMastodonApiCallback
    {
        private readonly Db db;

        private readonly WeightsProfile weightsProfile;

        public TagsAnalyzer(Db db, WeightsProfile weightsProfile)
        {
            this.db = db;
            this.weightsProfile = weightsProfile;
        }

        public async Task<bool> ProcessResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var jsonArray = JsonConvert.DeserializeObject<JArray>(responseContent);

            if (jsonArray != null)
            {
                foreach (var item in jsonArray)
                {
                    var name = item["name"]?.ToString()!;
                    db.InsertOrReplaceWordScore(name, weightsProfile.TagsScore);
                }
            }

            return true;
        }
    }
}