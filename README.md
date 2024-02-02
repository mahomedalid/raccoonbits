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

## MVP

Because we don't like in an ideal world, I made the following design and technical decisions:

1. Do not require any cloud-based service, running locally for now with a command line interface.
2. All information is stored in local sqlite databases.
3. It will boost the content through a personalized bot, eliminated the need of creating and maintaining a whole UX, service or mobile app to show the curated feed.
4. Use dotnet because is fun.
5. Process, adjust algorithm, and boost content on-demand (manual execution of cli), and manually (edit of algorith weights).
6. It considers words score (words and tags used in the posts are relevant for you), fame score (based in the author followers), buzz score (based in the favorites and reposts) and host score (based in how relevant the mastodon instance is for you).
7. Scores have weights, but also are normalized, to avoid an outlier in one score to disproportionately impact the overall content curation, ensuring a balanced and meaningful user experience.

## ROADMAP

After having the MVP working the next priorities will be:

1. Enhance the algorithm with LLMs vector embeddings, replacing the simple words score.
2. Enhance usability by providing a front-end/UI to configure the algorithm, and schedule the feed creation.
3. Create a cloud-based service that can use OAuth to have a wizard where non-tech users do not need to setup anything.

> Note: **This is not a search engine.** Data is frugal, and we only boost curated content.
