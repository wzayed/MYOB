using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MYOB.Services;
namespace MYOB.Support;
public sealed class ScreenPermissionPageFilter(IPermissionService permissions) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var screen = ScreenCatalog.FromPath(context.HttpContext.Request.Path);
        if (screen is null) { await next(); return; }
        if (context.HttpContext.User.Identity?.IsAuthenticated != true) { context.Result = new ChallengeResult(); return; }
        if (!await permissions.HasAsync(context.HttpContext.User, screen.Key)) { context.Result = new ForbidResult(); return; }
        await next();
    }
}
