using System.Security.Cryptography;
using System.Text;

namespace FeedReader.WebApi.Extensions
{
    public static class StringExtensions
    {
        public static string Md5(this string str)
        {
            using (var md5 = MD5.Create())
            {
                var input = Encoding.UTF8.GetBytes(str);
                var output = md5.ComputeHash(input);
                var sb = new StringBuilder();
                foreach (byte b in output)
                {
                    sb.Append(b.ToString("x2").ToLower());
                }
                return sb.ToString();
            }
        }
    }
}
