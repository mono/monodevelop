//
// PackageOperationMessageTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using NuGet.ProjectManagement;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageOperationMessageTests
	{
		[Test]
		public void Level_CreateInfoMessage_CreatesMessageWithMessageLevelSetToInfo ()
		{
			var message = new PackageOperationMessage (MessageLevel.Info, "test");

			Assert.AreEqual (MessageLevel.Info, message.Level);
		}

		[Test]
		public void Level_CreateWarningMessage_CreatesMessageWithMessageLevelSetToWarning ()
		{
			var message = new PackageOperationMessage (MessageLevel.Warning, "test");

			Assert.AreEqual (MessageLevel.Warning, message.Level);
		}

		[Test]
		public void ToString_CreateWarningMessage_ReturnsMessage ()
		{
			var message = new PackageOperationMessage (MessageLevel.Warning, "test");
			var text = message.ToString ();

			Assert.AreEqual ("test", text);
		}

		[Test]
		public void ToString_CreateFormattedWarningMessage_ReturnsFormattedMessage ()
		{
			string format = "Test '{0}'.";
			var message = new PackageOperationMessage (MessageLevel.Warning, format, "A");
			var text = message.ToString ();

			var expectedText = "Test 'A'.";
			Assert.AreEqual (expectedText, text);
		}
	}
}