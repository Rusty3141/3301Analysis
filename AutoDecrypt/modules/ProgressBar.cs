using System;
using System.Text;
using System.Threading;

/// <summary>
/// Class <c>ProgressBar</c> adds an animated progress bar for long operations to the console.
/// It works with redirected output, implements IProgress<T>; is thread-safe and performant.
/// This class only: https://opensource.org/licenses/MIT, adapted from https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54.
/// </summary>
namespace AutoDecrypt.modules
{
    internal class ProgressBar : IDisposable, IProgress<float>
    {
        private const int barResolution = 20;
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1f / 8);
        private readonly Timer progressTimer;

        private float currentProgress = 0f;
        private string currentText = string.Empty;
        private bool disposed = false;

        private const string animation = @"|/-\";
        private int animationStep = 0;

        public ProgressBar()
        {
            IOTools.AttemptToSetCursorVisibility(false);

            progressTimer = new Timer(TimerHandler, new AutoResetEvent(false), animationInterval, animationInterval);

            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        public void Report(float value)
        {
            value = Math.Clamp(value, 0, 1);
            Interlocked.Exchange(ref currentProgress, value);
        }

        private void TimerHandler(object state)
        {
            lock (progressTimer)
            {
                if (disposed) return;

                int progressStep = (int)(currentProgress * barResolution);
                int percentage = (int)Math.Floor(currentProgress * 100);

                string text = $"{animation[animationStep++ % animation.Length]} [{new string('■', progressStep)}{new string('─', barResolution - progressStep)}] {percentage}%";

                UpdateText(text);
                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            int commonPrefixLength = 0;
            int commonLength = Math.Min(currentText.Length, text.Length);

            while (commonPrefixLength < commonLength && text[commonPrefixLength] == currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            StringBuilder outputBuilder = new();
            outputBuilder.Append('\b', currentText.Length - commonPrefixLength);

            outputBuilder.Append(text[commonPrefixLength..]);

            int overlap = currentText.Length - text.Length;
            if (overlap > 0)
            {
                outputBuilder.Append(' ', overlap);
                outputBuilder.Append('\b', overlap);
            }

            Console.Write(outputBuilder);
            currentText = text;
        }

        private void ResetTimer()
        {
            progressTimer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (progressTimer)
            {
                disposed = true;
                UpdateText(string.Empty);

                IOTools.AttemptToSetCursorVisibility(true);
            }
        }
    }
}