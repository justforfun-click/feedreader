using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FeedReader.Share
{
    public class Utils
    {
        public static string Md5(string input)
        {
            using (var md5 = MD5.Create())
            {
                return String.Join("", from b in md5.ComputeHash(Encoding.UTF8.GetBytes(input)) select b.ToString("x2"));
            }
        }

        public static string Sha256(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                return string.Join("", from b in sha256.ComputeHash(Encoding.UTF8.GetBytes(input)) select b.ToString("x2"));
            }
        }

        public static string Base64Encode(string str)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        public static string Base64Decode(string str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }
    }
}
