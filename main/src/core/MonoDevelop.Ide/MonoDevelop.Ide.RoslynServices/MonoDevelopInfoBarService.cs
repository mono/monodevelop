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
	[ExportWorkspaceService(typeof(IInfoBarService), layer: ServiceLayer.Host), Shared]
    sealed class MonoDevelopInfoBarService : ForegroundThreadAffinitizedObject, IInfoBarService
    {
        readonly IForegroundNotificationService _foregroundNotificationService;
        readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        public MonoDevelopInfoBarService(IForegroundNotificationService foregroundNotificationService, IAsynchronousOperationListenerProvider listenerProvider)
        {
            _foregroundNotificationService = foregroundNotificationService;
            _listener = listenerProvider.GetListener(FeatureAttribute.InfoBar);
        }

        public void ShowInfoBarInActiveView(string message, params InfoBarUI[] items)
        {
            ThisCanBeCalledOnAnyThread();
            ShowInfoBar(activeView: true, message: message, items: items);
        }

        public void ShowInfoBarInGlobalView(string message, params InfoBarUI[] items)
        {
            ThisCanBeCalledOnAnyThread();
            ShowInfoBar(activeView: false, message: message, items: items);
        }

        void ShowInfoBar(bool activeView, string message, params InfoBarUI[] items)
        {
            // We can be called from any thread since errors can occur anywhere, however we can only construct and InfoBar from the UI thread.
            _foregroundNotificationService.RegisterNotification(() =>
            {
				if (TryGetInfoBarHost (activeView, out var infoBarHost)) {
					infoBarHost.AddInfoBar (message, ToUIItems (items));
				}
            }, _listener.BeginAsyncOperation(nameof(ShowInfoBar)));
        }

		static InfoBarItem[] ToUIItems (InfoBarUI[] items)
		{
			return items.Select (x => new InfoBarItem (x.Title, ToUIKind (x.Kind), x.Action, x.CloseAfterAction)).ToArray ();
		}

		static InfoBarItem.InfoBarItemKind ToUIKind (InfoBarUI.UIKind kind)
		{
			switch (kind)
			{
			case InfoBarUI.UIKind.Button:
				return InfoBarItem.InfoBarItemKind.Button;
			case InfoBarUI.UIKind.Close:
				return InfoBarItem.InfoBarItemKind.Close;
			case InfoBarUI.UIKind.HyperLink:
				return InfoBarItem.InfoBarItemKind.Hyperlink;
			default:
				LoggingService.LogError ("Unknown InfoBarUI.UIKind value {0}", kind.ToString ());
				return InfoBarItem.InfoBarItemKind.Button;
			}

		}

        bool TryGetInfoBarHost(bool activeView, out IInfoBarHost infoBarHost)
        {
            AssertIsForeground();

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
 /*
		void CreateInfoBar(IInfoBarHost infoBarHost, string message, InfoBarUI[] items)
        {
            var factory = _serviceProvider.GetService(typeof(SVsInfoBarUIFactory)) as IVsInfoBarUIFactory;
            if (factory == null)
            {
                // no info bar factory, don't do anything
                return;
            }

            var textSpans = new List<IVsInfoBarTextSpan>()
            {
                new InfoBarTextSpan(message)
            };

            // create action item list
            var actionItems = new List<IVsInfoBarActionItem>();

            foreach (var item in items)
            {
                switch (item.Kind)
                {
                    case InfoBarUI.UIKind.Button:
                        actionItems.Add(new InfoBarButton(item.Title));
                        break;
                    case InfoBarUI.UIKind.HyperLink:
                        actionItems.Add(new InfoBarHyperlink(item.Title));
                        break;
                    case InfoBarUI.UIKind.Close:
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(item.Kind);
                }
            }

            var infoBarModel = new InfoBarModel(
                textSpans,
                actionItems.ToArray(),
                KnownMonikers.StatusInformation,
                isCloseButtonVisible: true);

            if (!TryCreateInfoBarUI(factory, infoBarModel, out var infoBarUI))
            {
                return;
            }

            uint? infoBarCookie = null;
            var eventSink = new InfoBarEvents(items, () =>
            {
                // run given onClose action if there is one.
                items.FirstOrDefault(i => i.Kind == InfoBarUI.UIKind.Close).Action?.Invoke();

                if (infoBarCookie.HasValue)
                {
                    infoBarUI.Unadvise(infoBarCookie.Value);
                }
            });

            infoBarUI.Advise(eventSink, out var cookie);
            infoBarCookie = cookie;

            infoBarHost.AddInfoBar(infoBarUI);
        }

        private class InfoBarEvents : IVsInfoBarUIEvents
        {
            private readonly InfoBarUI[] _items;
            private readonly Action _onClose;

            public InfoBarEvents(InfoBarUI[] items, Action onClose)
            {
                Contract.ThrowIfNull(onClose);

                _items = items;
                _onClose = onClose;
            }

            public void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
            {
                var item = _items.FirstOrDefault(i => i.Title == actionItem.Text);
                if (item.IsDefault)
                {
                    return;
                }

                item.Action?.Invoke();

                if (!item.CloseAfterAction)
                {
                    return;
                }

                infoBarUIElement.Close();
            }

            public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
            {
                _onClose();
            }
        }

        private static bool TryCreateInfoBarUI(IVsInfoBarUIFactory infoBarUIFactory, IVsInfoBar infoBar, out IVsInfoBarUIElement uiElement)
        {
            uiElement = infoBarUIFactory.CreateInfoBar(infoBar);
            return uiElement != null;
        }
        */
    }
}
