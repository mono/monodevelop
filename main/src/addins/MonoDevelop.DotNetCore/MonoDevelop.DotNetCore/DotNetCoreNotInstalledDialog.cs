//
// DotNetCoreNotInstalledDialog.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreNotInstalledDialog : IDisposable
	{
		GenericMessage message;
		AlertButton downloadButton;

		public DotNetCoreNotInstalledDialog ()
		{
			Build ();
		}

		void Build ()
		{
			message = new GenericMessage {
				Text = GettextCatalog.GetString (".NET Core is required to run this application."),
				DefaultButton = 1,
				Icon = Stock.Information
			};

			downloadButton = new AlertButton (GettextCatalog.GetString ("Download .NET Core..."));
			message.Buttons.Add (AlertButton.Cancel);
			message.Buttons.Add (downloadButton);

			message.AlertButtonClicked += AlertButtonClicked;
		}

		void AlertButtonClicked (object sender, AlertButtonEventArgs e)
		{
			if (e.Button == downloadButton)
				DesktopService.ShowUrl ("https://go.microsoft.com/fwlink/?linkid=834543");
		}

		public void Dispose ()
		{
			message.AlertButtonClicked -= AlertButtonClicked;
		}

		public void Show ()
		{
			MessageService.GenericAlert (message);
		}
	}
}
