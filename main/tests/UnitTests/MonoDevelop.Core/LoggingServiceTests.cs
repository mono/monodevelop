//
// LoggingServiceTests.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using MonoDevelop.Core.Logging;
using NUnit.Framework;
using System.Collections.Generic;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class LoggingServiceTests
	{
		readonly LoggingServiceTestsLogger logger = new LoggingServiceTestsLogger ();

		[SetUp]
		public void SetUp ()
		{
			logger.EnabledLevel = EnabledLoggingLevel.All;
			LoggingService.AddLogger (logger);
		}

		[TearDown]
		public void TearDown ()
		{
			LoggingService.RemoveLogger (logger.Name);
		}

		void AssertLastMessageEqual (string message, LogLevel level)
		{
			Assert.AreEqual (level, logger.Messages [logger.Messages.Count - 1].Item1);
			AssertLastMessageEqual (message);
		}

		void AssertLastMessageEqual (string message)
		{
			Assert.AreEqual (message, logger.Messages [logger.Messages.Count - 1].Item2);
		}

		[Test]
		public void CheckWeGotDefaultLoggers ()
		{
			Assert.NotNull (LoggingService.GetLogger ("ConsoleLogger"));
			Assert.NotNull (LoggingService.GetLogger ("Instrumentation logger"));
		}

		[Test]
		public void CheckWeGotOurLogger ()
		{
			Assert.AreSame (logger, LoggingService.GetLogger (logger.Name));
		}

		/*
		 * Find a way to get only this logger.
		[TestCase (EnabledLoggingLevel.None,	false,	false,	false,	false,	false)]
		[TestCase (EnabledLoggingLevel.Fatal,	true,	false,	false,	false,	false)]
		[TestCase (EnabledLoggingLevel.Error,	false,	true,	false,	true,	false)]
		[TestCase (EnabledLoggingLevel.Warn,	false,	false,	true,	false,	false)]
		[TestCase (EnabledLoggingLevel.Info,	false,	false,	false,	true,	false)]
		[TestCase (EnabledLoggingLevel.Debug,	false,	false,	false,	false,	true)]
		[TestCase (EnabledLoggingLevel.All,		true,	true,	true,	true,	true)]
		public void TestLoggingLevels (EnabledLoggingLevel levelToSet, bool fatalEnabled, bool errorEnabled, bool warnEnabled, bool infoEnabled, bool debugEnabled)
		{
			logger.EnabledLevel = levelToSet;

			Assert.AreEqual (fatalEnabled, LoggingService.IsLevelEnabled (LogLevel.Fatal));
			Assert.AreEqual (errorEnabled, LoggingService.IsLevelEnabled (LogLevel.Error));
			Assert.AreEqual (warnEnabled, LoggingService.IsLevelEnabled (LogLevel.Warn));
			Assert.AreEqual (infoEnabled, LoggingService.IsLevelEnabled (LogLevel.Info));
			Assert.AreEqual (debugEnabled, LoggingService.IsLevelEnabled (LogLevel.Debug));
		}
		*/

		[TestCase (LogLevel.Fatal, "LogFatalError")]
		[TestCase (LogLevel.Error, "LogError")]
		[TestCase (LogLevel.Warn, "LogWarning")]
		[TestCase (LogLevel.Info, "LogInfo")]
		[TestCase (LogLevel.Debug, "LogDebug")]
		public void TestSimpleLogging (LogLevel level, string methodName)
		{
			const string message = "This is a log message";
			const string format = "{0}";
			string[] exceptionMessage = new[] {
				"This is a log message",
				"System.Exception: Exception of type 'System.Exception' was thrown.",
				"  at MonoDevelop.Core.LoggingServiceTests.TestSimpleLogging (LogLevel level, System.String methodName)", // [0x000c7] in /path/to/monodevelop/main/tests/UnitTests/MonoDevelop.Core/LoggingServiceTests.cs:LINENO
				"Exception Data:",
				"key: value",
				"key2: value2"
			};

			var logMethod = typeof(LoggingService).GetMethod (methodName, new[] { typeof(string) });
			logMethod.Invoke (null, new[] { message });
			AssertLastMessageEqual (message, level);

			var logFormat = typeof(LoggingService).GetMethod (methodName, new[] { typeof(string), typeof(object[]) });
			logFormat.Invoke (null, new object[] { format, new object[] { message } });
			AssertLastMessageEqual (message, level);

			try {
				var e = new Exception ();
				e.Data["key"] = "value";
				e.Data["key2"] = "value2";
				throw e;
			} catch (Exception e) {
				// Test exception logging.
				var logException = typeof(LoggingService).GetMethod (methodName, new[] { typeof(string), typeof(Exception) });
				logException.Invoke (null, new object[] { message, e });

				var levelMessage = logger.Messages [logger.Messages.Count - 1];
				var actualMessage = levelMessage.Item2.Split (new[] { Environment.NewLine }, StringSplitOptions.None);
				var actualLevel = levelMessage.Item1;

				Assert.AreEqual (level, actualLevel);
				for (int i = 0; i < actualMessage.Length; ++i)
					Assert.IsTrue (actualMessage[i].StartsWith (exceptionMessage[i]), "Line {0} mismatches.{1}Expected: {2}{3}Actual: {4}", i, Environment.NewLine,
						exceptionMessage[i], Environment.NewLine, actualMessage[i]);

				// Test that the message is the same when no exception is sent.
				logException.Invoke (null, new object[] { message, null });
				Assert.AreSame (message, logger.Messages [logger.Messages.Count - 1].Item2);
			}
		}

		class LoggingServiceTestsLogger : ILogger
		{
			#region ILogger implementation
			List<Tuple<LogLevel, string>> messages = new List<Tuple<LogLevel, string>> ();

			public LoggingServiceTestsLogger ()
			{
			}

			public IReadOnlyList<Tuple<LogLevel, string>> Messages {
				get { return messages; }
			}

			public void Log (LogLevel level, string message)
			{
				messages.Add (new Tuple<LogLevel, string> (level, message));
			}

			EnabledLoggingLevel enabledLevel;
			public EnabledLoggingLevel EnabledLevel {
				get { return enabledLevel; }
				set { enabledLevel = value; }
			}

			public string Name {
				get { return "Logging tests logger"; }
			}

			#endregion
		}
	}
}

