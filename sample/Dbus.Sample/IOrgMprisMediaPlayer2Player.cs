using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus.Sample
{
    [DbusConsume("org.mpris.MediaPlayer2.Player")]
    public interface IOrgMprisMediaPlayer2Player : IDisposable
    {
        Task NextAsync();
        Task PreviousAsync();
        Task PauseAsync();
        Task PlayPauseAsync();
        Task StopAsync();
        Task PlayAsync();
        Task SeekAsync(long offsetInUsec);
        Task SetPositionAsync(ObjectPath track, long startPosition);
        Task OperUriAsync(string uri);

        event Action<long> Seeked;

        Task<string> GetPlaybackStatusAsync();
        Task<string> GetLoopStatusAsync();
        Task SetLoopStatusAsync(string status);
        Task<double> GetRateAsync();
        Task SetRateAsync(double rate);
        Task<bool> GetShuffleAsync();
        Task SetSuffleAsync(bool shuffle);
        Task<IDictionary<string, object>> GetMetadataAsync();
        Task<double> GetVolumeAsync();
        Task SetVolumeAsync(double volume);
        Task<long> GetPositionAsync();
        Task<double> GetMinimumRateAsync();
        Task<double> GetMaximumRateAsync();
        Task<bool> GetCanGoNextAsync();
        Task<bool> GetCanGoPreviousAsync();
        Task<bool> GetCanPlayAsync();
        Task<bool> GetCanPauseAsync();
        Task<bool> GetCanSeekAsync();
        Task<bool> GetCanControlAsync();
    }
}
