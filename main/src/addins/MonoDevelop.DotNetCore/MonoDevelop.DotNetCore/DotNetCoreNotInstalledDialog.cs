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
		public static readonly string DotNetCoreDownloadUrl = "https://aka.ms/vs/mac/install-netcore";
		public static readonly string DotNetCore20DownloadUrl = "https://aka.ms/vs/mac/install-netcore2";
		public static readonly string DotNetCore21DownloadUrl = "https://aka.ms/vs/mac/install-netcore21";
		public static readonly string DotNetCore22DownloadUrl = "https://aka.ms/vs/mac/install-netcore22";
		//FIXME: aka.ms is not available yet for netcore30 (https://dotnet.microsoft.com/download/dotnet-core/3.0)
		public static readonly string DotNetCore30DownloadUrl = "https://aka.ms/vs/mac/install-netcore30";

		static readonly string defaultMessage = GettextCatalog.GetString (".NET Core SDK is not installed. This is required to build and run .NET Core projects.");
		static readonly string unsupportedMessage = GettextCatalog.GetString ("The .NET Core SDK installed is not supported. Please install a more recent version.");
		static readonly string dotNetCore20Message = GettextCatalog.GetString (".NET Core 2.0 SDK is not installed. This is required to build and run .NET Core 2.0 projects.");
		static readonly string dotNetCore21Message = GettextCatalog.GetString (".NET Core 2.1 SDK is not installed. This is required to build and run .NET Core 2.1 projects.");
		static readonly string dotNetCore22Message = GettextCatalog.GetString (".NET Core 2.2 SDK is not installed. This is required to build and run .NET Core 2.2 projects.");
		static readonly string dotNetCore30Message = GettextCatalog.GetString (".NET Core 3.0 SDK is not installed. This is required to build and run .NET Core 3.0 projects.");

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
				Message = unsupportedMessage;
			else if (RequiresDotNetCore20) {
				Message = dotNetCore20Message;
				downloadUrl = DotNetCore20DownloadUrl;
			} else if (RequiresDotNetCore21) {
				Message = dotNetCore21Message;
				downloadUrl = DotNetCore21DownloadUrl;
			} else if (RequiresDotNetCore22) {
				Message = dotNetCore22Message;
				downloadUrl = DotNetCore22DownloadUrl;
			} else if (RequiresDotNetCore30) {
				Message = dotNetCore30Message;
				downloadUrl = DotNetCore30DownloadUrl;
			}

			MessageService.GenericAlert (message);
		}

		public string Message {
			get { return message.Text; }
			set { message.Text = value; }
		}

		public bool IsUnsupportedVersion { get; set; }
		public bool RequiresDotNetCore20 { get; set; }
		public bool RequiresDotNetCore21 { get; set; }
		public bool RequiresDotNetCore22 { get; set; }
		public bool RequiresDotNetCore30 { get; set; }
	}
}
