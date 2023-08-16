using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace RaccoonBitsCore
{
    public class FavoritesProcessor : IMastodonApiCallback
    {
        private readonly Db db;

        public FavoritesProcessor(Db db)
        {
            this.db = db;
        }

        public async Task<bool> ProcessResponse(HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            
            var jsonArray = JsonConvert.DeserializeObject<JArray>(responseContent);

            if (jsonArray != null)
            {

                foreach (var item in jsonArray)
                {
                    var id = (ulong)item["id"]!;
                    db.InsertOrReplaceLike(id, JsonConvert.SerializeObject(item));
                }
            }

            return true;
        }
    }
}