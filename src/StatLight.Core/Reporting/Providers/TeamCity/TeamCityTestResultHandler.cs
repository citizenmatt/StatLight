
using StatLight.Core.Events.Aggregation;
using StatLight.Core.Properties;

namespace StatLight.Core.Reporting.Providers.TeamCity
{
    using System;
    using StatLight.Client.Harness.Events;
    using StatLight.Core.Reporting;
    using StatLight.Core.Events;

    public class TeamCityTestResultHandler : ITestingReportEvents,
        IListener<TestExecutionClassBeginClientEvent>,
        IListener<TestExecutionClassCompletedClientEvent>,
        IListener<TestExecutionMethodBeginClientEvent>
    {
        private readonly ICommandWriter _messageWriter;
        private readonly string _assemblyName;

        public TeamCityTestResultHandler(ICommandWriter messageWriter, string assemblyName)
        {
            _messageWriter = messageWriter;
            _assemblyName = assemblyName;
        }

        public void PublishStart()
        {
            _messageWriter.Write(CommandFactory.TestSuiteStarted(_assemblyName));
        }

        public void PublishStop()
        {
            _messageWriter.Write(CommandFactory.TestSuiteFinished(_assemblyName));
        }

        private void WrapTestWithStartAndEnd(Action action, string name, long durationMilliseconds)
        {
            _messageWriter.Write(CommandFactory.TestStarted(name));
            action();
            _messageWriter.Write(CommandFactory.TestFinished(name, durationMilliseconds));
        }

        public void Handle(TraceClientEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            _messageWriter.Write(message.Message);
        }

        public void Handle(DialogAssertionServerEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            string writeMessage = message.Message;
            WriteServerEventFailure("DialogAssertionServerEvent", writeMessage);
        }

        private void WriteServerEventFailure(string name, string writeMessage)
        {
            const int durationMilliseconds = 0;

            WrapTestWithStartAndEnd(() => _messageWriter.Write(
                CommandFactory.TestFailed(
                    name,
                    writeMessage,
                    writeMessage)),
                name,
                durationMilliseconds);
        }

        public void Handle(BrowserHostCommunicationTimeoutServerEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            string writeMessage = message.Message;
            WriteServerEventFailure("BrowserHostCommunicationTimeoutServerEvent", writeMessage);
        }

        public void Handle(TestExecutionClassBeginClientEvent message)
        {
            _messageWriter.Write(CommandFactory.TestSuiteStarted(message.NamespaceName + "." + message.ClassName));
        }

        public void Handle(TestExecutionClassCompletedClientEvent message)
        {
            _messageWriter.Write(CommandFactory.TestSuiteFinished(message.NamespaceName + "." + message.ClassName));
        }

        public void Handle(TestExecutionMethodBeginClientEvent message)
        {
            _messageWriter.Write(CommandFactory.TestStarted(message.MethodName));
        }

        public void Handle(TestCaseResult message)
        {
            if (message == null) throw new ArgumentNullException("message");
            var name = message.MethodName;
            var durationMilliseconds = message.TimeToComplete.TotalMilliseconds;

            switch (message.ResultType)
            {
                case ResultType.Ignored:
                    _messageWriter.Write(CommandFactory.TestIgnored(name, string.Empty));
                    break;

                case ResultType.Passed:
                    break;

                case ResultType.Failed:
                    _messageWriter.Write(CommandFactory.TestFailed(name, message.ExceptionInfo.FullMessage, message.ExceptionInfo.FullMessage));
                    break;

                case ResultType.SystemGeneratedFailure:
                    _messageWriter.Write(CommandFactory.TestFailed(name, "StatLight generated test failure", message.OtherInfo));
                    break;

                default:
                    "Unknown TestCaseResult (to StatLight) - {0}".FormatWith(message.ResultType)
                        .WrapConsoleMessageWithColor(Settings.Default.ConsoleColorError, true);
                    break;
            }

            _messageWriter.Write(CommandFactory.TestFinished(name, durationMilliseconds));
        }

        public void Handle(FatalSilverlightExceptionServerEvent message)
        {
            if (message == null) throw new ArgumentNullException("message");
            string writeMessage = message.Message;
            WriteServerEventFailure("FatalSilverlightExceptionServerEvent", writeMessage);
        }
    }
}
