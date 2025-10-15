using CustomerScoreTest.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CustomerScoreTest.Tools
{
    public class ExceptionFilter: IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            ResponseBase responseBase = new ResponseBase()
            {
                ResponseHeader = new ResponseHeader()
                {
                    Message = context.Exception.Message,
                    SubStatusCode = "0",
                    StatusCode = "503",
                }
            };

            context.Result = new ObjectResult(responseBase)
            {
                StatusCode = 503,
            };
            context.ExceptionHandled = true;
        }

    }
}
