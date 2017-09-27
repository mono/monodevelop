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

using MonoDevelop.Ide.Status;

namespace Ide.Tests
{
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
			Assert.AreSame (expectedContextRemoved, e.Context);
			expectedContextRemoved = null;
		}

		void ContextAdded (object sender, StatusServiceContextEventArgs e)
		{
			Assert.True (expectContextAdded);
			contextThatWasAdded = e.Context;

			expectContextAdded = false;
		}

		[Test]
		public void TestMainContextMessage ()
		{
			expectedString = MakeRandomString ();
			expectedContext = StatusService.MainContext;
			StatusService.MainContext.ShowMessage (expectedString);
		}

		[Test]
		public void TestMainContextProgress ()
		{
			expectedProgressStarted = true;
			expectedString = MakeRandomString ();
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
		public void TestAddContext ()
		{
			expectContextAdded = true;
			var newContext = StatusService.CreateContext ();

			Assert.AreSame (contextThatWasAdded, newContext);
			contextThatWasAdded = null;
		}

		[Test]
		public void TestRemoveContext ()
		{
			expectContextAdded = true;
			var newContext = StatusService.CreateContext ();

			// Assume this worked as it is tested by AddContext

			expectedContextRemoved = newContext;
			expectedContextRemoved.Dispose ();
		}

		[Test]
		public void TestMessageOnNewContext ()
		{
			expectContextAdded = true;
			expectedContext = StatusService.CreateContext ();

			expectedString = MakeRandomString ();
			expectedContext.ShowMessage (expectedString);
		}

		string MakeRandomString ()
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
}
