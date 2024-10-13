using Comentsys.Assets.Display;
using Comentsys.Assets.FluentEmoji;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

namespace Countdown.Widget;

internal class Helper
{
    private const string toast_url_path_part = "%20";
    private const string toast_url_file_part = "_";
    private const string toast_url_domain = "https://raw.githubusercontent.com";
    private const string toast_url_repo = "/microsoft/fluentui-emoji/refs/heads";
    private const string toast_url_asset_path = "/main/assets/{0}/3D/{1}_3d.png";
    private static readonly Regex regex = new("#([0-9a-fA-F]{6}|[0-9a-fA-F]{3})");
    private static readonly Regex split = new(@"\p{Lu}\p{Ll}*");

    // PadColor Method
    internal static List<Color> PadColor(
    IEnumerable<Color> values, int total)
    {
        var times = 0;
        var items = new List<Color>();
        if (values.Count() < total)
        {
            while (items.Count < total)
            {
                times++;
                items = Enumerable.Repeat(values, times)
                    .SelectMany(x => x).ToList();
            }
        }
        else if (values.Count() > total)
        {
            items = values.Take(total).ToList();
        }
        else
        {
            items = values.ToList();
        }
        return items;
    }

    // ListColor & ListDisplay Methods
    internal static Color[] ListColor(
        string content, int total, int times)
    {
        var items = new List<Color>();
        if (!string.IsNullOrWhiteSpace(content))
        {
            foreach (Match match in regex.Matches(content))
            {
                if (ColorTranslator.FromHtml(match.Value) is Color item
                    && !items.Contains(item))
                {
                    items.Add(item);
                }
            }
        }
        var values = PadColor(items, total);
        return [.. PadColor(values, total * times)];
    }

    internal static List<string> ListDisplay(
        FluentEmojiType type, DisplayType display)
    {
        var content = FlatFluentEmoji.Get(type)
            .ToSvgString();
        var items = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            items.Add(display == DisplayType.Segment ?
            Segment.Get(i, ListColor(content, 7, 1))
            .ToBase64EncodedSvgString() :
            Matrix.Get(i, ListColor(content, 5, 7))
            .ToBase64EncodedSvgString());
        }
        return items;
    }

    // GetImageData, GetImageUri & GetNow Methods
    internal static string GetImageData(FluentEmojiType type) =>
        FlatFluentEmoji.Get(type)
        .ToBase64EncodedSvgString();

    internal static Uri GetImageUri(FluentEmojiType type)
    {
        var value = Enum.GetName(type);
        var elements = split.Matches(value)
            .Select(match => match.Value);
        var path = string.Join(toast_url_path_part,
            elements.Select((item, index) => index == 0 ?
            item :
            item.ToLower()));
        var file = path.ToLower()
            .Replace(toast_url_path_part, toast_url_file_part);
        var asset = string.Format(toast_url_asset_path, path, file);
        return new Uri($"{toast_url_domain}{toast_url_repo}{asset}");
    }

    internal static DateTime GetNow()
    {
        var now = DateTime.Now;
        var date = DateOnly.FromDateTime(now);
        var time = new TimeOnly(now.Hour, now.Minute);
        return new DateTime(date, time);
    }
}
