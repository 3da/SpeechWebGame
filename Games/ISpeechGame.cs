using Microsoft.AspNetCore.SignalR;

namespace SpeechWebGame.Games;

public interface ISpeechGame
{
    Task RunAsync(ISingleClientProxy client, CancellationToken token);
}