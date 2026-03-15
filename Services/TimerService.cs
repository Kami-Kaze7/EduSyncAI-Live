using System;
using System.Timers;

namespace EduSyncAI
{
    public class TimerService
    {
    private System.Timers.Timer _timer;
    private int _secondsRemaining;
    private const int MaxSeconds = 300; // 5 minutes

    public event EventHandler<int> TimeChanged;
    public event EventHandler<string> WarningTriggered;
    public event EventHandler TimerExpired;

    public int SecondsRemaining => _secondsRemaining;
    public bool IsRunning => _timer?.Enabled ?? false;

    public void Start()
    {
        _secondsRemaining = MaxSeconds;
        _timer = new System.Timers.Timer(1000); // 1 second interval
        _timer.Elapsed += OnTimerElapsed;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
    }

    public void Reset()
    {
        Stop();
        _secondsRemaining = MaxSeconds;
        TimeChanged?.Invoke(this, _secondsRemaining);
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        _secondsRemaining--;
        TimeChanged?.Invoke(this, _secondsRemaining);

        // Trigger warnings at specific intervals
        if (_secondsRemaining == 60)
        {
            WarningTriggered?.Invoke(this, "1 minute remaining!");
        }
        else if (_secondsRemaining == 30)
        {
            WarningTriggered?.Invoke(this, "30 seconds remaining!");
        }

        // Timer expired
        if (_secondsRemaining <= 0)
        {
            Stop();
            TimerExpired?.Invoke(this, EventArgs.Empty);
        }
    }

    public string GetFormattedTime()
    {
        var minutes = _secondsRemaining / 60;
        var seconds = _secondsRemaining % 60;
        return $"{minutes:D2}:{seconds:D2}";
    }
    }
}
