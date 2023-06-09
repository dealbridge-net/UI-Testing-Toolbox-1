using System;
using Atata;
using Lombiq.Tests.UI.Pages;
using Lombiq.Tests.UI.Services;
using OpenQA.Selenium;

namespace Lombiq.Tests.UI.Extensions
{
    public static class NavigationUITestContextExtensions
    {
        // The context is passed in to every method so they're future-proof in the case Atata won't be fully static. Also,
        // with async code it's also necessary to re-set AtataContext.Current now, see: https://github.com/atata-framework/atata/issues/364

        public static void GoToHomePage(this UITestContext context) => context.GoToRelativeUrl("/");

        public static void GoToRelativeUrl(this UITestContext context, string relativeUrl, bool onlyIfNotAlreadyThere = true)
        {
            var uri = context.GetAbsoluteUri(relativeUrl);

            if (onlyIfNotAlreadyThere && new Uri(context.Driver.Url) == uri) return;

            context.Driver.Navigate().GoToUrl(uri);
        }

        public static Uri GetAbsoluteUri(this UITestContext context, string relativeUrl) =>
            new Uri(context.Scope.BaseUri, relativeUrl);

        public static T GoToPage<T>(this UITestContext context) where T : PageObject<T>
        {
            AtataContext.Current = context.Scope.AtataContext;
            return Go.To<T>();
        }

        public static T GoToPage<T>(this UITestContext context, string relativeUrl) where T : PageObject<T>
        {
            var uri = context.GetAbsoluteUri(relativeUrl);

            AtataContext.Current = context.Scope.AtataContext;
            return Go.To<T>(url: uri.ToString());
        }

        public static OrchardCoreDashboardPage GoToDashboard(this UITestContext context) =>
            context.GoToPage<OrchardCoreDashboardPage>();

        public static OrchardCoreSetupPage GoToSetupPage(this UITestContext context) =>
            context.GoToPage<OrchardCoreSetupPage>();

        public static void GoToSmtpWebUI(this UITestContext context) =>
            context.Driver.Navigate().GoToUrl(context.SmtpServiceRunningContext.WebUIUri);

        public static ITargetLocator SwitchTo(this UITestContext context) => context.Driver.SwitchTo();

    }
}
