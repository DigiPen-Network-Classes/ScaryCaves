using System.Text;

namespace ScaryCavesWeb.Live;

public static class SseFormatter
{
    /// <summary>
    /// One named Server-Sent Event. Every line of data needs its own "data: " prefix,
    /// or a multi-line HTML fragment would end the event at its first newline.
    /// </summary>
    public static string Format(string eventName, string data)
    {
        var sb = new StringBuilder();
        sb.Append("event: ").Append(eventName).Append('\n');
        foreach (var line in data.ReplaceLineEndings("\n").Split('\n'))
        {
            sb.Append("data: ").Append(line).Append('\n');
        }
        sb.Append('\n');
        return sb.ToString();
    }
}
