namespace RaccoonBitsCore
{
    public class WeightsProfile
    {
        public int TagsScore { get; set; } = 75;

        public int MinimumFavoritesWordsCount { get; internal set; } = 10;

        public WeightsProfile()
        {
        }
    }
}