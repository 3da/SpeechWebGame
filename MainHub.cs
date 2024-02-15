using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Runtime.Versioning;
using System.Speech.AudioFormat;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using NAudio.Lame;
using NAudio.Wave;
using SpeechWebGame.Games;
using SpeechWebGame.Tools;

namespace SpeechWebGame
{
    [SupportedOSPlatform("windows")]
    public class MainHub : Hub
    {
        private readonly SpeechService _speechService;
        private readonly InputService _inputService;
        private readonly IServiceProvider _serviceProvider;
        private readonly GameRunner _gameRunner;

        private static ConcurrentDictionary<string, CancellationTokenSource> _gameTokens = new();

        public MainHub(
            SpeechService speechService,
            InputService inputService,
            IServiceProvider serviceProvider,
            GameRunner gameRunner)
        {
            _speechService = speechService;
            _inputService = inputService;
            _serviceProvider = serviceProvider;
            _gameRunner = gameRunner;
        }

        public override async Task OnConnectedAsync()
        {
        }


        public async Task StartGameAsync(string gameId)
        {
            var cts = _gameTokens.AddOrUpdate(Context.ConnectionId, _ => new CancellationTokenSource(), (_, source) =>
            {
                source.Cancel();
                source.Dispose();
                return new CancellationTokenSource();
            });

            var token = cts.Token;

            await _speechService.PlaySpeechAsync(Clients.Caller, "Добро пожаловать в игру!", cancellationToken: token);
            
            await _gameRunner.RunAsync(Clients.Caller, gameId, token);
        }

        public void StopGame()
        {
            if (_gameTokens.TryRemove(Context.ConnectionId, out var cts))
                cts.Cancel();
        }

        public GameInfo[] GetGames()
        {
            return _gameRunner.Games;
        }


        public async Task SendSpeechAsync(ChannelReader<byte[]> stream, Guid id, int sampleRate)
        {
            var pipe2 = new Pipe();

            var writer = pipe2.Writer;

            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            var reader = new RawSourceWaveStream(new StreamWrapper(pipe2.Reader.AsStream()), waveFormat);
            var sampleProvider = reader.ToSampleProvider();
            var wave16 = sampleProvider.ToWaveProvider16();

            _inputService.SetInputResult(id, wave16);

            await foreach (var buffer in stream.ReadAllAsync())
            {
                await writer.WriteAsync(buffer);
            }

            await writer.CompleteAsync();
        }

        public async Task SendTextAsync(Guid id, string text)
        {
            _inputService.SetInputResult(id, text);
        }

        public void ListenSpeechComplete(Guid id)
        {
            _speechService.ListenSpeechComplete(id);
        }
    }
}
