namespace Sitecore.Support.Mvc
{
    using Sitecore.Common;
    using Sitecore.Configuration;
    using Sitecore.Sites;
    using Sitecore.Web.Pipelines.InitializeSpeakLayout;
    using System.Web.Mvc;

    internal class ShellSiteAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            Switcher<SiteContext, SiteContextSwitcher>.Exit();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            InitializeSpeakLayoutArgs args = new InitializeSpeakLayoutArgs();
            new SetDisplayMode().Process(args);
            Switcher<SiteContext, SiteContextSwitcher>.Enter(Factory.GetSite("shell"));
            new DisableAnalytics().Process(args);
        }
    }
}