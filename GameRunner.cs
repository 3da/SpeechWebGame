using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using SpeechWebGame.Games;

namespace SpeechWebGame
{
    public class GameRunner
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly GameInfo[] _gameInfos;
        private readonly Dictionary<string?, Type> _dict;

        public GameRunner(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var interf = typeof(ISpeechGame);
            var gameTypes = Assembly.GetExecutingAssembly().ExportedTypes.Where(q => interf.IsAssignableFrom(q)).ToArray();
            var gameModes = gameTypes.Select(t => new { Type = t, Info = t.GetProperty("GameInfo")?.GetValue(null) as GameInfo })
                .Where(q => q.Info?.Id != null).ToArray();
            _gameInfos = gameModes.Select(q => q.Info).ToArray()!;
            _dict = gameModes.ToDictionary(q => q.Info!.Id!, q => q.Type, StringComparer.InvariantCultureIgnoreCase)!;
        }

        public async Task RunAsync(ISingleClientProxy client, string gameId, CancellationToken token = default)
        {
            if (!_dict.TryGetValue(gameId, out var gameType))
            {
                throw new NotImplementedException();
            }

            var game = (ISpeechGame)ActivatorUtilities.CreateInstance(_serviceProvider, gameType);

            await game.RunAsync(client, token);
        }

        public GameInfo[] Games => _gameInfos;
    }
}
