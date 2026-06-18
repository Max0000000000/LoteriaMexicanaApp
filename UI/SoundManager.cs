using System;
using System.IO;
using System.Media;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;

namespace LoteriaMexicanaApp.UI
{
    public class SoundManager
    {
        private readonly SpeechSynthesizer? _synth;
        private readonly SoundPlayer _bellPlayer;
        private byte[]? _bellWavBytes;

        public SoundManager()
        {
            try
            {
                _synth = new SpeechSynthesizer();
                UpdateVoice();

                // Adjust volume and rate
                _synth.Volume = 100;
                _synth.Rate = -1; // Speak slightly slower for clarity
            }
            catch
            {
                // Speech synthesis might fail on systems without standard audio output devices
                _synth = null;
            }

            // Synthesize the bell sound in memory
            _bellWavBytes = GenerateBellSoundBytes(1000, 0.4); // 1000 Hz, 0.4 seconds decay
            _bellPlayer = new SoundPlayer(new MemoryStream(_bellWavBytes));
            _bellPlayer.Load();
        }

        public void UpdateVoice()
        {
            if (_synth == null) return;
            try
            {
                string targetLang = TranslationManager.CurrentLanguage.Equals("EN", StringComparison.OrdinalIgnoreCase) ? "en" : "es";
                foreach (var voice in _synth.GetInstalledVoices())
                {
                    if (voice.VoiceInfo.Culture.TwoLetterISOLanguageName.Equals(targetLang, StringComparison.OrdinalIgnoreCase))
                    {
                        _synth.SelectVoice(voice.VoiceInfo.Name);
                        break;
                    }
                }
            }
            catch
            {
                // Ignore voice change errors
            }
        }

        /// <summary>
        /// Programmatically generates a sine wave bell chime with exponential decay.
        /// </summary>
        private byte[] GenerateBellSoundBytes(double frequency, double durationSeconds)
        {
            int sampleRate = 44100;
            short bitsPerSample = 16;
            short channels = 1;

            int sampleCount = (int)(sampleRate * durationSeconds);
            int dataSize = sampleCount * channels * (bitsPerSample / 8);
            int fileSize = 36 + dataSize;

            byte[] wav = new byte[44 + dataSize];

            // RIFF header
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("RIFF"), 0, wav, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(fileSize), 0, wav, 4, 4);
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("WAVE"), 0, wav, 8, 4);

            // fmt subchunk
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("fmt "), 0, wav, 12, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(16), 0, wav, 16, 4); // Subchunk size (16 for PCM)
            Buffer.BlockCopy(BitConverter.GetBytes((short)1), 0, wav, 20, 2); // Audio format (1 = PCM)
            Buffer.BlockCopy(BitConverter.GetBytes(channels), 0, wav, 22, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(sampleRate), 0, wav, 24, 4);

            int byteRate = sampleRate * channels * (bitsPerSample / 8);
            Buffer.BlockCopy(BitConverter.GetBytes(byteRate), 0, wav, 28, 4);

            short blockAlign = (short)(channels * (bitsPerSample / 8));
            Buffer.BlockCopy(BitConverter.GetBytes(blockAlign), 0, wav, 32, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(bitsPerSample), 0, wav, 34, 2);

            // data subchunk
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("data"), 0, wav, 36, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(dataSize), 0, wav, 40, 4);

            // Generate wave data (decaying sine wave)
            double tConstant = durationSeconds / 4.0; // Decay factor
            for (int i = 0; i < sampleCount; i++)
            {
                double time = (double)i / sampleRate;
                double amplitude = Math.Exp(-time / tConstant); // Exponential decay

                // Mix main frequency and a couple of harmonics for metallic timbre
                double angle1 = 2.0 * Math.PI * frequency * time;
                double angle2 = 2.0 * Math.PI * (frequency * 1.5) * time;
                double angle3 = 2.0 * Math.PI * (frequency * 2.0) * time;

                double sinVal = (Math.Sin(angle1) + 0.5 * Math.Sin(angle2) + 0.25 * Math.Sin(angle3)) / 1.75;
                short sample = (short)(sinVal * amplitude * short.MaxValue * 0.7);

                int offset = 44 + i * 2;
                Buffer.BlockCopy(BitConverter.GetBytes(sample), 0, wav, offset, 2);
            }

            return wav;
        }

        /// <summary>
        /// Plays the bell chime.
        /// </summary>
        public void PlayBell()
        {
            try
            {
                _bellPlayer.Play();
            }
            catch
            {
                // Suppress audio device errors
            }
        }

        /// <summary>
        /// Announces a card. First rings the bell, then speaks the name and optional riddle.
        /// </summary>
        public void CallCard(string cardName, string riddle = "")
        {
            Task.Run(() =>
            {
                try
                {
                    PlayBell();
                    // Wait a moment for the bell chime to finish playing
                    Task.Delay(350).Wait();

                    if (_synth != null)
                    {
                        // Clean up text-to-speech queues
                        _synth.SpeakAsyncCancelAll();

                        // Speak card name
                        _synth.Speak($"¡{cardName}!");

                        if (!string.IsNullOrWhiteSpace(riddle))
                        {
                            _synth.Speak(riddle);
                        }
                    }
                }
                catch
                {
                    // Suppress audio speech errors
                }
            });
        }
    }
}
