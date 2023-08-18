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

var accessTokenOption = new Option<string>("--accessToken", () => Environment.GetEnvironmentVariable("MASTODON_ACCESS_TOKEN") ?? "", "Access token for Mastodon");
var hostOption = new Option<string>("--host", () => Environment.GetEnvironmentVariable("MASTODON_HOST") ?? "", "Mastodon host");
var weightOption = new Option<int>("--weight", () => 5, "Minimum score (number of liked pods) for instances");

var favoritesCmd = new Command("favorites", "Retrieves favorites from mastodon and updates the algorithm profile");

favoritesCmd.AddOption(accessTokenOption);

favoritesCmd.SetHandler(async (accessToken, host) =>
{
    var logger = serviceProvider.GetRequiredService<ILogger>();

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

tagsCmd.SetHandler(async (accessToken, host) =>
{
    var logger = serviceProvider.GetRequiredService<ILogger>();

    var mastodonService = new MastodonService(host, accessToken)
    {
        Logger = logger
    };

    var processor = new TagsAnalyzer(db, weightsProfile);
    await mastodonService.FetchHashtags(processor);
}, accessTokenOption, hostOption);

var fetchTimelinesCmd = new Command("fetch-timelines", "Fetch public timelines from instances");

fetchTimelinesCmd.SetHandler(async (weight) =>
{
    var logger = serviceProvider.GetRequiredService<ILogger>();

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

rankPosts.SetHandler(() =>
{
    var logger = serviceProvider.GetRequiredService<ILogger>();

    var processor = new PostRankProcessor(db.GetWordsRank(), db.GetHostsRank());

    var posts = db.GetPosts("score IS NULL", processor);

    foreach (var post in posts)
    {
        db.UpdateRankedPost(post);
    }
});


rootCommand.AddCommand(fetchTimelinesCmd);
rootCommand.AddCommand(favoritesCmd);
rootCommand.AddCommand(tagsCmd);

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