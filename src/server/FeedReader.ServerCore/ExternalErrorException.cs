using Microsoft.AspNetCore.Mvc;
using System;
using System.Web.Http;

namespace FeedReader.WebApi
{
    public class ExternalErrorExcepiton : Exception
    {
        public ExternalErrorExcepiton(string message)
            : base(message)
        {
        }

        public virtual IActionResult ToHttpActionResult()
        {
            return new BadRequestErrorMessageResult(Message);
        }
    }

    public class ExternalErrorExceptionUnauthentication : ExternalErrorExcepiton
    {
        public ExternalErrorExceptionUnauthentication()
            : base(null)
        {
        }

        public override IActionResult ToHttpActionResult()
        {
            return new UnauthorizedResult();
        }
    }

    public class ExternalErrorExceptionNotFound : ExternalErrorExcepiton
    {
        public ExternalErrorExceptionNotFound()
            : base(null)
        {
        }

        public override IActionResult ToHttpActionResult()
        {
            return new NotFoundResult();
        }
    }
}
