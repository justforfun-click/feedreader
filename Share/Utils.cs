using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FeedReader.Share
{
    public static class Utils
    {
        public static string Md5(string input)
        {
            using (var md5 = MD5.Create())
            {
                return String.Join("", from b in md5.ComputeHash(Encoding.UTF8.GetBytes(input)) select b.ToString("x2"));
            }
        }

        public static string ToStringEmptyIfZero(this int i)
        {
            return i == 0 ? "" : i.ToString();
        }
    }
}
