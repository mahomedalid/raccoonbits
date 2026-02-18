using AutoBoostBot;
using System.CommandLine;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSimpleConsole(options => options.TimestampFormat = "hh:mm:ss ");

    if (args.Any(a => a == "--debug"))
    {
        builder.SetMinimumLevel(LogLevel.Debug);
    }
});

var logger = loggerFactory.CreateLogger("AutoBoostBot");

string currentDirectory = Directory.GetCurrentDirectory();
string dbFilePath = Path.Combine(currentDirectory, "autoboost.db");

var db = new AutoBoostDb(dbFilePath);

// ── Shared options ──────────────────────────────────────────────────

var accessTokenOption = new Option<string>("--access-token", "Mastodon access token")
{
    IsRequired = true,
};

var hostOption = new Option<string>("--host", "Mastodon instance host (e.g. mastodon.social)")
{
    IsRequired = true,
};

var accountOption = new Option<string>("--account", "Account handle (e.g. user@instance.tld)")
{
    IsRequired = true,
};

var limitOption = new Option<int>("--limit", () => 40, "Max recent statuses to check per account");

// ── Root command ────────────────────────────────────────────────────

var rootCommand = new RootCommand("AutoBoostBot - Automatically boost posts from tracked Mastodon accounts");

// ── add ─────────────────────────────────────────────────────────────

var addCmd = new Command("add", "Add an account to the auto-boost list");
addCmd.AddOption(accountOption);

addCmd.SetHandler((account) =>
{
    db.AddAccount(account);
    logger.LogInformation("Added {Account} to auto-boost list", account);
}, accountOption);

// ── remove ──────────────────────────────────────────────────────────

var removeCmd = new Command("remove", "Remove an account from the auto-boost list");
removeCmd.AddOption(accountOption);

removeCmd.SetHandler((account) =>
{
    db.RemoveAccount(account);
    logger.LogInformation("Removed {Account} from auto-boost list", account);
}, accountOption);

// ── list ────────────────────────────────────────────────────────────

var listCmd = new Command("list", "List all tracked accounts");

listCmd.SetHandler(() =>
{
    var accounts = db.GetAccounts();

    if (accounts.Count == 0)
    {
        logger.LogInformation("No accounts on the auto-boost list. Use 'add' first.");
        return;
    }

    logger.LogInformation("Tracked accounts ({Count}):", accounts.Count);

    foreach (var acct in accounts)
    {
        logger.LogInformation("  {Account}", acct);
    }

    logger.LogInformation("Total posts boosted so far: {Count}", db.GetBoostedCount());
});

// ── run ─────────────────────────────────────────────────────────────

var runCmd = new Command("run", "Boost recent posts from all tracked accounts");
runCmd.AddOption(accessTokenOption);
runCmd.AddOption(hostOption);
runCmd.AddOption(limitOption);

runCmd.SetHandler(async (accessToken, host, limit) =>
{
    var mastodon = new MastodonClient(host, accessToken)
    {
        Logger = loggerFactory.CreateLogger<MastodonClient>()
    };

    var accounts = db.GetAccounts();

    if (accounts.Count == 0)
    {
        logger.LogInformation("No accounts configured. Use 'add' first.");
        return;
    }

    logger.LogInformation("Starting auto-boost run for {Count} account(s)", accounts.Count);

    var totalBoosted = 0;
    var totalSkipped = 0;

    foreach (var acct in accounts)
    {
        try
        {
            logger.LogInformation("Processing account: {Account}", acct);

            var accountId = await mastodon.LookupAccount(acct);

            if (accountId == null)
            {
                logger.LogWarning("Could not find account {Account}, skipping", acct);
                continue;
            }

            var statuses = await mastodon.GetAccountStatuses(accountId, limit);

            logger.LogInformation("Found {Count} recent statuses for {Account}", statuses.Count, acct);

            foreach (var status in statuses)
            {
                string statusUri = status.uri?.ToString() ?? status.url?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(statusUri))
                {
                    continue;
                }

                if (db.IsAlreadyBoosted(statusUri))
                {
                    totalSkipped++;
                    continue;
                }

                try
                {
                    string statusId = status.id?.ToString() ?? string.Empty;

                    if (!string.IsNullOrEmpty(statusId))
                    {
                        await mastodon.BoostStatusById(statusId);
                    }
                    else
                    {
                        var resolved = await mastodon.ResolveAndBoost(statusUri);

                        if (!resolved)
                        {
                            logger.LogWarning("Could not resolve {Uri}", statusUri);
                            continue;
                        }
                    }

                    db.MarkBoosted(statusUri, acct);
                    totalBoosted++;

                    logger.LogInformation("Boosted: {Uri}", statusUri);

                    // Rate-limit to avoid API throttling
                    await Task.Delay(2000);
                }
                catch (Exception ex)
                {
                    logger.LogError("Failed to boost {Uri}: {Message}", statusUri, ex.Message);
                }
            }

            // Pause between accounts
            await Task.Delay(3000);
        }
        catch (Exception ex)
        {
            logger.LogError("Error processing account {Account}: {Message}", acct, ex.Message);
        }
    }

    logger.LogInformation(
        "Auto-boost complete. Boosted: {Boosted}, Skipped (already boosted): {Skipped}",
        totalBoosted, totalSkipped);

}, accessTokenOption, hostOption, limitOption);

// ── Wire up ─────────────────────────────────────────────────────────

rootCommand.AddCommand(addCmd);
rootCommand.AddCommand(removeCmd);
rootCommand.AddCommand(listCmd);
rootCommand.AddCommand(runCmd);

return await rootCommand.InvokeAsync(args);
