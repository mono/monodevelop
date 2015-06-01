//
// VCSUtils.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using NUnit.Framework;
using MonoDevelop.Components.AutoTest;

namespace UserInterfaceTests
{
	public class VCSUtils
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		public enum VersionControlType
		{
			Git,
			Subversion
		}

		public static string CheckoutOrClone (string repoUrl, Action<string> screenshot, string cloneToLocation = null, VersionControlType cvsType = VersionControlType.Git, int cloneTimeoutSecs = 60)
		{
			cloneToLocation = cloneToLocation ?? Util.CreateTmpDir ("clone");
			Session.ExecuteCommand (MonoDevelop.VersionControl.Commands.Checkout);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog"));
			screenshot ("Checkout-Window-Ready");
			Assert.IsTrue (Session.SelectElement (c => c.Marked ("repCombo").Model ().Text (cvsType.ToString ())));
			Assert.IsTrue (Session.EnterText (c => c.Textfield ().Marked ("repositoryUrlEntry"), repoUrl));
			Assert.IsTrue (Session.EnterText (c => c.Textfield ().Marked ("entryFolder"), cloneToLocation));
			screenshot ("Before-Clicking-OK");
			Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog").Children ().Button ().Marked ("buttonOk")));
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.Dialogs.ProgressDialog"), 15000);
			screenshot ("CheckoutClone-In-Progress");
			Session.WaitForNoElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.Dialogs.ProgressDialog"), cloneTimeoutSecs * 1000);
			screenshot ("Checkout-Successful");

			return cloneToLocation;
		}
	}
}

