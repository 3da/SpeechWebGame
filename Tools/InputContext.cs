using NAudio.Wave;

namespace SpeechWebGame.Tools
{
    public class InputContext
    {
        public Guid Id { get; init; }
        public TaskCompletionSource<string> TextResult { get; } = new TaskCompletionSource<string>();
        public TaskCompletionSource<IWaveProvider> AudioResult { get; } = new TaskCompletionSource<IWaveProvider>();


    }
}
