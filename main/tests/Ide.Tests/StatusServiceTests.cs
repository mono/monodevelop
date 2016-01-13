//
// StatusServiceTests.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc
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
using System.Text;
using NUnit.Framework;
using MonoDevelop.Ide;
using MonoDevelop.Components.MainToolbar;

namespace Ide.Tests
{
	static class Utils
	{
		public static string MakeRandomString ()
		{
			StringBuilder builder = new StringBuilder ();
			Random random = new Random ();
			int length = random.Next (32);

			for (int i = 0; i < length; i++) {
				var ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65))) ;
				builder.Append(ch);
			}

			return builder.ToString ();
		}
	}

	[TestFixture]
	public class StatusServiceTests
	{
		[TestFixtureSetUp]
		public void AddListener ()
		{
			StatusService.ContextAdded += ContextAdded;
			StatusService.ContextRemoved += ContextRemoved;
			StatusService.MainContext.MessageChanged += MessageChanged;
			StatusService.MainContext.ProgressChanged += ProgressChanged;
		}

		[TestFixtureTearDown]
		public void RemoveListener ()
		{
			StatusService.ContextAdded -= ContextAdded;
			StatusService.ContextRemoved -= ContextRemoved;
			StatusService.MainContext.MessageChanged -= MessageChanged;
			StatusService.MainContext.ProgressChanged -= ProgressChanged;
		}

		string expectedString;
		double expectedProgress;
		bool expectedProgressStarted;
		bool expectedProgressEnded;
		bool expectedProgressChanged;
		StatusMessageContext expectedContext;

		bool expectContextAdded = true;
		StatusMessageContext contextThatWasAdded;
		StatusMessageContext expectedContextRemoved;

		void ProgressChanged (object sender, StatusMessageContextProgressChangedArgs e)
		{
			switch (e.EventType) {
			case StatusMessageContextProgressChangedArgs.ProgressChangedType.Begin:
				Assert.IsTrue (expectedProgressStarted);
				expectedProgressStarted = false;
				break;

			case StatusMessageContextProgressChangedArgs.ProgressChangedType.Finish:
				Assert.IsTrue (expectedProgressEnded);
				expectedProgressEnded = false;
				break;

			case StatusMessageContextProgressChangedArgs.ProgressChangedType.Fraction:
				Assert.IsTrue (expectedProgressChanged);
				Assert.AreEqual (e.Work, expectedProgress);
				Assert.AreSame (expectedContext, e.Context);

				expectedProgress = 0;
				expectedProgressChanged = false;
				break;

			case StatusMessageContextProgressChangedArgs.ProgressChangedType.Pulse:
				// We're not testing this, so any Pulse messages are wrong
				Assert.Fail ();
				break;
			}
		}
			
		void MessageChanged (object sender, StatusMessageContextMessageChangedArgs e)
		{
			Assert.AreEqual (expectedString, e.Message);
			Assert.AreSame (expectedContext, e.Context);
			expectedString = null;
			expectedContext = null;
		}

		void ContextRemoved (object sender, StatusServiceContextEventArgs e)
		{
			Assert.NotNull (e.Context);
			Assert.AreSame (expectedContextRemoved, e.Context);
			expectedContextRemoved = null;

			e.Context.MessageChanged -= MessageChanged;
			e.Context.ProgressChanged -= ProgressChanged;
		}

		void ContextAdded (object sender, StatusServiceContextEventArgs e)
		{
			Assert.True (expectContextAdded);
			Assert.NotNull (e.Context);
			contextThatWasAdded = e.Context;

			e.Context.MessageChanged += MessageChanged;
			e.Context.ProgressChanged += ProgressChanged;

			expectContextAdded = false;
		}

		[Test]
		public void TestMainContextMessage ()
		{
			expectedString = Utils.MakeRandomString ();
			expectedContext = StatusService.MainContext;
			StatusService.MainContext.ShowMessage (expectedString);
		}

		[Test]
		public void TestMainContextProgress ()
		{
			expectedProgressStarted = true;
			expectedString = Utils.MakeRandomString ();
			expectedContext = StatusService.MainContext;
			StatusService.MainContext.BeginProgress (expectedString);

			expectedProgressStarted = false;

			for (double w = 0.0; w < 100.0; w += 10.0) {
				expectedProgress = w;
				expectedProgressChanged = true;
				StatusService.MainContext.SetProgressFraction (w);
			}

			expectedProgressEnded = true;
			StatusService.MainContext.EndProgress ();
		}

		[Test]
		public void TestRemoveContext ()
		{
			expectContextAdded = true;
			var newContext = StatusService.CreateContext ();

			Assert.AreSame (contextThatWasAdded, newContext);

			expectedContextRemoved = newContext;
			expectedContextRemoved.Dispose ();

			Assert.IsNull (expectedContextRemoved);
		}

		[Test]
		public void TestMessageOnNewContext ()
		{
			expectContextAdded = true;
			var newContext = expectedContext = StatusService.CreateContext ();

			expectedString = Utils.MakeRandomString ();
			expectedContext.ShowMessage (expectedString);

			Assert.IsNull (expectedString);
			Assert.IsNull (expectedContext);

			expectedContextRemoved = newContext;
			newContext.Dispose ();

			Assert.IsNull (expectedContextRemoved);
		}
	}

	[TestFixture]
	public class StatusBarContextHandlerTest
	{
		string expectedString;
		StatusMessageContext expectedContext;

		void MessageChanged (object sender, StatusMessageContextMessageChangedArgs e)
		{
			Assert.AreEqual (expectedString, e.Message);
			Assert.AreSame (expectedContext, e.Context);
			expectedString = null;
			expectedContext = null;
		}

		[Test]
		public void TestContextStacking ()
		{
			var contextHandler = new StatusBarContextHandler ();
			contextHandler.MessageChanged += MessageChanged;

			var context1 = StatusService.CreateContext ();
			var context2 = StatusService.CreateContext ();

			var expectedString1 = Utils.MakeRandomString ();
			var expectedString2 = Utils.MakeRandomString ();

			expectedString = expectedString1;
			expectedContext = context1;
			context1.ShowMessage (expectedString);

			Assert.IsNull (expectedString);
			Assert.IsNull (expectedContext);

			expectedString = expectedString2;
			expectedContext = context2;
			context2.ShowMessage (expectedString);

			Assert.IsNull (expectedString);
			Assert.IsNull (expectedContext);

			// context2 is the top context, changes to context1 should not trigger message changed
			var expectedString3 = Utils.MakeRandomString ();
			expectedString = expectedString3;
			expectedContext = context1;

			context1.ShowMessage (expectedString);

			// If no message changed was received, then expectedString and expectedContext will not be null
			Assert.IsNotNull (expectedString);
			Assert.IsNotNull (expectedContext);

			context2.Dispose ();

			// As context2 has been removed a message changed signal should be sent as well
			// and expectedString/expectedContext will now be null
			Assert.IsNull (expectedString);
			Assert.IsNull (expectedContext);
		}
	}
}

