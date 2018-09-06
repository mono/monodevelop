//
// MonoDevelopInfoBarService.cs
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
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Imaging;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using Roslyn.Utilities;

namespace MonoDevelop.Ide.RoslynServices
{
	[ExportWorkspaceService (typeof (IInfoBarService), layer: ServiceLayer.Host), Shared]
	sealed class MonoDevelopInfoBarService : ForegroundThreadAffinitizedObject, IInfoBarService
	{
		readonly IForegroundNotificationService _foregroundNotificationService;
		readonly IAsynchronousOperationListener _listener;

		[ImportingConstructor]
		public MonoDevelopInfoBarService (IForegroundNotificationService foregroundNotificationService, IAsynchronousOperationListenerProvider listenerProvider)
		{
			_foregroundNotificationService = foregroundNotificationService;
			_listener = listenerProvider.GetListener (FeatureAttribute.InfoBar);
		}

		public void ShowInfoBarInActiveView (string message, params InfoBarUI [] items)
		{
			ThisCanBeCalledOnAnyThread ();
			ShowInfoBar (activeView: true, message: message, items: items);
		}

		public void ShowInfoBarInGlobalView (string message, params InfoBarUI [] items)
		{
			ThisCanBeCalledOnAnyThread ();
			ShowInfoBar (activeView: false, message: message, items: items);
		}

		void ShowInfoBar (bool activeView, string message, params InfoBarUI [] items)
		{
			// We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
			_foregroundNotificationService.RegisterNotification (() => {
				if (TryGetInfoBarHost (activeView, out var infoBarHost)) {
					var options = new InfoBarOptions (message) {
						Items = ToUIItems (items)
					};
					infoBarHost.AddInfoBar (options);
				}
			}, _listener.BeginAsyncOperation (nameof (ShowInfoBar)));
		}

		static InfoBarItem [] ToUIItems (InfoBarUI [] items)
		{
			return items.Select (x => new InfoBarItem (x.Title, ToUIKind (x.Kind), x.Action, x.CloseAfterAction)).ToArray ();
		}

		static InfoBarItemKind ToUIKind (InfoBarUI.UIKind kind)
		{
			switch (kind) {
			case InfoBarUI.UIKind.Button:
				return InfoBarItemKind.Button;
			case InfoBarUI.UIKind.Close:
				return InfoBarItemKind.Close;
			case InfoBarUI.UIKind.HyperLink:
				return InfoBarItemKind.Hyperlink;
			default:
				LoggingService.LogError ("Unknown InfoBarUI.UIKind value {0}", kind.ToString ());
				return InfoBarItemKind.Button;
			}

		}

		bool TryGetInfoBarHost (bool activeView, out IInfoBarHost infoBarHost)
		{
			AssertIsForeground ();

			infoBarHost = null;
			if (!IdeApp.IsInitialized || IdeApp.Workbench == null)
				return false;

			if (activeView) {
				// Maybe for pads also? Not sure if we should.
				infoBarHost = IdeApp.Workbench.ActiveDocument as IInfoBarHost;
			}

			if (infoBarHost == null)
				infoBarHost = IdeApp.Workbench.RootWindow as IInfoBarHost;

			return infoBarHost != null;
		}
	}
}
