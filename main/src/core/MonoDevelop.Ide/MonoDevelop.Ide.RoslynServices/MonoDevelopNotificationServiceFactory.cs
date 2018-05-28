//
// MonoDevelopNotificationService.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.RoslynServices
{
	[ExportWorkspaceServiceFactory (typeof (INotificationService), ServiceLayer.Host)]
	[Shared]
	class MonoDevelopNotificationServiceFactory : IWorkspaceServiceFactory
	{
		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices) => new MonoDevelopNotificationService ();

		internal class MonoDevelopNotificationService : INotificationService, INotificationServiceCallback
		{
			/// <summary>
			/// For testing purposes only.  If non-null, this callback will be invoked instead of showing a dialog.
			/// </summary>
			public Action<string, string, NotificationSeverity> NotificationCallback { get; set; }

			public bool ConfirmMessageBox (string message, string title = null, NotificationSeverity severity = NotificationSeverity.Warning)
			{
				if (NotificationCallback != null) {
					NotificationCallback?.Invoke (message, title, severity);
					return true;
				}

				return ShowAlert (message, title, severity, AlertButton.Yes, AlertButton.No) == AlertButton.Yes;
			}

			public void SendNotification (string message, string title = null, NotificationSeverity severity = NotificationSeverity.Warning)
			{
				if (NotificationCallback != null) {
					// invoke the callback and assume 'Yes' was clicked.  Since this is a test-only scenario, assuming yes should be fine.
					NotificationCallback?.Invoke (message, title, severity);
					return;
				}

				ShowAlert (message, title, severity, AlertButton.Ok);
			}

			// Roslyn usually does not set a title, only a message.
			AlertButton ShowAlert (string message, string title, NotificationSeverity severity, params AlertButton[] buttons)
			{
				string primary = title ?? message;
				string secondary = title != null ? message : null;
				string icon = GetSeverityIcon (severity);

				return MessageService.GenericAlert (icon, primary, secondary, buttons);
			}

			static string GetSeverityIcon (NotificationSeverity severity)
			{
				switch (severity)
				{
				case NotificationSeverity.Error:
					return Gui.Stock.Error;
				case NotificationSeverity.Information:
					return Gui.Stock.Information;
				case NotificationSeverity.Warning:
					return Gui.Stock.Warning;
				}
				LoggingService.LogError ("Unknown NotificationSeverity value {0}", severity.ToString ());
				return Gui.Stock.Information;
			}
		}
	}
}
