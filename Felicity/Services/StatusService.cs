﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Felicity.Services;

internal static class StatusService
{
    private static readonly Random rnd = new();

    private static readonly List<Game> gameList = new()
    {
        new Game("Clan Graphic on top", ActivityType.Watching),
        new Game("Destiny 3"),
        new Game("you 👀", ActivityType.Watching),
        new Game("Leaf break stuff 🔨", ActivityType.Watching),
        new Game("what does air taste like?", ActivityType.CustomStatus),
        new Game("you break the rules", ActivityType.Watching),
        new Game("Juice WRLD", ActivityType.Listening),
        new Game("Google Chrome"),
        new Game("$10k qp tourney", ActivityType.Competing),
        new Game("Pornhub VR"),
        new Game("ttv/purechill", ActivityType.Watching),
        new Game("sweet bird sounds", ActivityType.Listening),
        new Game("Felicity ... wait", ActivityType.Watching)
    };

    public static DiscordSocketClient _client;

    private static Game LastGame { get; set; }

    public static async void ChangeGame()
    {
        Game newGame;
        do
        {
            newGame = gameList[rnd.Next(gameList.Count)];
        } while (newGame == LastGame);

        try
        {
            await Task.Delay(1000);
            await _client.SetActivityAsync(newGame);
            LastGame = newGame;
        }
        catch
        {
            // ignored
            // client isn't ready or disconnected temporarily, not a big deal.
        }
    }
}