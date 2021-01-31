using System;
using System.IO;
using System.Text;

namespace FeedReader.WebApi.Test
{
    class TestUtils
    {
        public static string LoadTestData(string testFilename)
        {
            return File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", testFilename), Encoding.UTF8);
        }
    }
}
