using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RaccoonBitsCore;
using System.Text.Json.Nodes;
using System;
using System.Data.Common;

public class PostRankProcessor : IRecordProcessor<Post>
{
    public Dictionary<string, int> WordsRank { get; set; }

    public Dictionary<string, int> HostsRank { get; set; }

    public bool PenalizeBots { get; set; } = true;

    public PostRankProcessor(Dictionary<string, int> wordsRank, Dictionary<string, int> hostsRank)
    {
        this.WordsRank = wordsRank;
        this.HostsRank = hostsRank;
    }

    public Post Process(Post post)
    {
        dynamic item = JsonConvert.DeserializeObject(post.Body)!;

        var itemContent = item.content.ToString();
        var accountNote = (item.account?.note ?? string.Empty).ToString();

        var content = $"{itemContent}";
        string plainText = StringUtils.RemoveStopwords(StringUtils.StripHtmlTags(content).ToLowerInvariant());

        string[] words = plainText.Split(' ');

        double wordsScore = MathUtils.Normalize(CalculateWordsScore(plainText), 0, 500, 1, 100) / 100;

        int buzzScore = (int)item.replies_count + (int)item.reblogs_count * 2 + (int)item.favourites_count;

        double buzzScoreNormalized = MathUtils.Normalize(buzzScore, 0, 60, 1, 100) / 100;

        Uri itemUri = new Uri(item?.uri?.ToString());

        int largestValue = HostsRank.Values.Max();

        HostsRank.TryGetValue(itemUri.Host, out int hostWeight);

        double instanceScore = MathUtils.Normalize(hostWeight, 0, HostsRank.Values.Max(), 1, 100) / 100;

        //int fameScore = (int)// + (int)((int)item.account.statuses_count / 1000);
        int followers = 0;

        int.TryParse(item?.account?.followers_count?.ToString(), out followers);

        double fameScore = MathUtils.Normalize(followers, 0, 7000, 1, 100) / 100;

        if (PenalizeBots)
        {
            if ((bool)item?.account?.bot)
            {
                wordsScore -= 0.2;
                fameScore = 0.01;
            }
        }
        
        post.WordsScore = wordsScore;
        post.FameScore = fameScore;
        post.BuzzScore = buzzScoreNormalized;
        post.HostScore = instanceScore;
        post.Score = (double)(
                        (wordsScore * 0.50) +
                        (buzzScoreNormalized * 0.15) +
                        (fameScore * 0.1) +
                        (instanceScore * 0.25)
                    );

        return post;
    }

    private int CalculateWordsScore(string plainText)
    {
        int totalScore = 0;

        string[] words = plainText.Split(' ');

        string[] uniqueWords = words.Distinct().ToArray();

        int numberOfWordsToExtract = 150;

        int totalWords = Math.Min(numberOfWordsToExtract, uniqueWords.Length);

        for (int i = 0; i < totalWords; i++)
        {
            var word = uniqueWords[i];
            if (WordsRank.TryGetValue(word, out int wordScore))
            {
                totalScore += wordScore;
            }
        }

        if (uniqueWords.Length < 10)
        {
            totalScore -= 100;
        }

        if (uniqueWords.Length < 15)
        {
            totalScore -= 50;
        }

        return totalScore;
    }
}