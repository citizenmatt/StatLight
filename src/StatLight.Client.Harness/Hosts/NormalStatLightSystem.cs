﻿using System;
using System.Net;
using System.Windows;
using StatLight.Client.Harness.Messaging;
using StatLight.Core.Configuration;
using StatLight.Core.WebServer;

namespace StatLight.Client.Harness.Hosts
{
    public class NormalStatLightSystem : StatLightSystemBase
    {
        private readonly Uri _postbackUriBase;

        internal NormalStatLightSystem(Action<UIElement> onReady)
        {
            
#if WINDOWS_PHONE
            
            var urlx = "http://localhost:8887/";
#else
            var src = Application.Current.Host.Source;
            var urlx = src.Scheme + "://" + src.Host + ":" + src.Port + "/";
#endif
            _postbackUriBase = new Uri(urlx);
            SetPostbackUri(_postbackUriBase);

            OnReadySetupRootVisual(onReady);
        }

        private void OnReadySetupRootVisual(Action<UIElement> onReady)
        {
            if (onReady == null)
                throw new ArgumentNullException("onReady");

            OnReady = onReady;

            TestRunnerHost = LocateService<ITestRunnerHost>();
            GoGetTheTestRunConfiguration();
        }

        protected override void OnTestRunConfigurationDownloaded(ClientTestRunConfiguration clientTestRunConfiguration)
        {
            var loadedXapData = new ThisXapData(clientTestRunConfiguration.EntryPointAssembly, clientTestRunConfiguration.TestAssemblyFormalNames);
            Server.Debug("OnTestRunConfigurationDownloaded");
            TestRunnerHost.ConfigureWithLoadedXapData(loadedXapData);

            CompletedTestXapRequest = true;
            DisplayTestHarness();
        }
    }
}