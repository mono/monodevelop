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
		static readonly string DotNetCoreDownloadUrl = "https://aka.ms/vs/mac/install-netcore{0}";

		//FIXME: aka.ms is not available yet for netcore30 (https://dotnet.microsoft.com/download/dotnet-core/3.0)
		public static string GetDotNetCoreDownloadUrl (string version = "")
		{
			if (string.IsNullOrEmpty (version))
				return string.Format (DotNetCoreDownloadUrl, string.Empty);

			//special case for 2.0, 3.0, ..
			if (version.EndsWith (".0", StringComparison.InvariantCulture))
				version = version.Replace (".0", string.Empty);

			return string.Format (DotNetCoreDownloadUrl, version.Replace (".", string.Empty));
		}

		static readonly string defaultMessage = GettextCatalog.GetString (".NET Core SDK is not installed. This is required to build and run .NET Core projects.");
		static readonly string unsupportedMessage = GettextCatalog.GetString ("The .NET Core SDK installed is not supported. Please install a more recent version.");
		public static string GetDotNetCoreMessage (string version = "")
		{
			if (string.IsNullOrEmpty (version))
				return unsupportedMessage;

			return GettextCatalog.GetString (".NET Core {0} SDK is not installed. This is required to build and run .NET Core {0} projects.", version);
		}

		GenericMessage message;
		AlertButton downloadButton;
		string downloadUrl = DotNetCoreDownloadUrl;

		public DotNetCoreNotInstalledDialog ()
		{
			Build ();
		}

		void Build ()
		{
			message = new GenericMessage {
				Text = defaultMessage,
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
				DesktopService.ShowUrl (downloadUrl);
		}

		public void Dispose ()
		{
			message.AlertButtonClicked -= AlertButtonClicked;
		}

		public void Show ()
		{
			if (IsUnsupportedVersion)
				Message = GetDotNetCoreMessage ();
			else {
				Message = GetDotNetCoreMessage (RequiredDotNetCoreVersion.OriginalString);
				downloadUrl = GetDotNetCoreDownloadUrl (RequiredDotNetCoreVersion.OriginalString);
			}

			MessageService.GenericAlert (message);
		}

		public string Message {
			get { return message.Text; }
			set { message.Text = value; }
		}

		public bool IsUnsupportedVersion { get; set; }
		public DotNetCoreVersion RequiredDotNetCoreVersion { get; set; }
	}
}
