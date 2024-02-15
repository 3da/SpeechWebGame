using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using NAudio.Wave;
using Vosk;

namespace SpeechWebGame.Tools
{
    public class VoiceCapture : IDisposable
    {
        private readonly VoskRecognizer _recognizer;
        SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public VoiceCapture(int sampleRate = 24000)
        {
            //Model model = new Model(@"F:\VoskModels\vosk-model-ru-0.42");
            Model model = new Model(@"F:\VoskModels\vosk-model-small-ru-0.22");
            //Model model = new Model("vosk-model-small-ru-0.22");
            _recognizer = new VoskRecognizer(model, sampleRate);
            _recognizer.SetMaxAlternatives(3);
            _recognizer.SetWords(true);
        }

        public event Action<string> PartialResultReady;


        public async Task<string[]> CaptureAsync(IWaveProvider waveProvider,
            TimeSpan duration,
            Func<string, TimeSpan, bool>? testFunc = default,
            CancellationToken cancellationToken = default)
        {
            _recognizer.Reset();


            var cts1 = new CancellationTokenSource();
            var ctoken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(duration).Token, cts1.Token);

            var hasInput = 0;



            cancellationToken = ctoken.Token;
            var captureTask = Task.Run(async () =>
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    const int bufferSize = 1024;
                    var buffer = new byte[bufferSize];

                    var readCount = waveProvider.Read(buffer, 0, bufferSize);

                    if (readCount == 0)
                        break;

                    Interlocked.Exchange(ref hasInput, 1);
                    await _semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        _recognizer.AcceptWaveform(buffer, readCount);
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                ctoken.Cancel();

            }, cancellationToken);

            var processPartialResultTask = Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                bool TestFunc(string s) => testFunc.Invoke(s, sw.Elapsed);
                while (true)
                {
                    if (Interlocked.CompareExchange(ref hasInput, 0, 1) == 1)
                    {
                        await _semaphore.WaitAsync(cancellationToken);
                        string partialResult;
                        try
                        {
                            partialResult = _recognizer.PartialResult();
                        }
                        finally
                        {
                            _semaphore.Release();
                        }

                        if (ProcessPartialResult(partialResult, testFunc != null ? TestFunc : null))
                        {
                            cts1.CancelAfter(300);
                            return;
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                        await Task.Delay(700, ctoken.Token);
                    }
                    else
                        await Task.Delay(100, ctoken.Token);
                }
            });



            Console.WriteLine("Ожидается голосовой ввод");

            string[] result;
            try
            {
                await Task.WhenAll(captureTask, processPartialResultTask);
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                var finalResult = _recognizer.FinalResult();

                var node = JsonSerializer.Deserialize<VoskFinalResult>(finalResult, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                });

                result = node.Alternatives.Select(q => q.Text).ToArray();
            }
            return result;
        }

        private bool ProcessPartialResult(string partialResult, Func<string, bool>? testFunc)
        {
            var node = JsonSerializer.Deserialize<JsonNode>(partialResult);
            var partial = node["partial"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(partial))
            {
                if (testFunc?.Invoke(partial) == true)
                {
                    return true;
                }

                OnPartialResultReady(partial);
            }

            return false;
        }

        public void Dispose()
        {
            _recognizer.Dispose();
        }

        protected virtual void OnPartialResultReady(string obj)
        {
            PartialResultReady?.Invoke(obj);
        }
    }
}
