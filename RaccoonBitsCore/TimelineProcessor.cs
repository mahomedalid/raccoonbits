using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RaccoonBitsCore;

public class TimelineProcessor : IMastodonApiCallback
{
    private readonly Db db;

    public TimelineProcessor(Db db)
    {
        this.db = db;
    }

    public async Task<bool> ProcessResponse(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonArray = JsonConvert.DeserializeObject<JArray>(responseContent);

        foreach (var item in jsonArray!)
        {
            var uri = item["uri"]?.ToString()!;
            db.InsertOrReplacePost(uri, JsonConvert.SerializeObject(item), 0);
        }

        return true;
    }
}