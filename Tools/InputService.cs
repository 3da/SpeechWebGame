using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using NAudio.Wave;

namespace SpeechWebGame.Tools
{
    public class InputService
    {
        private readonly ConcurrentDictionary<Guid, InputContext> _concurrentDictionary = new();

        public async Task<string[]> GetInputAsync(
            ISingleClientProxy client,
            bool wideAnswer,
            TimeSpan timeout,
            Func<string, TimeSpan, bool>? testFunc = null,
            CancellationToken cancellationToken = default,
            params string[] variants)
        {
            var visibleCases = variants;


            var context = _concurrentDictionary.GetOrAdd(Guid.NewGuid(), k => new InputContext()
            {
                Id = k
            });
            var id = context.Id;

            await client.SendAsync("input", id, visibleCases, wideAnswer, timeout, cancellationToken: cancellationToken);

            var sw = Stopwatch.StartNew();
            using var cts = new CancellationTokenSource(timeout.Add(TimeSpan.FromSeconds(500)));
            var token = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken).Token;
            await using var reg = token.Register(() =>
            {
                context.AudioResult.TrySetCanceled(token);
                context.TextResult.TrySetCanceled(token);
            });

            bool TestFunc(string str, TimeSpan time)
            {
                if (variants.Any(v => v.Equals(str, StringComparison.InvariantCultureIgnoreCase)))
                    return true;
                return testFunc?.Invoke(str, time) == true;
            }

            string[]? result = null;
            try
            {
                var captureAudioTask = Task.Run(async () =>
                {
                    try
                    {
                        var wave = await context.AudioResult.Task;

                        using var audioCapture = new VoiceCapture(wave.WaveFormat.SampleRate);
                        audioCapture.PartialResultReady += s =>
                        {
                            client.SendAsync("partialInput", id, s, cancellationToken: token);
                        };
                        return await audioCapture.CaptureAsync(wave, timeout - sw.Elapsed, testFunc: TestFunc, cancellationToken: token);
                    }
                    catch (OperationCanceledException)
                    {
                        return Array.Empty<string>();
                    }
                }, token);

                var captureTextTask = Task.Run(async () =>
                {
                    try
                    {
                        var text = await context.TextResult.Task;
                        return new[] { text };
                    }
                    catch (OperationCanceledException)
                    {
                        return Array.Empty<string>();
                    }
                }, token);

                var completedTask = await Task.WhenAny(captureTextTask, captureAudioTask);

                if (captureTextTask.IsCompleted && captureAudioTask.IsCompleted)
                {
                    return captureAudioTask.Result.Concat(captureTextTask.Result).ToArray();
                }

                result = completedTask.Result;
                return result;
            }
            finally
            {
                context.TextResult.TrySetCanceled();
                context.AudioResult.TrySetCanceled();
                await client.SendAsync("stopInput", id, result, cancellationToken: token);
            }

        }

        public void SetInputResult(Guid id, IWaveProvider waveProvider)
        {
            if (_concurrentDictionary.TryGetValue(id, out var context))
            {
                context.AudioResult.TrySetResult(waveProvider);
            }
        }

        public void SetInputResult(Guid id, string text)
        {
            if (_concurrentDictionary.TryGetValue(id, out var context))
            {
                context.TextResult.TrySetResult(text);
            }
        }
    }
}
