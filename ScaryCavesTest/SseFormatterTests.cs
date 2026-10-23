using ScaryCavesWeb.Live;

namespace ScaryCavesTest;

public class SseFormatterTests
{
    [Fact]
    public void Format_SingleLine() =>
        Assert.Equal("event: message\ndata: <li>x</li>\n\n", SseFormatter.Format("message", "<li>x</li>"));

    [Fact]
    public void Format_MultilineHtml_PrefixesEveryLine() =>
        Assert.Equal("event: room\ndata: <div>\ndata: <p>a</p>\ndata: </div>\n\n",
            SseFormatter.Format("room", "<div>\n<p>a</p>\r\n</div>"));
}
