using Lombiq.Tests.UI.Exceptions;
using Lombiq.Tests.UI.Extensions;
using Lombiq.Tests.UI.Helpers;
using OpenQA.Selenium.Remote;
using Selenium.Axe;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lombiq.Tests.UI.Services
{
    public class UITestManifest
    {
        public string Name { get; set; }
        public Action<UITestContext> Test { get; set; }
    }


    public static class UITestExecutor
    {
        private readonly static object _setupSnapshotManangerLock = new object();
        private static SynchronizingWebApplicationSnapshotManager _setupSnapshotManangerInstance;


        /// <summary>
        /// Executes a test on a new Orchard Core web app instance within a newly created Atata scope.
        /// </summary>
        public static async Task ExecuteOrchardCoreTest(UITestManifest testManifest, OrchardCoreUITestExecutorConfiguration configuration)
        {
            if (string.IsNullOrEmpty(testManifest.Name))
            {
                throw new ArgumentException("You need to specify the name of the test.");
            }

            if (configuration.OrchardCoreConfiguration == null)
            {
                throw new ArgumentNullException($"{nameof(configuration.OrchardCoreConfiguration)} should be provided.");
            }

            var startTime = DateTime.UtcNow;
            DebugHelper.WriteTimestampedLine($"Starting the execution of {testManifest.Name}.");

            configuration.OrchardCoreConfiguration.SnapshotDirectoryPath = configuration.SetupSnapshotPath;
            var runSetupOperation = configuration.SetupOperation != null;

            if (runSetupOperation)
            {
                lock (_setupSnapshotManangerLock)
                {
                    _setupSnapshotManangerInstance ??= new SynchronizingWebApplicationSnapshotManager(configuration.SetupSnapshotPath);
                }
            }

            configuration.AtataConfiguration.TestName = testManifest.Name;

            var dumpConfiguration = configuration.FailureDumpConfiguration;
            var dumpFolderNameBase = testManifest.Name;
            if (dumpConfiguration.UseShortNames && dumpFolderNameBase.Contains('('))
            {
#pragma warning disable S4635 // String offset-based methods should be preferred for finding substrings from offsets
                dumpFolderNameBase = dumpFolderNameBase.Substring(
                    dumpFolderNameBase.Substring(0, dumpFolderNameBase.IndexOf('(')).LastIndexOf('.') + 1);
#pragma warning restore S4635 // String offset-based methods should be preferred for finding substrings from offsets
            }

            var dumpRootPath = Path.Combine(dumpConfiguration.DumpsDirectoryPath, dumpFolderNameBase.MakeFileSystemFriendly());
            DirectoryHelper.SafelyDeleteDirectoryIfExists(dumpRootPath);

            if (configuration.AccessibilityCheckingConfiguration.CreateReportAlways)
            {
                var directoryPath = configuration.AccessibilityCheckingConfiguration.AlwaysCreatedAccessibilityReportsDirectoryPath;
                if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
            }

            var testOutputHelper = configuration.TestOutputHelper;
            var retryCount = 0;
            while (true)
            {
                BrowserLogMessage[] browserLogMessages = null;
                async Task<BrowserLogMessage[]> GetBrowserLog(RemoteWebDriver driver) =>
                    browserLogMessages ??= (await driver.GetAndEmptyBrowserLog()).ToArray();

                SmtpService smtpService = null;
                IWebApplicationInstance applicationInstance = null;
                UITestContext context = null;

                try
                {
                    async Task<UITestContext> CreateContext()
                    {
                        SmtpServiceRunningContext smtpContext = null;

                        if (configuration.UseSmtpService)
                        {
                            smtpService = new SmtpService();
                            smtpContext = await smtpService.Start();
                            configuration.OrchardCoreConfiguration.BeforeAppStart += (contentRoot, argumentsBuilder) =>
                                argumentsBuilder.Add("--SmtpPort").Add(smtpContext.Port);
                        }

                        applicationInstance = new OrchardCoreInstance(configuration.OrchardCoreConfiguration, testOutputHelper);
                        var uri = await applicationInstance.StartUp();

                        var atataScope = AtataFactory.StartAtataScope(
                            testOutputHelper,
                            uri,
                            configuration);

                        return new UITestContext(testManifest.Name, configuration, applicationInstance, atataScope, smtpContext);
                    }

                    if (runSetupOperation)
                    {
                        var resultUri = await _setupSnapshotManangerInstance.RunOperationAndSnapshotIfNew(async () =>
                        {
                            // Note that the context creation needs to be done here too because the Orchard app needs
                            // the snapshot config to be available at startup too.
                            context = await CreateContext();

                            return (context, configuration.SetupOperation(context));
                        });

                        if (context == null) context = await CreateContext();

                        context.GoToRelativeUrl(resultUri.PathAndQuery);
                    }

                    if (context == null) context = await CreateContext();

                    testManifest.Test(context);

                    try
                    {
                        if (configuration.AssertAppLogs != null) await configuration.AssertAppLogs(context.Application);
                    }
                    catch (Exception)
                    {
                        testOutputHelper.WriteLine("Application logs: " + Environment.NewLine);
                        testOutputHelper.WriteLine(await context.Application.GetLogOutput());

                        throw;
                    }

                    try
                    {
                        configuration.AssertBrowserLog?.Invoke(await GetBrowserLog(context.Scope.Driver));
                    }
                    catch (Exception)
                    {
                        testOutputHelper.WriteLine("Browser logs: " + Environment.NewLine);
                        testOutputHelper.WriteLine((await GetBrowserLog(context.Scope.Driver)).ToFormattedString());

                        throw;
                    }

                    return;
                }
                catch (Exception ex)
                {
                    testOutputHelper.WriteLine($"The test failed with the following exception: {ex}.");

                    if (context != null)
                    {
                        var dumpContainerPath = Path.Combine(dumpRootPath, "Attempt " + retryCount.ToString());
                        var debugInformationPath = Path.Combine(dumpContainerPath, "DebugInformation");

                        Directory.CreateDirectory(dumpContainerPath);
                        Directory.CreateDirectory(debugInformationPath);

                        if (dumpConfiguration.CaptureAppSnapshot)
                        {
                            await context.Application.TakeSnapshot(Path.Combine(dumpContainerPath, "AppDump"));
                        }

                        if (dumpConfiguration.CaptureScreenshot)
                        {
                            // Only PNG is supported on .NET Core.
                            context.Scope.Driver.GetScreenshot().SaveAsFile(Path.Combine(debugInformationPath, "Screenshot.png"));
                        }

                        if (dumpConfiguration.CaptureHtmlSource)
                        {
                            await File.WriteAllTextAsync(Path.Combine(debugInformationPath, "PageSource.html"), context.Scope.Driver.PageSource);
                        }

                        if (dumpConfiguration.CaptureBrowserLog)
                        {
                            await File.WriteAllLinesAsync(
                                Path.Combine(debugInformationPath, "BrowserLog.log"),
                                (await GetBrowserLog(context.Scope.Driver)).Select(message => message.ToString()));
                        }

                        if (ex is AccessibilityAssertionException accessibilityAssertionException
                            && configuration.AccessibilityCheckingConfiguration.CreateReportOnFailure)
                        {
                            context.Driver.CreateAxeHtmlReport(
                                accessibilityAssertionException.AxeResult,
                                Path.Combine(debugInformationPath, "AccessibilityReport.html"));
                        }
                    }

                    if (retryCount == configuration.MaxRetryCount)
                    {
                        var dumpFolderAbsolutePath = Path.Combine(AppContext.BaseDirectory, dumpRootPath);
                        testOutputHelper.WriteLine($"The test was attempted {retryCount + 1} time(s) and won't be retried anymore. You can see more details on why it's failing in the FailureDumps folder: {dumpFolderAbsolutePath}");
                        throw;
                    }

                    testOutputHelper.WriteLine(
                        $"The test was attempted {retryCount + 1} time(s). {configuration.MaxRetryCount - retryCount} more attempt(s) will be made.");
                }
                finally
                {
                    if (context != null) context.Scope.Dispose();
                    if (applicationInstance != null) await applicationInstance.DisposeAsync();
                    if (smtpService != null) await smtpService.DisposeAsync();

                    DebugHelper.WriteTimestampedLine($"Finishing the execution of {testManifest.Name}, total time: {DateTime.UtcNow - startTime}.");
                }

                retryCount++;
            }
        }
    }
}