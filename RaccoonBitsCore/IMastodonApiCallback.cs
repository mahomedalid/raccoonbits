namespace RaccoonBitsCore
{
    public interface IMastodonApiCallback
    {
        Task<bool> ProcessResponse(HttpResponseMessage response);
    }
}