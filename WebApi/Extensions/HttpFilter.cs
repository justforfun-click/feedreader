using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace FeedReader.WebApi.Extensions
{
    class HttpFilter
    {
        
        public static async Task<IActionResult> RunAsync(HttpRequest req, Func<Task<IActionResult>> func)
        {
            object res;
            if (req.HttpContext.Items.TryGetValue("ExternalErrorExcepiton", out res))
            {
                return ((ExternalErrorExcepiton)res).ToHttpActionResult();
            }
            
            try
            {
                return await func();    
            }
            catch (ExternalErrorExcepiton ex)
            {
                return ex.ToHttpActionResult();
            }
            catch (Exception ex)
            {
                return new BadRequestErrorMessageResult(ex.Message);
            }
        }

        public static async Task<object> RunAsync(HttpRequest req, Func<Task<object>> func)
        {
            object res;
            if (req.HttpContext.Items.TryGetValue("ExternalErrorExcepiton", out res))
            {
                return null;
            }
            
            try
            {
                
                return await func();
            }
            catch (ExternalErrorExcepiton ex)
            {
                req.HttpContext.Items.Add("ActionResult", ex.ToHttpActionResult());
                return null;
            }
            catch (Exception ex)
            {
                req.HttpContext.Items.Add("ActionResult", new BadRequestErrorMessageResult(ex.Message));
                return null;
            }
        }
    }
}
