using Atata;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;

namespace Lombiq.Tests.UI.Extensions
{
    public static class FormWebDriverExtensions
    {
        public static IWebElement TryFillElement(this IWebDriver driver, IWebElement element, string value)
        {
            element.ClearWithLogging();

            if (value.Contains('@', StringComparison.Ordinal))
            {
                // This should prevent OpenQA.Selenium.WebDriverException: move target out of bounds error.
                var actions = new Actions(driver);
                actions.MoveToElement(element);
                actions.Perform();

                // On some platforms, probably due to keyboard settings, the @ character can be missing from the address
                // when entered into a text field so we need to use Actions. The following solution doesn't work:
                // https://stackoverflow.com/a/52202594/220230. This needs to be done in addition to the standard
                // FillInWith() as without that some forms start to behave strange and not save values.
                driver.Perform(actions => actions.SendKeys(element, value));
            }
            else
            {
                element.SendKeysWithLogging(value);
            }

            return element;
        }
    }
}
