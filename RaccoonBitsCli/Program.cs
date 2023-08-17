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