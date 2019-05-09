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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.WelcomePage
{
	public static class WelcomePageService
	{
		static bool visible;
		static WelcomePageFrame welcomePage;
		static IWelcomeWindowProvider welcomeWindowProvider;
		static IWelcomeWindowProvider WelcomeWindowProvider {
			get {
				if (welcomeWindowProvider == null) {
					welcomeWindowProvider = AddinManager.GetExtensionObjects<IWelcomeWindowProvider> ().FirstOrDefault ();
				}

				return welcomeWindowProvider;
			}
		}

		public static event EventHandler WelcomePageShown;
		public static event EventHandler WelcomePageHidden;

		internal static async Task Initialize ()
		{
			IdeApp.Initialized += (s, args) => {
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
					if (!IdeApp.IsExiting && IdeApp.Workbench.Documents.Count == 0 && !IdeApp.Workspace.IsOpen && !HasWindowImplementation) {
						ShowWelcomePage ();
					}
				};
			};

			if (HasWindowImplementation) {
				await Runtime.GetService<DesktopService> ();
				var commandManager = await Runtime.GetService<CommandManager> ();
				await ShowWelcomeWindow (new WelcomeWindowShowOptions (false));
				// load the global menu for the welcome window to avoid unresponsive menus on Mac
				IdeServices.DesktopService.SetGlobalMenu (commandManager, "/MonoDevelop/Ide/MainMenu", "/MonoDevelop/Ide/AppMenu");
			}
		}

		public static bool WelcomePageVisible => visible;

		public static bool WelcomeWindowVisible => WelcomeWindowProvider?.IsWindowVisible ?? false;

		public static Window WelcomeWindow => WelcomeWindowProvider?.WindowInstance;

		public static bool HasWindowImplementation => WelcomeWindowProvider != null;

		public static async void ShowWelcomePageOrWindow (WelcomeWindowShowOptions options = null)
		{
			if (options == null) {
				options = new WelcomeWindowShowOptions (true);
			}

			// Try to get a dialog version of the "welcome screen" first
			if (!await ShowWelcomeWindow (options)) {
				ShowWelcomePage (true);
			}
		}

		public static void HideWelcomePageOrWindow ()
		{
			if (WelcomeWindowProvider != null) {
				WelcomeWindowProvider.HideWindow ();
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

		public static async Task<bool> ShowWelcomeWindow (WelcomeWindowShowOptions options)
		{
			if (WelcomeWindowProvider == null) {
				return false;
			}

			await WelcomeWindowProvider.ShowWindow (options);
			visible = true;

			return true;
		}
	}
}

