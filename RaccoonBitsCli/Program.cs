using RaccoonBitsCore;
using System.CommandLine.Parsing;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceCollection = new ServiceCollection();

ConfigureServices(serviceCollection, args);

using var serviceProvider = serviceCollection.BuildServiceProvider();

var db = serviceProvider.GetRequiredService<Db>();
var weightsProfile = serviceProvider.GetRequiredService<WeightsProfile>();

var rootCommand = new RootCommand();

var loggerFactory = serviceProvider.GetService<ILoggerFactory>()!;

var accessTokenOption = new Option<string>("--accessToken", "Access token for Mastodon")
{
    IsRequired = true,
};

var hostOption = new Option<string>("--host", "Mastodon host")
{
    IsRequired = true,
};

var weightOption = new Option<int>("--weight", () => 5, "Minimum score (number of liked posts) for instances");
var minimumWordsScoreOption = new Option<double>("--words-score", () => 0.3, "Minimum of words score for ranked posts");
var minimumBuzzScoreOption = new Option<double>("--buzz-score", () => 0.01, "Minimum of buzz score for ranked posts");
var limitOption = new Option<int>("--top", () => 3, "Number of top ranked posts to boost");

var favoritesCmd = new Command("favorites", "Retrieves favorites from mastodon and updates the algorithm profile");

favoritesCmd.AddOption(accessTokenOption);
favoritesCmd.AddOption(hostOption);

favoritesCmd.SetHandler(async (accessToken, host) =>
{
    var logger = loggerFactory.CreateLogger<Program>();

    var mastodonService = new MastodonService(host, accessToken)
    {
        Logger = logger
    };

    var favoritesProcessor = new FavoritesProcessor(db);
    await mastodonService.FetchFavorites(favoritesProcessor);

    var favoritesAnalyzer = new FavoritesAnalyzer(db, weightsProfile)
    {
        Logger = logger
    };

    favoritesAnalyzer.Execute();

}, accessTokenOption, hostOption);

var tagsCmd = new Command("tags", "Retrieves tags from mastodon and updates the algorithm profile");

tagsCmd.AddOption(accessTokenOption);
tagsCmd.AddOption(hostOption);

tagsCmd.SetHandler(async (accessToken, host) =>
{
    var logger = loggerFactory.CreateLogger<Program>();

    var mastodonService = new MastodonService(host, accessToken)
    {
        Logger = logger
    };

    var processor = new TagsAnalyzer(db, weightsProfile);
    await mastodonService.FetchHashtags(processor);
}, accessTokenOption, hostOption);

var fetchTimelinesCmd = new Command("fetch-timelines", "Fetch public timelines from instances");

fetchTimelinesCmd.AddOption(weightOption);

fetchTimelinesCmd.SetHandler(async (weight) =>
{
    var logger = loggerFactory.CreateLogger<Program>();

    var hosts = db.GetMstdInstances(weight);

    var processor = new TimelineProcessor(db);

    foreach (var host  in hosts)
    {
        var mastodonService = new MastodonService(host)
        {
            Logger = logger
        };

        try
        {
            await mastodonService.GetPublicTimeline(processor, true);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex.Message);
        }

        try
        {
            await mastodonService.GetPublicTimeline(processor, false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex.Message);
        }
    }
}, weightOption);

var rankPosts = new Command("rank-posts", "Rank posts");

var wordsWeightOption = new Option<double>("--wordsWeight", () => 0.5, "Weight in the algorithm to the words score (decimal between 0 and 1)");
var buzzWeightOption = new Option<double>("--buzzWeight", () => 0.15, "Weight in the algorithm to the buzz (favorites+boosts) score (decimal between 0 and 1)");
var fameWeightOption = new Option<double>("--fameWeight", () => 0.1, "Weight in the algorithm to the fame (author followers) weight (decimal between 0 and 1)");
var instanceWeightOption = new Option<double>("--instanceWeight", () => 0.25, "Weight in the algorithm to the instance rank (how much you have like posts from that instance) (decimal between 0 and 1)");

rankPosts.SetHandler((wordsWeight, buzzWeight, fameWeight, instanceWeight) =>
{
    var logger = loggerFactory.CreateLogger<Program>();

    logger?.LogInformation($"Ranking pending posts");

    var processor = new PostRankProcessor(db.GetWordsRank(), db.GetHostsRank())
    {
        WordsWeight = wordsWeight,
        BuzzWeight = buzzWeight,
        FameWeight = fameWeight,
        InstanceWeight = instanceWeight
    };
    
    var posts = db.GetPosts("SELECT * FROM posts WHERE score IS NULL", null, processor);

    logger?.LogInformation($"Posts pending to be ranked: {posts.Count()}");

    foreach (var post in posts)
    {
        db.UpdateRankedPost(post);
    }
}, wordsWeightOption, buzzWeightOption, fameWeightOption, instanceWeightOption);

rankPosts.AddOption(wordsWeightOption);
rankPosts.AddOption(buzzWeightOption);
rankPosts.AddOption(fameWeightOption);
rankPosts.AddOption(instanceWeightOption);

var boosPostsCmd = new Command("boost-posts", "Boost highest ranked posts");

boosPostsCmd.AddOption(accessTokenOption);
boosPostsCmd.AddOption(hostOption);
boosPostsCmd.AddOption(minimumWordsScoreOption);
boosPostsCmd.AddOption(minimumBuzzScoreOption);
boosPostsCmd.AddOption(limitOption);

boosPostsCmd.SetHandler(async (accessToken, host, minimumWordsScore, minimumBuzzScore, limit) =>
{
    var logger = loggerFactory.CreateLogger<Program>();

    var mastodonService = new MastodonService(host, accessToken)
    {
        Logger = logger
    };

    try
    {
        logger?.LogInformation($"Boosting posts in {host}");

        var processor = new PostRankProcessor(db.GetWordsRank(), db.GetHostsRank());

        logger?.LogInformation($"Getting {limit} posts with score > {minimumWordsScore} and buzz > {minimumBuzzScore}");

        var sql = "SELECT * FROM posts WHERE wordsScore > @wordsScore AND buzzScore > @buzzScore AND boosted IS NULL ORDER BY score DESC LIMIT @limit";

        var parameters = new Dictionary<string, object>
        {
            { "@wordsScore", minimumWordsScore },
            { "@buzzScore", minimumBuzzScore },
            { "@limit", limit }
        };
        
        var posts = db.GetPosts(sql, parameters);

        logger?.LogInformation($"{posts.Count()} retrieved");

        foreach (var post in posts)
        {
            await mastodonService.BoostPost(post);
            db.MarkPostAsBoosted(post);
        }
    } catch (Exception ex)
    {
        logger?.LogCritical(ex.ToString());
    }
   
}, accessTokenOption, hostOption, minimumWordsScoreOption, minimumBuzzScoreOption, limitOption);

var suggestedTags = new Command("suggested-tags", "Show suggested hashtags to follow");

rootCommand.AddCommand(suggestedTags);

suggestedTags.SetHandler(() =>
{
    var logger = loggerFactory.CreateLogger<Program>();

    var rows = db.GetWords("score > 30 AND score < 100 ORDER BY score DESC");

    foreach (var row in rows)
    {
        logger?.LogInformation($"Tag: {row.Tag} {row.Score}");
    }
});

rootCommand.AddCommand(fetchTimelinesCmd);
rootCommand.AddCommand(favoritesCmd);
rootCommand.AddCommand(tagsCmd);
rootCommand.AddCommand(rankPosts);
rootCommand.AddCommand(boosPostsCmd);

var result = await rootCommand.InvokeAsync(args);

return result;

static void ConfigureServices(ServiceCollection serviceCollection, string[] args)
{
    serviceCollection
        .AddLogging(configure =>
        {
            configure.AddSimpleConsole(options => options.TimestampFormat = "hh:mm:ss ");

            if (args.Any("--debug".Contains))
            {
                configure.SetMinimumLevel(LogLevel.Debug);
            }
        })
        .AddSingleton((sp) => new Db("myprofile.db"))
        .AddSingleton((sp) => new WeightsProfile());
}