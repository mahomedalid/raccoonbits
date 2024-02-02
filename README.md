# RaccoonBits

The algorithm for mastodon - a personalized curated fediverse timeline bot. 

The idea that started this project is based on [this blog post](https://www.maho.dev/2023/08/i-miss-the-algorithm-i-want-something-for-my-mastodon/).

## Goals

On an ideal world with unlimited development resources this project would allow to:

1. Generate a curated and personalized feed from posts across the fediverse.
2. Be as little as intrusive as possible for others.
3. Be able to be setup and used easily by any non-tech user.
4. Give complete flexibility to understand and adjust the algorithm.
5. Automatically learn from your activity (likes/reposts) to improve the curated algorithm.
6. Be frugal, not store any content besides the needs for generate the personalized feed.

### MVP

Because we don't like in an ideal world, I made the following design and technical decisions:

1. Do not require any cloud-based service, running locally for now with a command line interface.
2. All information is stored in local sqlite databases.
3. It will boost the content through a personalized bot, eliminated the need of creating and maintaining a whole UX, service or mobile app to show the curated feed.
4. Use dotnet because is fun.
5. Process, adjust algorithm, and boost content on-demand (manual execution of cli), and manually (edit of algorith weights).
6. It considers words score (words and tags used in the posts are relevant for you), fame score (based in the author followers), buzz score (based in the favorites and reposts) and host score (based in how relevant the mastodon instance is for you).
7. Scores have weights, but also are normalized, to avoid an outlier in one score to disproportionately impact the overall content curation, ensuring a balanced and meaningful user experience.

### Roadmap

After having the MVP working the next priorities will be:

1. Enhance the algorithm with LLMs vector embeddings, replacing the simple words score.
2. Enhance usability by providing a front-end/UI to configure the algorithm, and schedule the feed creation.
3. Create a cloud-based service that can use OAuth to have a wizard where non-tech users do not need to setup anything.

> Note: **This is not a search engine.** Data is frugal, and we only boost curated content.

## Getting Started

For now the whole thing is run through a command line.

### 1. Get an API access token for your account

The utility will require access to the API of the mastodon instance of the user that wants the content curated. To get access the utility requires an access token. This is how to obtain one:

1. Sign into your Mastodon account
1. Go to your preferences (usually there is a link in the bottom right corner of your home page).
1. In the bottom left corner of your your preferences page, click the Developers link.
1. On the Your applications page, click the blue **NEW APPLICATION** button
1. Give your application a name, and give **only read** permissions to at least `accounts`, `bookmarks`, and `favourites`. We do not need write. follow, adming, nor push permissions.
1. At the bottom of the page, click the blue `SUBMIT` button
1. You will be directed back to the Your applications page, but now you should see your application name. Click in that application.
1. In your application's page, there are three tokens. You need `Your access token one`. This token will give to anyone who have it access to read your bookmarks and favourites, so be as careful as you need.

Note: if your access token is ever compromised, you can click regenerate, and your old access token will stop working, and you'll be shown a new one. 

### 2. Setup the Bot that will boost curated content

You will need to create a bot account that will boost the content. Not all mastodon instances allow bots, or they have different rules, or approval process, so you may need to be careful and thoughtful where to host it.

The setup of a bot account in most of the mastodon instances is very similar to creating a regular account, once that you have an approved bot account follow the steps on the step one to get an access token, **just that this time it will need additional write permissions: `read`, `read:favourites`, `write:favourites`, `write:statuses`.

### 3. Setup RaccoonBits CLI

1. Download the latest version for Windows, Linux or MacOs from the [Releases](https://github.com/mahomedalid/raccoonbits/releases) page and decompress it in a folder.
2. Open a terminal and go to the folder.
3. Run `./RaccoonBitsCli` to see the options.

```bash
$ ./RaccoonBitsCli
Required command was not provided.

Description:

Usage:
  RaccoonBitsCli [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  suggested-tags   Show suggested hashtags to follow
  fetch-timelines  Fetch public timelines from instances
  favorites        Retrieves favorites from mastodon and updates the algorithm profile
  tags             Retrieves tags from mastodon and updates the algorithm profile
  rank-posts       Rank posts
  boost-posts      Boost highest ranked posts
```

### 4. Initialize your algorithm profile

Initialize your algorithm profile by retrieving favorites and tags. You will need the access token on step 1. Example:

```bash
# This will retrieve favorites from the mastodon instance hachyderm.io
./RaccoonBitsCli favorites --host hachyderm.io --accessToken <MYACCESSTOKEN>
```
   
```bash
# This will retrieve tags from the mastodon instance hachyderm.io
./RaccoonBitsCli tags --host hachyderm.io --accessToken <MYACCESSTOKEN>
```

Favorites will be the base for the words score and host score. The logic is that the more favorites of certain instance you have, the more likely is that posts from that mastodon instance are relevant for you. The same way, we analyze the words and specially tags, to find posts in the fediverse that are relevant (even if those posts does not use the # character).

### 5. Fetch timelines and rank posts

1. This step will iterate over all the mastodon instances where you have already favorited a post, and fetch the public timelines of that instant.

```bash
./RaccoonBitsCli fetch-timelines
```

You can adjust the minimum threshold of instances to be scrapped. For example, if you want to start retrieving posts of an instance until you liked at least 10 posts of such instance you can run:

```bash
./RaccoonBitsCli fetch-timelines --weight 10
```

You can also use the weight, to create an script that fetch instances that are very relevant for you each minute (ex. --weight 100), and other instances each 10 minutes or each hour.

2. The previous step will populate a local sqlite database with posts, but we will rank them in a separate step.

```bash
./RaccoonBitsCli rank-posts
```

### 6. Boost posts

Now is time to wake up the bot, boost posts that you may lost in the noise and infinite time-space. For example, for a bot hosted in techhub.social as [@raccoonbits](https://techhub.social/users/raccoonbits):

```
./RaccoonBitsCli  boost-posts --accessToken <BOTACCESSTOKEN> --host techhub.social
```

> Note: The access token is different from the one used to get your profile, this one is for the bot.

All scores are normalized in a decimal between 0 (low-relevance) to 1 (high-relevance, for example, already liked posts). 
You can modify the threshold that posts need to pass to be boosted (the default is 0.5 for words):

```
./RaccoonBitsCli boost-posts --words-score 0.7
```

Posts that are boosted are marked so they don't boost again. The bot will boost max 5 posts in each run, but if the process that fetch posts, obtains more than 5 relevant posts and/or run more often than the command to boost posts, you may want to modify this value:

```
./RacoonBitsCli boost-posts --limitOption 10
```

I have also found useful to combine both options, to boost posts with very high words more often, but if my bot goes quiet boost more posts to get more content.

## Combining all together

You may find that running all these commands manually is a hassle, but you can combine all in one script, this is my example for bash, which can be read as:

- Each 20 minutes fetch timelines and rank posts
- Each 5 minutes boost maximum 5 posts
- Each ~1.5 hrs update my user profile/algorithm by retrieving tags or new favorites

```bash
#!/bin/bash

count=0

while true; do
    date

    if [ $((count % 4)) -eq 0 ]; then
        echo "Running the algorithm"
        ./RaccoonBitsCli fetch-timelines
        ./RaccoonBitsCli rank-posts
        echo "done"
    fi

    echo "Waking up the bot"
    dotnet run -- boost-posts -- accessToken $BOT_ACCESS_TOKEN --host techhub.social
    echo "done"

    if [ $((count + 1 % 20)) -eq 0 ]; then
        echo "Updating user profile on likes"
        ./RaccoonBitsCli favorites --host hachyderm.io --accessToken $MY_ACCESS_TOKEN
        ./RaccoonBitsCli tags --host hachyderm.io --accessToken $MY_ACCESS_TOKEN
        echo "done"
    fi

    ((count++))

    echo "Sleeping 5 minutes ..."
    sleep 300  # Sleep for 5 minutes (300 seconds)
```
# Contribution

Contributions are welcome! If you have additional samples, improvements, or ideas, please open an issue or submit a pull request. I REALLY NEED HELP.

# License

This repository is licensed under the MIT License - see the LICENSE file for details. Feel free to use, modify, and share in accordance with the license terms.
