// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
// Derived from https://github.com/dotnet/roslyn/blob/master/src/EditorFeatures/Core/Implementation/Notification/EditorNotificationServiceFactory.cs

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	[ExportWorkspaceServiceFactory (typeof (INotificationService), ServiceLayer.Editor)]
	[Shared]
	internal class EditorNotificationServiceFactory : IWorkspaceServiceFactory
	{
		private static object s_gate = new object ();

		private static EditorDialogService s_singleton;

		public IWorkspaceService CreateService (HostWorkspaceServices workspaceServices)
		{
			lock (s_gate) {
				if (s_singleton == null) {
					s_singleton = new EditorDialogService ();
				}
			}

			return s_singleton;
		}

		private class EditorDialogService : INotificationService, INotificationServiceCallback
		{

            /// <summary>
            /// For testing purposes only.  If non-null, this callback will be invoked instead of showing a dialog.
            /// </summary>
            public Action<string, string, NotificationSeverity> NotificationCallback { get; set; }

			public void SendNotification (
				string message,
				string title = null,
				NotificationSeverity severity = NotificationSeverity.Warning)
			{
				var callback = NotificationCallback;
				if (callback != null) {
					// invoke the callback
					callback (message, title, severity);
				} else {
					var image = SeverityToImage (severity);
					MessageService.GenericAlert (image, title, message, AlertButton.Ok);
				}
			}

			public bool ConfirmMessageBox (
				string message,
				string title = null,
				NotificationSeverity severity = NotificationSeverity.Warning)
			{
				var callback = NotificationCallback;
				if (callback != null) {
					// invoke the callback and assume 'Yes' was clicked.  Since this is a test-only scenario, assuming yes should be fine.
					callback (message, title, severity);
					return true;
				} else {
					var image = SeverityToImage (severity);
					return MessageService.GenericAlert (image, title, message, AlertButton.Yes, AlertButton.No) == AlertButton.Yes;
				}
			}

			private static IconId SeverityToImage (NotificationSeverity severity)
			{
				switch (severity) {
				case NotificationSeverity.Information:
					return Gui.Stock.Information;
				case NotificationSeverity.Warning:
					return Gui.Stock.Warning;
				default:
					return Gui.Stock.Error;
				}
			}
		}
	}
}
