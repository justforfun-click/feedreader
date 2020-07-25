using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace FeedReader.WebApi.Extensions
{
    class HttpFilter
    {
        
        public static async Task<IActionResult> RunAsync(HttpRequest req, Func<Task<IActionResult>> func)
        {
            object res;
            if (req.HttpContext.Items.TryGetValue("ActionResult", out res))
            {
                return (IActionResult)res;
            }
            return await func();
        }

        public static async Task<object> RunAsync(HttpRequest req, Func<Task<object>> func)
        {
            object res;
            if (req.HttpContext.Items.TryGetValue("ActionResult", out res))
            {
                return null;
            }

            var obj = await func();
            if (obj is IActionResult)
            {
                req.HttpContext.Items.Add("ActionResult", obj);
                return null;
            }
            else
            {
                return obj;
            }
        }
    }
}
