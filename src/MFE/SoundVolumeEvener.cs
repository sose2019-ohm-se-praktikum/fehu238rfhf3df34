using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MFE
{
    public static partial class SoundVolumeEvener
    {
        public static async Task RunSoundVolumeEvening(IEnumerable<string> files, IProgress<float> progress, CancellationToken cancellationToken = default)
        {
            await RunSoundVolumeEvening(files, new SoundVolumeEvenerSettings(), progress, cancellationToken);
        }

        public static async Task RunSoundVolumeEvening(IEnumerable<string> files, SoundVolumeEvenerSettings settings, IProgress<float> progress, CancellationToken cancellationToken = default)
        {
            //throw new NotImplementedException();
            var i = 1;
            while (!cancellationToken.IsCancellationRequested || i <= 100)
            {
                progress.Report(((float) i) / 100);
                await Task.Delay(25);
                i++;
            }
        }
    }
}
