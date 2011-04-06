﻿using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Windows;
using StatLight.Client.Harness.Messaging;
using StatLight.Core.Common;
using StatLight.Core.Configuration;
using StatLight.Core.Serialization;
using StatLight.Core.WebServer;

namespace StatLight.Client.Harness.Hosts
{
    public abstract class StatLightSystemBase
    {
        protected bool TestRunConfigurationDownloadComplete;
        protected ITestRunnerHost TestRunnerHost;
        protected bool CompletedTestXapRequest;
        protected Action<UIElement> OnReady;
        protected abstract void OnTestRunConfigurationDownloaded(ClientTestRunConfiguration clientTestRunConfiguration);

        protected static T LocateService<T>() where T : class
        {
            MessageBox.Show("HI");
            T service = null;
            try
            {
                Assembly[] list;
#if WINDOWS_PHONE
                Assembly.Load("StatLight.Client.Harness.Phone");
                //Assembly.Load("StatLight.Client.Harness.MSTest");
                var runnerHostType = Type.GetType("StatLight.Client.Harness.Hosts.MSTest.MSTestRunnerHost");
                MessageBox.Show(runnerHostType.ToString());
                //var constructorInfos = runnerHost.GetConstructor(Type.EmptyTypes);
                //MessageBox.Show((constructorInfos != null).ToString());
                //var obj = constructorInfos.Invoke(new object[0]);

                var parameterlessCtor = (from c in runnerHostType.GetConstructors(
  BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                         where c.GetParameters().Length == 0
                                         select c).FirstOrDefault();
                if (parameterlessCtor != null)
                    return (T)parameterlessCtor.Invoke(null);
                throw new NotImplementedException();

#else
                list = System.Windows.Deployment.Current.Parts.Select(
                            ap => System.Windows.Application.GetResourceStream(new Uri(ap.Source, UriKind.Relative))).Select(
                                stream => new System.Windows.AssemblyPart().Load(stream.Stream)).ToArray();
#endif
                var type = list
                    .SelectMany(s => s.GetTypes())
                    .Where(w => w != typeof(T))
                    .Where(w => typeof(T).IsAssignableFrom(w))
                    .ToArray();

                if (type.Length == 0)
                    throw new StatLightException(
                        "Could not locate an instance of [{0}] in the following assemblies [{1}]".FormatWith(
                            typeof(T).FullName,
                            string.Join("   - " + Environment.NewLine, list.Select(s => s.FullName).ToArray())));
                if (type.Length > 1)
                {
                    throw new StatLightException(
                        "Found multiple types that could be assignable from [{0}]. The types are [{1}]. The following assemblies were scanned [{2}]".FormatWith(
                            typeof(T).FullName,
                            string.Join("   - " + Environment.NewLine, type.Select(s => s.FullName).ToArray()),
                            string.Join("   - " + Environment.NewLine, list.Select(s => s.FullName).ToArray())));
                }
                service = (T)Activator.CreateInstance(type.Single());
            }
            catch (ReflectionTypeLoadException rfex)
            {
                ReflectionInfoHelper.HandleReflectionTypeLoadException(rfex);
            }

            if (service == null)
                Server.Trace("Could not locate service {0}.".FormatWith(typeof(T).FullName));

            return service;
        }

        public void GoGetTheTestRunConfiguration()
        {
            var client = new WebClient
                             {
                                 AllowReadStreamBuffering = true
                             };

            client.OpenReadCompleted += (sender, e) =>
                                            {
                                                var clientTestRunConfiguration = e.Result.Deserialize<ClientTestRunConfiguration>();
                                                ClientTestRunConfiguration.CurrentClientTestRunConfiguration = clientTestRunConfiguration;
                                                TestRunConfigurationDownloadComplete = true;
                                                TestRunnerHost.ConfigureWithClientTestRunConfiguration(clientTestRunConfiguration);
                                                OnTestRunConfigurationDownloaded(clientTestRunConfiguration);
                                            };
            client.OpenReadAsync(StatLightServiceRestApi.GetTestRunConfiguration.ToFullUri());
        }

        protected void DisplayTestHarness()
        {
            if (TestRunConfigurationDownloadComplete && CompletedTestXapRequest)
            {
                var rootVisual = TestRunnerHost.StartRun();
                OnReady(rootVisual);
            }
        }

        protected static void SetPostbackUri(Uri postbackUriBase)
        {
            StatLightServiceRestApi.PostbackUriBase = postbackUriBase;
        }

    }
}