using Comentsys.Assets.FluentEmoji;

namespace Countdown.Widget;

public class ConfigureData
{
    public string Error { get; set; }

    public bool Active { get; set; }

    public string Countdown { get; set; }

    public FluentEmojiType TimerType { get; set; }

    public string TimerDate { get; set; }

    public string TimerTime { get; set; }

    public DisplayType DisplayType { get; set; }
}
