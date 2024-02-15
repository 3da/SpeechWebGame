using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NAudio.Wave;
using System.Runtime.InteropServices;

namespace SpeechWebGame.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AudioController : ControllerBase
    {
        private readonly AudioWorker _audioWorker;

        public AudioController(AudioWorker audioWorker)
        {
            _audioWorker = audioWorker;
        }

        [HttpGet]
        public async Task<IActionResult> GetAudioAsync(Guid id)
        {
            var audioStream = _audioWorker.GetAudio(id);

            return File(audioStream, "audio/mp3", "audio.mp3");
        }

        
    }
}
