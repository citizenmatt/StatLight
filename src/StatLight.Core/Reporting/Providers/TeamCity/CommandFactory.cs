namespace StatLight.Core.Reporting.Providers.TeamCity
{
	using System.Globalization;

	internal static class CommandFactory
	{
		public static Command TestSuiteStarted(string suiteName)
		{
			return new Command(CommandType.testSuiteStarted)
				.AddMessage("name", suiteName);
		}

		public static Command TestSuiteFinished(string suiteName)
		{
			return new Command(CommandType.testSuiteFinished)
				.AddMessage("name", suiteName);
		}

		public static Command TestStarted(string testName)
		{
			return new Command(CommandType.testStarted)
				.AddMessage("name", testName)
				.AddMessage("captureStandardOutput", "true");
		}

        public static Command TestFinished(string testName, double duration)
        {
            return new Command(CommandType.testFinished)
                .AddMessage("name", testName)
                .AddMessage("duration", duration.ToString(CultureInfo.InvariantCulture));
        }

        public static Command TestIgnored(string testName, string reason)
		{
			return new Command(CommandType.testIgnored)
				.AddMessage("name", testName)
				.AddMessage("message", reason);
		}

		public static Command TestFailed(string testName, string message, string details)
		{
			return new Command(CommandType.testFailed)
				.AddMessage("name", testName)
				.AddMessage("message", message)
				.AddMessage("details", details);
		}

		public static Command TestFailed(string testName, string message, string details, string type, string expected, string actual)
		{
			return new Command(CommandType.testFailed)
				.AddMessage("type", type)
				.AddMessage("name", testName)
				.AddMessage("message", message)
				.AddMessage("details", details)
				.AddMessage("expected", expected)
				.AddMessage("actual", actual);
		}

		public static Command TestStdErr(string testName, string @out)
		{
			return new Command(CommandType.testStdErr)
				.AddMessage("name", testName)
				.AddMessage("out", @out);
		}

		public static Command TestStdOut(string testName, string @out)
		{
			return new Command(CommandType.testStdOut)
				.AddMessage("name", testName)
				.AddMessage("out", @out);
		}
	}
}
