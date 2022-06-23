﻿using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Felicity.Options;
using Felicity.Util;
using Microsoft.Extensions.Options;

namespace Felicity.Services.Hosted;

public class DiscordStartupService : BackgroundService
{
    private readonly LogAdapter<BaseSocketClient> _adapter;
    private readonly CommandService _commandService;
    private readonly IOptions<DiscordBotOptions> _discordBotOptions;
    private readonly DiscordShardedClient _discordShardedClient;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private int _shardsReady;
    private TaskCompletionSource<bool>? _taskCompletionSource;

    public DiscordStartupService(
        DiscordShardedClient discordShardedClient,
        IOptions<DiscordBotOptions> discordBotOptions,
        InteractionService interactionService,
        CommandService commandService,
        IServiceProvider serviceProvider,
        LogAdapter<BaseSocketClient> adapter)
    {
        _discordShardedClient = discordShardedClient;
        _discordBotOptions = discordBotOptions;
        _interactionService = interactionService;
        _commandService = commandService;
        _serviceProvider = serviceProvider;
        _adapter = adapter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordShardedClient.Log += async logMessage =>
        {
            await _adapter.Log(logMessage);
        };

        PrepareClientAwaiter();
        await _discordShardedClient.LoginAsync(TokenType.Bot, _discordBotOptions.Value.Token);
        await _discordShardedClient.StartAsync();
        await WaitForReadyAsync(stoppingToken);

        // load text commands
        await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        // load interactions
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

        // register your commands here
        //await _interactionService.RegisterCommandsToGuildAsync();
    }

    private void PrepareClientAwaiter()
    {
        _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _shardsReady = 0;

        _discordShardedClient.ShardReady += OnShardReady;
    }

    private Task OnShardReady(DiscordSocketClient _)
    {
        _shardsReady++;
        if (_shardsReady != _discordShardedClient.Shards.Count)
            return Task.CompletedTask;

        _taskCompletionSource!.TrySetResult(true);
        _discordShardedClient.ShardReady -= OnShardReady;

        return Task.CompletedTask;
    }

    private Task WaitForReadyAsync(CancellationToken cancellationToken)
    {
        if (_taskCompletionSource is null)
            throw new InvalidOperationException(
                "The sharded client has not been registered correctly. Did you use ConfigureDiscordShardedHost on your HostBuilder?");

        if (_taskCompletionSource.Task.IsCompleted)
            return _taskCompletionSource.Task;

        var registration = cancellationToken.Register(
            state => { ((TaskCompletionSource<object>) state!).TrySetResult(null!); },
            _taskCompletionSource);

        return _taskCompletionSource.Task.ContinueWith(_ => registration.DisposeAsync(), cancellationToken);
    }
}