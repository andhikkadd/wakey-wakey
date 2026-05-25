using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace SolveAlarm.Services
{
    public class AudioService : IDisposable
    {
        private IWavePlayer? _wavePlayer;
        private WaveStream? _waveStream;
        private Timer? _volumeTimer;
        private readonly object _lock = new object();
        private bool _isPlaying = false;

        public void PlayAlarm(string? soundFilePath)
        {
            lock (_lock)
            {
                if (_isPlaying) return;
                _isPlaying = true;

                // 1. Force 100% volume and unmute immediately
                ForceMaxVolumeAndUnmute();

                // 2. Start the 250ms volume enforcement loop
                _volumeTimer = new Timer(VolumeTimerCallback, null, 0, 250);

                // 3. Setup playback
                try
                {
                    _wavePlayer = new WaveOutEvent();

                    if (!string.IsNullOrEmpty(soundFilePath) && File.Exists(soundFilePath) && IsSupportedFormat(soundFilePath))
                    {
                        try
                        {
                            var reader = new AudioFileReader(soundFilePath);
                            _waveStream = new LoopStream(reader);
                            _wavePlayer.Init(_waveStream);
                            Debug.WriteLine($"Playing custom sound: {soundFilePath}");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error playing custom sound, falling back to synth: {ex.Message}");
                            PlayFallbackSynth();
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Custom sound path missing or invalid, using synth fallback.");
                        PlayFallbackSynth();
                    }

                    _wavePlayer.Play();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Critical error during audio init: {ex.Message}");
                }
            }
        }

        private void PlayFallbackSynth()
        {
            var synthProvider = new AlarmSampleProvider();
            _wavePlayer!.Init(synthProvider);
        }

        public void StopAlarm()
        {
            lock (_lock)
            {
                if (!_isPlaying) return;
                _isPlaying = false;

                // Stop volume timer
                _volumeTimer?.Dispose();
                _volumeTimer = null;

                // Stop playback
                try
                {
                    _wavePlayer?.Stop();
                    _wavePlayer?.Dispose();
                    _wavePlayer = null;

                    _waveStream?.Dispose();
                    _waveStream = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error stopping audio: {ex.Message}");
                }
            }
        }

        public static bool IsSupportedFormat(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            string ext = Path.GetExtension(filePath).ToLower();
            return ext == ".wav" || ext == ".mp3";
        }

        public static bool ValidateSoundFile(string? filePath, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrEmpty(filePath)) return true; // Empty is fine, fallback will be used

            if (!File.Exists(filePath))
            {
                errorMessage = "File does not exist.";
                return false;
            }

            if (!IsSupportedFormat(filePath))
            {
                errorMessage = "Unsupported format. Only .wav and .mp3 are supported.";
                return false;
            }

            return true;
        }

        private void VolumeTimerCallback(object? state)
        {
            ForceMaxVolumeAndUnmute();
        }

        private void ForceMaxVolumeAndUnmute()
        {
            try
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    using (var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                    {
                        if (device != null)
                        {
                            if (device.AudioEndpointVolume.Mute)
                            {
                                device.AudioEndpointVolume.Mute = false;
                                Debug.WriteLine("Audio endpoint was muted. Unmuted.");
                            }

                            if (device.AudioEndpointVolume.MasterVolumeLevelScalar < 1.0f)
                            {
                                device.AudioEndpointVolume.MasterVolumeLevelScalar = 1.0f;
                                Debug.WriteLine("Volume was below 100%. Set to 100%.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // CoreAudio endpoints may fail if no playback device is connected.
                Debug.WriteLine($"Error enforcing volume: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopAlarm();
        }
    }

    /// <summary>
    /// Custom sample provider to synthesize a loud, high-intensity oscillating siren alarm.
    /// Eliminates the need for external sound assets.
    /// </summary>
    public class AlarmSampleProvider : ISampleProvider
    {
        private double _phase;
        private readonly int _sampleRate = 44100;
        private int _currentSample = 0;

        public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Pattern: 400ms on, 200ms off
                int totalMs = (int)((long)_currentSample * 1000 / _sampleRate);
                bool soundOn = (totalMs % 600) < 400;

                if (soundOn)
                {
                    // Siren oscillates between 800Hz and 1400Hz every 200ms
                    double oscillationTime = (totalMs % 400) / 400.0;
                    double freq = 800 + 600 * Math.Sin(oscillationTime * Math.PI);
                    
                    double phaseStep = 2 * Math.PI * freq / _sampleRate;
                    _phase += phaseStep;
                    
                    // Generate square/sine mixed wave for extra harsh alarm buzz
                    double primarySine = Math.Sin(_phase);
                    double harmonic = Math.Sin(_phase * 2) * 0.3; // harsh overtones
                    buffer[offset + i] = (float)(0.6 * (primarySine + harmonic));
                }
                else
                {
                    buffer[offset + i] = 0.0f;
                }

                _currentSample++;
                if (_currentSample >= _sampleRate * 60) // Reset counter every minute to avoid overflow
                {
                    _currentSample = 0;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Standard NAudio stream wrapper to support looping.
    /// </summary>
    public class LoopStream : WaveStream
    {
        private readonly WaveStream _sourceStream;

        public LoopStream(WaveStream sourceStream)
        {
            _sourceStream = sourceStream;
        }

        public override WaveFormat WaveFormat => _sourceStream.WaveFormat;
        public override long Length => _sourceStream.Length;
        public override long Position
        {
            get => _sourceStream.Position;
            set => _sourceStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0)
                {
                    if (_sourceStream.Position == 0)
                    {
                        break; // End of stream or unseekable
                    }
                    _sourceStream.Position = 0; // Loop back
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _sourceStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
