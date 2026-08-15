using ABP.Core.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ArtemisBankingPro.Filters
{
    public class HandleDomainExceptionFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not DomainException && context.Exception is not ValidationException)
                return;

            context.ExceptionHandled = true;

            var factory = context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
            var tempData = factory.GetTempData(context.HttpContext);
            tempData["ErrorMessage"] = context.Exception.Message;
            tempData.Save();

            var referer = context.HttpContext.Request.Headers["Referer"].ToString();
            var urlHelperFactory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = urlHelperFactory.GetUrlHelper(context);
            if (!string.IsNullOrWhiteSpace(referer) && urlHelper.IsLocalUrl(referer))
            {
                context.Result = new RedirectResult(referer);
            }
            else
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
