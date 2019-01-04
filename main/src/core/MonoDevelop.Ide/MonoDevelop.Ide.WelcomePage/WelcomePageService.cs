//
// WelcomePageService.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui;
using Mono.Addins;
using System.Linq;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.WelcomePage
{
	public static class WelcomePageService
	{
		static bool visible;
		static WelcomePageFrame welcomePage;
		static IWelcomeWindowProvider welcomeWindowProvider;
		static Window welcomeWindow;

		public static event EventHandler WelcomePageShown;
		public static event EventHandler WelcomePageHidden;

		internal static void Initialize ()
		{
			IdeApp.Workbench.RootWindow.Hidden += (sender, e) => {
				if (!IdeApp.IsExiting && HasWindowImplementation) {
					ShowWelcomeWindow (new WelcomeWindowShowOptions (true));
				}
			};
			IdeApp.Workspace.FirstWorkspaceItemOpened += delegate {
				HideWelcomePageOrWindow ();
			};
			IdeApp.Workspace.LastWorkspaceItemClosed += delegate {
				if (!IdeApp.IsExiting && !IdeApp.Workspace.WorkspaceItemIsOpening) {
					ShowWelcomePageOrWindow ();
				}
			};
			IdeApp.Workbench.DocumentOpened += delegate {
				HideWelcomePageOrWindow ();
			};
			IdeApp.Workbench.DocumentClosed += delegate {
				if (!IdeApp.IsExiting && IdeApp.Workbench.Documents.Count == 0 && !IdeApp.Workspace.IsOpen) {
					ShowWelcomePageOrWindow ();
				}
			};
		}

		public static bool WelcomePageVisible => visible;

		public static bool WelcomeWindowVisible => welcomeWindow != null && visible;

		public static Window WelcomeWindow => welcomeWindow;

		public static bool HasWindowImplementation => AddinManager.GetExtensionObjects<IWelcomeWindowProvider> ().Any ();

		public static void ShowWelcomePageOrWindow (WelcomeWindowShowOptions options = null)
		{
			if (options == null) {
				options = new WelcomeWindowShowOptions (true);
			}

			// Try to get a dialog version of the "welcome screen" first
			if (!ShowWelcomeWindow (options)) {
				ShowWelcomePage (true);
			}
		}

		public static void HideWelcomePageOrWindow ()
		{
			if (HasWindowImplementation && welcomeWindowProvider != null && welcomeWindow != null) {
				welcomeWindowProvider.HideWindow (welcomeWindow);
			} else {
				HideWelcomePage (true);
			}

			visible = false;
		}

		public static void ShowWelcomePage (bool animate = false)
		{
			if (!visible) {
				visible = true;
				if (welcomePage == null) {
					var provider = AddinManager.GetExtensionObjects<IWelcomePageProvider> ().FirstOrDefault ();
					welcomePage = new WelcomePageFrame (provider != null ? provider.CreateWidget () : new DefaultWelcomePage ());
				}
				WelcomePageShown?.Invoke (welcomePage, EventArgs.Empty);
				welcomePage.UpdateProjectBar ();
				((DefaultWorkbench)IdeApp.Workbench.RootWindow).BottomBar.Visible = false;
				((DefaultWorkbench)IdeApp.Workbench.RootWindow).DockFrame.AddOverlayWidget (welcomePage, animate);
				welcomePage.GrabFocus ();
			}
		}

		public static void HideWelcomePage (bool animate = false)
		{
			if (visible) {
				visible = false;
				((DefaultWorkbench)IdeApp.Workbench.RootWindow).BottomBar.Show ();
				((DefaultWorkbench)IdeApp.Workbench.RootWindow).DockFrame.RemoveOverlayWidget (animate);
			}
			WelcomePageHidden?.Invoke (welcomePage, EventArgs.Empty);
		}

		public static bool ShowWelcomeWindow (WelcomeWindowShowOptions options)
		{
			if (welcomeWindowProvider == null) {
				welcomeWindowProvider = AddinManager.GetExtensionObjects<IWelcomeWindowProvider> ().FirstOrDefault ();
				if (welcomeWindowProvider == null)
					return false;
			}

			if (welcomeWindow == null) {
				welcomeWindow = welcomeWindowProvider.CreateWindow ();
				if (welcomeWindow == null)
					return false;
			}

			welcomeWindowProvider.ShowWindow (welcomeWindow, options);
			visible = true;

			return true;
		}
	}
}

