namespace RaccoonBitsCore
{
    public class WeightsProfile
    {
        public int TagsScore { get; set; } = 100;
        public int MinimumFavoritesWordsCount { get; internal set; } = 5;

        public WeightsProfile()
        {
        }
    }
}