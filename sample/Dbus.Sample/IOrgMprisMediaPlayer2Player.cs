using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Dbus.Sample;

[DbusConsume("org.mpris.MediaPlayer2.Player")]
public interface IOrgMprisMediaPlayer2Player : IDisposable, IAsyncDisposable, INotifyPropertyChanged, IDbusPropertyInitialization
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
    string PlaybackStatus { get; }
    Task<string> GetLoopStatusAsync();
    Task SetLoopStatusAsync(string status);
    string LoopStatus { get; }
    Task<double> GetRateAsync();
    Task SetRateAsync(double rate);
    double Rate { get; }
    Task<bool> GetShuffleAsync();
    Task SetShuffleAsync(bool shuffle);
    bool Shuffle { get; }
    Task<IDictionary<string, object>> GetMetadataAsync();
    IDictionary<string, object> Metadata { get; }
    Task<double> GetVolumeAsync();
    Task SetVolumeAsync(double volume);
    double Volume { get; }
    Task<long> GetPositionAsync(); // Does not emit PropertiesChanged
    Task<double> GetMinimumRateAsync();
    double MinimumRate { get; }
    Task<double> GetMaximumRateAsync();
    double MaximumRate { get; }
    Task<bool> GetCanGoNextAsync();
    bool CanGoNext { get; }
    Task<bool> GetCanGoPreviousAsync();
    bool CanGoPrevious { get; }
    Task<bool> GetCanPlayAsync();
    bool CanPlay { get; }
    Task<bool> GetCanPauseAsync();
    bool CanPause { get; }
    Task<bool> GetCanSeekAsync();
    bool CanSeek { get; }
    Task<bool> GetCanControlAsync(); // Does not emit PropertiesChanged
}
