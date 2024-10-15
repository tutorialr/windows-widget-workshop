using Comentsys.Assets.FluentEmoji;
using Comentsys.Toolkit.WindowsAppSdk;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Microsoft.Windows.Widgets.Providers;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Countdown.Widget;

internal class CountdownWidget : WidgetBase
{
    private const string zero = "00";
    private const string format = "D2";
    private const string save = "save";
    private const string reset = "reset";
    private const string close = "close";
    private const string time = "HH:mm";
    private const string date = "yyyy-MM-dd";
    private const string template = "ms-appx:///Assets/Template.json";
    private const string configure = "ms-appx:///Assets/Configure.json";
    private const string error = "Must be in future but before 100 days";
    private const string finished = "{0} at {1:HH:mm} on {2:d MMMM yyyy}";
    private static readonly JsonSerializerOptions options = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly TemplateData _template = new();

    // Private Members
    private ConfigureData _configure = new();
    private List<string> _display = [];
    private Timer _timer = null;
    private int _total = 0;

    // When, Update & Calculate Methods
    private DateTime When() =>
    DateTime.Parse($"{_configure.TimerDate} {_configure.TimerTime}");

    private void Update()
    {
        SetState(JsonSerializer.Serialize(_configure, options));
        var update = new WidgetUpdateRequestOptions(Id)
        {
            Template = GetTemplateForWidget(),
            Data = GetDataForWidget(),
            CustomState = State
        };
        WidgetManager.GetDefault().UpdateWidget(update);
    }

    private bool Calculate(
        out string days,
        out string hours,
        out string minutes,
        out int total)
    {
        var now = Helper.GetNow();
        var when = When();
        var diff = when - now;
        days = diff.Days.ToString(format);
        hours = diff.Hours.ToString(format);
        minutes = diff.Minutes.ToString(format);
        total = (int)diff.TotalMinutes;
        return when > now && diff.Days < 100;
    }

    // Display & Clear Methods
    private void Display(
    string days,
    string hours,
    string minutes)
    {
        _template.DaysTens = _display[int.Parse(days[0].ToString())];
        _template.DaysUnits = _display[int.Parse(days[1].ToString())];
        _template.HoursTens = _display[int.Parse(hours[0].ToString())];
        _template.HoursUnits = _display[int.Parse(hours[1].ToString())];
        _template.MinutesTens = _display[int.Parse(minutes[0].ToString())];
        _template.MinutesUnits = _display[int.Parse(minutes[1].ToString())];
    }

    private void Clear()
    {
        _total = 0;
        _timer = null;
        _configure.Active = false;
        Display(zero, zero, zero);
        Update();
    }

    // Reset & Toast Methods
    private void Reset()
    {
        _timer = null;
        var now = Helper.GetNow();
        _configure.TimerDate = now.ToString(date);
        _configure.TimerTime = now.ToString(time);
        _configure.DisplayType = DisplayType.Segment;
        _configure.TimerType = FluentEmojiType.TimerClock;
        _configure.Countdown = nameof(_configure.Countdown);
        _display = Helper.ListDisplay(_configure.TimerType, _configure.DisplayType);
        _template.ImageData = Helper.GetImageData(_configure.TimerType);
        _template.Countdown = _configure.Countdown;
        Clear();
    }

    private void Toast()
    {
        Clear();
        var when = When();
        var text = _template.Countdown;
        var image = Helper.GetImageUri(_configure.TimerType);
        var toast = new AppNotificationBuilder()
        .AddText(text)
        .AddText(string.Format(finished, text, when, when))
        .SetInlineImage(image)
        .BuildNotification();
        AppNotificationManager.Default.Show(toast);
    }

    // Tick & Start Methods
    private void Tick(object state)
    {
        if (!Configure && _configure.Active)
        {
            var valid = Calculate(
                out string days, out string hours,
                out string minutes, out int total);
            if (IsActivated && valid && total != _total)
            {
                Display(days, hours, minutes);
                Update();
            }
            if (total <= 0)
            {
                Toast();
            }
        }
    }

    private void Start()
    {
        _display = Helper.ListDisplay(_configure.TimerType, _configure.DisplayType);
        _template.ImageData = Helper.GetImageData(_configure.TimerType);
        _template.Countdown = _configure.Countdown;
        _timer ??= new Timer(Tick, null, 0, 100);
        _configure.Active = true;
    }

    // Constructor and DefinitionId & Configure Properties
    public CountdownWidget(string widgetId, string startingState) :
    base(widgetId, startingState)
    {
        try
        {
            _configure = string.IsNullOrWhiteSpace(startingState) ?
                new ConfigureData() :
                JsonSerializer.Deserialize<ConfigureData>(
                    startingState, options);
        }
        catch
        {
            _configure = new();
        }
        if (_configure.Active)
        {
            Start();
        }
        else
        {
            Reset();
        }
    }

    public static string DefinitionId { get; } = nameof(CountdownWidget);

    protected bool Configure { get; set; } = false;

    // OnActionInvoked & OnCustomizationRequested Methods
    public override void OnActionInvoked(
    WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (actionInvokedArgs.Verb == save)
        {
            try
            {
                _configure = JsonSerializer.Deserialize<ConfigureData>(
                    actionInvokedArgs.Data, options);
                if (Calculate(out string days, out string hours,
                    out string minutes, out int total))
                {
                    Start();
                    _total = total;
                    Configure = false;
                    Display(days, hours, minutes);
                    _configure.Error = string.Empty;
                }
                else
                {
                    _configure.Error = error;
                    Configure = true;
                }
            }
            catch { }
        }
        else if (actionInvokedArgs.Verb == reset)
        {
            _configure.Error = string.Empty;
            Configure = false;
            Reset();
        }
        else
        {
            Configure = false;
        }
        Update();
    }

    public override void OnCustomizationRequested(
        WidgetCustomizationRequestedArgs customizationRequestedArgs)
    {
        Configure = true;
        Update();
    }

    // Activate, Deactivate, GetDataForWidget & GetTemplateForWidget Methods
    public override void Activate() =>
    isActivated = true;

    public override void Deactivate() =>
        isActivated = false;

    public override string GetDataForWidget() => Configure ?
        JsonSerializer.Serialize(_configure, options) :
        JsonSerializer.Serialize(_template);

    public override string GetTemplateForWidget() => Configure ?
        WidgetHelper.ReadJsonFromPackage(configure) :
        WidgetHelper.ReadJsonFromPackage(template);
}
