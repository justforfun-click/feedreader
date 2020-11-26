using FeedReader.WebApi.Processors;
using Xunit;

namespace FeedReader.WebApi.Test
{
    public class HtmlParserTest
    {
        [Fact]
        public void ParseShortcutIcon()
        {
            var htmlContent = TestUtils.LoadTestData("NewsYcombinator.2020.08.10.html");
            Assert.Equal("https://news.ycombinator.com/favicon.ico", new HtmlParser(htmlContent, "https://news.ycombinator.com/").ShortcurtIcon);

            htmlContent = TestUtils.LoadTestData("CoolShell.2020.08.10.html");
            Assert.Equal("https://coolshell.cn/wp-content/uploads/2020/04/mini.logo_.png", new HtmlParser(htmlContent, "https://coolshell.cn").ShortcurtIcon);

            htmlContent = TestUtils.LoadTestData("CssTricks.2020.08.10.html");
            Assert.Equal("https://css-tricks.com/apple-touch-icon.png", new HtmlParser(htmlContent, "https://css-tricks.com").ShortcurtIcon);
        }
    }
}