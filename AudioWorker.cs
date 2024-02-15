using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;

namespace SpeechWebGame
{
    public class AudioWorker
    {
        private ConcurrentDictionary<Guid, Pipe> _audioStreams = new();

        public Stream GetAudio(Guid id)
        {
            //_audioStreams.TryGetValue(id, out var pipe);
            if (!_audioStreams.Remove(id, out var pipe))
            {
                throw new Exception();
            }
            var stream = pipe.Reader.AsStream();
            return stream;
        }

        public async Task CreateSoundAsync(Guid id, Pipe source)
        {
            _audioStreams.TryAdd(id, source);
        }



    }
}
