using System.Collections.Concurrent;
using NAudio.Lame;
using NAudio.Wave;
using System.IO.Pipelines;
using System.Runtime.Versioning;
using System.Speech.AudioFormat;
using Microsoft.AspNetCore.SignalR;
using SpeechWebGame.Tools;

namespace SpeechWebGame
{
    [SupportedOSPlatform("windows")]
    public class SpeechService
    {
        private readonly AudioWorker _audioWorker;

        private readonly ConcurrentDictionary<Guid, TaskCompletionSource> _speeches =
            new ConcurrentDictionary<Guid, TaskCompletionSource>();

        public SpeechService(AudioWorker audioWorker)
        {
            _audioWorker = audioWorker;
        }

        public async Task PlaySpeechAsync(ISingleClientProxy client, string text, string? displayText = null, int rate = 0, CancellationToken cancellationToken = default)
        {
            var pipe = new Pipe();

            var str = pipe.Writer.AsStream();

            _ = Task.Run(async () =>
            {
                try
                {
                    var speech = new System.Speech.Synthesis.SpeechSynthesizer();
                    speech.Rate = rate;
                    SpeechAudioFormatInfo synthFormat = new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Mono);
                    var wavWriter = new LameMP3FileWriter(new StreamWrapper(str), new WaveFormat(48000, 1), LAMEPreset.EXTREME);
                    speech.SetOutputToAudioStream(wavWriter, synthFormat);

                    speech.SpeakAsync(text);
                    speech.SpeakCompleted += async (_, q) =>
                    {
                        speech.Dispose();
                        await pipe.Writer.CompleteAsync(q.Error);

                    };
                }
                catch (Exception e)
                {
                    await pipe.Writer.CompleteAsync(e);
                }
            }, cancellationToken);

            var id = Guid.NewGuid();
            _ = Task.Run(() => _audioWorker.CreateSoundAsync(id, pipe));
            var tcs = new TaskCompletionSource();
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var token = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token;
            await using var reg = token.Register(() => tcs.SetCanceled(token));
            _speeches.TryAdd(id, tcs);
            await client.SendAsync("audio", id, displayText ?? text, cancellationToken: token);
            await tcs.Task;
        }

        public void ListenSpeechComplete(Guid id)
        {
            if (_speeches.TryGetValue(id, out var tcs))
            {
                tcs.TrySetResult();
            }
        }


    }
}
