using Business.Audio;
using Google.Cloud.TextToSpeech.V1;
using Infrastructure.Audio;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TestScripts
{
    public class GoogleSpeechTest
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("=== Google Speech Service Test ===\n");

            // Initialize dependencies
            var client = TextToSpeechClient.Create();
            var cache = new FileAudioCache("test_voice_cache");
            var speechService = new GoogleSpeechService(client, cache);

            // Test cases
            var testCases = new[]
            {
                //new { Text = "Hello, this is a test in English.", Language = "en", Gender = VoiceGender.Female },
                new { Text = "của", Language = "vi", Gender = VoiceGender.Female },
                //new { Text = "", Language = "vi", Gender = VoiceGender.Female },
                //new { Text = "Bonjour, ceci est un test en français.", Language = "fr", Gender = VoiceGender.Male }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"\nGenerating audio for: \"{testCase.Text}\"");
                Console.WriteLine($"Language: {testCase.Language}, Gender: {testCase.Gender}");

                try
                {
                    // Generate speech
                    var audioBytes = await speechService.GenerateSpeechAsync(
                        testCase.Text,
                        testCase.Gender,
                        testCase.Language
                    );

                    Console.WriteLine($"✓ Audio generated successfully ({audioBytes.Length} bytes)");

                    // Save to temporary file
                    var tempFile = Path.Combine(Path.GetTempPath(), $"test_audio_{Guid.NewGuid()}.wav");
                    await File.WriteAllBytesAsync(tempFile, audioBytes);
                    Console.WriteLine($"✓ Saved to: {tempFile}");

                    // Play the audio
                    Console.WriteLine("Playing audio...");
                    PlayAudio(tempFile);

                    // Wait a bit before next test
                    await Task.Delay(2000);

                    // Clean up
                    try
                    {
                        //File.Delete(tempFile);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                }
            }

            Console.WriteLine("\n=== Test Complete ===");
        }

        private static void PlayAudio(string filePath)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Use PowerShell to play audio on Windows
                    var psi = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"(New-Object Media.SoundPlayer '{filePath}').PlaySync()\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    var process = Process.Start(psi);
                    process?.WaitForExit();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // Use aplay or paplay on Linux
                    var psi = new ProcessStartInfo
                    {
                        FileName = "aplay",
                        Arguments = filePath,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    var process = Process.Start(psi);
                    process?.WaitForExit();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Use afplay on macOS
                    var psi = new ProcessStartInfo
                    {
                        FileName = "afplay",
                        Arguments = filePath,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    var process = Process.Start(psi);
                    process?.WaitForExit();
                }
                else
                {
                    Console.WriteLine("⚠ Audio playback not supported on this platform");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Could not play audio: {ex.Message}");
                Console.WriteLine($"  Audio file saved at: {filePath}");
            }
        }
    }
}
