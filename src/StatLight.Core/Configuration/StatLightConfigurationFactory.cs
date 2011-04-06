﻿
namespace StatLight.Core.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using StatLight.Core.Common;
    using StatLight.Core.WebBrowser;
    using StatLight.Core.WebServer.XapHost;
    using StatLight.Core.WebServer.XapInspection;

    public class StatLightConfigurationFactory : IStatLightConfigurationFactory
    {
        public const int DefaultDialogSmackDownElapseMilliseconds = 5000;
        private readonly ILogger _logger;
        private readonly XapHostFileLoaderFactory _xapHostFileLoaderFactory;

        public StatLightConfigurationFactory(ILogger logger)
            : this(logger, new XapHostFileLoaderFactory(logger))
        {
        }

        public StatLightConfigurationFactory(ILogger logger, XapHostFileLoaderFactory xapHostFileLoaderFactory)
        {
            _logger = logger;
            _xapHostFileLoaderFactory = xapHostFileLoaderFactory;
        }



        public StatLightConfiguration GetStatLightConfigurationForXap(UnitTestProviderType unitTestProviderType, string xapPath, MicrosoftTestingFrameworkVersion? microsoftTestingFrameworkVersion, Collection<string> methodsToTest, string tagFilters, int numberOfBrowserHosts, bool isRemoteRun, string queryString, WebBrowserType webBrowserType, bool forceBrowserStart, bool showTestingBrowserHost)
        {
            if (queryString == null)
                throw new ArgumentNullException("queryString");

            IEnumerable<ITestFile> filesToCopyIntoHostXap = new List<ITestFile>();
            IEnumerable<string> testAssemblyFormalNames = new List<string>();
            string entryPointAssembly = string.Empty;
            if (isRemoteRun)
            {
            }
            else
            {
                AssertFileExists(xapPath);

                var xapReader = new XapReader(_logger);

                TestFileCollection testFileCollection = xapReader.LoadXapUnderTest(xapPath);

                SetupUnitTestProviderType(testFileCollection, ref unitTestProviderType, ref microsoftTestingFrameworkVersion);

                entryPointAssembly = testFileCollection.TestAssemblyFullName;

                filesToCopyIntoHostXap = testFileCollection.FilesContainedWithinXap;
                testAssemblyFormalNames = testFileCollection.GetAssemblyNames();
            }

            ConfigureForPhone(unitTestProviderType, ref webBrowserType);

            var clientConfig = new ClientTestRunConfiguration(unitTestProviderType, methodsToTest, tagFilters, numberOfBrowserHosts, webBrowserType, showTestingBrowserHost, entryPointAssembly, testAssemblyFormalNames);

            var serverConfig = CreateServerConfiguration(
                xapPath,
                unitTestProviderType,
                microsoftTestingFrameworkVersion,
                filesToCopyIntoHostXap,
                DefaultDialogSmackDownElapseMilliseconds,
                queryString,
                forceBrowserStart,
                showTestingBrowserHost);

            return new StatLightConfiguration(clientConfig, serverConfig);
        }

        private static void ConfigureForPhone(UnitTestProviderType unitTestProviderType, ref WebBrowserType webBrowserType)
        {
            if (unitTestProviderType == UnitTestProviderType.MSTestPhone)
            {
                webBrowserType = WebBrowserType.Phone;
                return;
            }
        }

        public StatLightConfiguration GetStatLightConfigurationForDll(UnitTestProviderType unitTestProviderType, string dllPath, MicrosoftTestingFrameworkVersion? microsoftTestingFrameworkVersion, Collection<string> methodsToTest, string tagFilters, int numberOfBrowserHosts, bool isRemoteRun, string queryString, WebBrowserType webBrowserType, bool forceBrowserStart, bool showTestingBrowserHost)
        {
            if (queryString == null)
                throw new ArgumentNullException("queryString");

            IEnumerable<ITestFile> filesToCopyIntoHostXap = new List<ITestFile>();
            string entryPointAssembly = string.Empty;
            IEnumerable<string> testAssemblyFormalNames = new List<string>();
            if (isRemoteRun)
            {
            }
            else
            {
                AssertFileExists(dllPath);

                var dllFileInfo = new FileInfo(dllPath);

                var assemblyResolver = new AssemblyResolver(_logger, dllFileInfo.Directory);
                var dependentAssemblies = assemblyResolver.ResolveAllDependentAssemblies(dllFileInfo.FullName);

                var coreFileUnderTest = new TestFile(dllFileInfo.FullName);
                var dependentFilesUnderTest = dependentAssemblies.Select(file => new TestFile(file)).ToList();
                dependentFilesUnderTest.Add(coreFileUnderTest);

                var testFileCollection = new TestFileCollection(_logger,
                                                          AssemblyName.GetAssemblyName(dllFileInfo.FullName).ToString(),
                                                          dependentFilesUnderTest);

                SetupUnitTestProviderType(testFileCollection, ref unitTestProviderType, ref microsoftTestingFrameworkVersion);

                entryPointAssembly = testFileCollection.TestAssemblyFullName;

                filesToCopyIntoHostXap = testFileCollection.FilesContainedWithinXap;
                testAssemblyFormalNames = testFileCollection.GetAssemblyNames();
            }

            ConfigureForPhone(unitTestProviderType, ref webBrowserType);

            var clientConfig = new ClientTestRunConfiguration(unitTestProviderType, methodsToTest, tagFilters, numberOfBrowserHosts, webBrowserType, showTestingBrowserHost, entryPointAssembly, testAssemblyFormalNames);

            var serverConfig = CreateServerConfiguration(
                dllPath,
                clientConfig.UnitTestProviderType,
                microsoftTestingFrameworkVersion,
                filesToCopyIntoHostXap,
                DefaultDialogSmackDownElapseMilliseconds,
                queryString,
                forceBrowserStart,
                showTestingBrowserHost);

            return new StatLightConfiguration(clientConfig, serverConfig);
        }




        private static void SetupUnitTestProviderType(TestFileCollection testFileCollection, ref UnitTestProviderType unitTestProviderType, ref MicrosoftTestingFrameworkVersion? microsoftTestingFrameworkVersion)
        {
            if (unitTestProviderType == UnitTestProviderType.Undefined || microsoftTestingFrameworkVersion == null)
            {
                //TODO: Print message telling the user what the type is - and if they give it
                // we don't have to "reflect" on the xap to determine the test provider type.

                if (unitTestProviderType == UnitTestProviderType.Undefined)
                {
                    unitTestProviderType = testFileCollection.UnitTestProvider;
                }

                if (
                    (testFileCollection.UnitTestProvider == UnitTestProviderType.MSTest ||
                     unitTestProviderType == UnitTestProviderType.MSTest ||
                     unitTestProviderType == UnitTestProviderType.MSTestWithCustomProvider)
                    && microsoftTestingFrameworkVersion == null)
                {
                    microsoftTestingFrameworkVersion = testFileCollection.MSTestVersion;
                }
            }
        }

        private static void AssertFileExists(string xapPath)
        {
            if (!File.Exists(xapPath))
            {
                throw new FileNotFoundException("File could not be found. [{0}]".FormatWith(xapPath));
            }
        }

        private ServerTestRunConfiguration CreateServerConfiguration(
            string xapPath,
            UnitTestProviderType unitTestProviderType,
            MicrosoftTestingFrameworkVersion? microsoftTestingFrameworkVersion,
            IEnumerable<ITestFile> filesToCopyIntoHostXap,
            long dialogSmackDownElapseMilliseconds,
            string queryString,
            bool forceBrowserStart,
            bool showTestingBrowserHost)
        {
            XapHostType xapHostType = _xapHostFileLoaderFactory.MapToXapHostType(unitTestProviderType, microsoftTestingFrameworkVersion);

            Func<byte[]> hostXapFactory = () =>
            {
                byte[] hostXap = _xapHostFileLoaderFactory.LoadXapHostFor(xapHostType);
                hostXap = RewriteXapWithSpecialFiles(hostXap, filesToCopyIntoHostXap);
                return hostXap;
            };

            return new ServerTestRunConfiguration(hostXapFactory, dialogSmackDownElapseMilliseconds, xapPath, xapHostType, queryString, forceBrowserStart, showTestingBrowserHost);
        }

        private byte[] RewriteXapWithSpecialFiles(byte[] xapHost, IEnumerable<ITestFile> filesToCopyIntoHostXap)
        {
            if (filesToCopyIntoHostXap.Any())
            {
                var rewriter = new XapRewriter(_logger);

                xapHost = rewriter.RewriteZipHostWithFiles(xapHost, filesToCopyIntoHostXap)
                                .ToByteArray();
            }

            return xapHost;
        }


    }


    public interface IStatLightConfigurationFactory
    {
        StatLightConfiguration GetStatLightConfigurationForXap(UnitTestProviderType unitTestProviderType, string xapPath, MicrosoftTestingFrameworkVersion? microsoftTestingFrameworkVersion, Collection<string> methodsToTest, string tagFilters, int numberOfBrowserHosts, bool isRemoteRun, string queryString, WebBrowserType webBrowserType, bool forceBrowserStart, bool showTestingBrowserHost);

        StatLightConfiguration GetStatLightConfigurationForDll(UnitTestProviderType unitTestProviderType, string dllPath, MicrosoftTestingFrameworkVersion? microsoftTestingFrameworkVersion, Collection<string> methodsToTest, string tagFilters, int numberOfBrowserHosts, bool isRemoteRun, string queryString, WebBrowserType webBrowserType, bool forceBrowserStart, bool showTestingBrowserHost);
    }

}
