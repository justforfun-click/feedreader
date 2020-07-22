using System.Linq;

namespace FeedReader.Share.Extensions
{
    public static class ObjectExtension
    {
        public static void CopyPropertiesTo<S, D>(this S source, D dest)
        {
            var sourceProperties = typeof(S).GetProperties().Where(x => x.CanRead);
            var destProperties = typeof(D).GetProperties().Where(x => x.CanWrite);
            foreach (var prop in sourceProperties)
            {
                var p = destProperties.FirstOrDefault(x => x.Name == prop.Name && x.PropertyType == prop.PropertyType);
                if (p != null)
                {
                    p.SetValue(dest, prop.GetValue(source));
                }
            }
        }
    }
}
