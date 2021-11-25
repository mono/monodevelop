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
		public static event EventHandler WelcomeWindowShown;
		public static event EventHandler WelcomeWindowHidden;

		internal static async Task Initialize (bool hideWelcomePage)
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

			if (!hideWelcomePage && HasWindowImplementation) {
				await Runtime.GetService<DesktopService> ();
				var commandManager = await Runtime.GetService<CommandManager> ();

				var reason = await IdeApp.LaunchCompletionSource.Task;

				if (IdeApp.LaunchReason == IdeApp.LaunchType.Normal) {
					await ShowWelcomeWindow (new WelcomeWindowShowOptions (false));
				} else if (IdeApp.LaunchReason == IdeApp.LaunchType.Unknown) {
					LoggingService.LogInternalError ("LaunchCompletion is still Unknown", new Exception ());
				}
			}
		}

		public static bool WelcomePageVisible => visible;

		public static bool WelcomeWindowVisible => WelcomeWindowProvider?.IsWindowVisible ?? false;

		public static Window WelcomeWindow => WelcomeWindowProvider?.WindowInstance;

		public static bool HasWindowImplementation => WelcomeWindowProvider != null;

		public static async Task ShowWelcomePageOrWindow (WelcomeWindowShowOptions options = null)
		{
			if (options == null) {
				options = new WelcomeWindowShowOptions (true);
			}

			// Try to get a dialog version of the "welcome screen" first
			if (!await ShowWelcomeWindow (options)) {
				await ShowWelcomePage (true);
			}
		}

		public static async void HideWelcomePageOrWindow ()
		{
			await Runtime.RunInMainThread (async () => {
				if (WelcomeWindowProvider != null) {
					await WelcomeWindowProvider.HideWindow ();
					visible = false;
					WelcomeWindowHidden?.Invoke (WelcomeWindow, EventArgs.Empty);
				} else {
					await HideWelcomePage (true);
				}
			});
		}

		public static async Task ShowWelcomePage (bool animate = false)
		{
			if (!visible) {
				visible = true;
				await Runtime.RunInMainThread (() => {
					if (welcomePage == null) {
						var provider = AddinManager.GetExtensionObjects<IWelcomePageProvider> ().FirstOrDefault ();
						welcomePage = new WelcomePageFrame (provider != null ? provider.CreateWidget () : new DefaultWelcomePage ());
					}
					WelcomePageShown?.Invoke (welcomePage, EventArgs.Empty);
					welcomePage.UpdateProjectBar ();
				
					var rootWindow = (DefaultWorkbench)IdeApp.Workbench.RootWindow;
					if (rootWindow.BottomBar is MonoDevelopStatusBar statusBar) {
						statusBar.Visible = false;
					}

					if (rootWindow.DockFrame is Components.Docking.DockFrame dockFrame) {
						dockFrame.AddOverlayWidget (welcomePage, animate);
					}
					welcomePage.GrabFocus ();
				});
			}
		}

		public static async Task HideWelcomePage (bool animate = false)
		{
			if (visible) {
				visible = false;
				await Runtime.RunInMainThread (() => {
					((DefaultWorkbench)IdeApp.Workbench.RootWindow).BottomBar.Show ();
					((DefaultWorkbench)IdeApp.Workbench.RootWindow).DockFrame.RemoveOverlayWidget (animate);
					WelcomePageHidden?.Invoke (welcomePage, EventArgs.Empty);
				});
			}
		}

		public static async Task<bool> ShowWelcomeWindow (WelcomeWindowShowOptions options)
		{
			if (!HasWindowImplementation) {
				return false;
			}

			await Runtime.RunInMainThread (async () => {
				await WelcomeWindowProvider.ShowWindow (options);
				visible = true;
				WelcomeWindowShown?.Invoke (WelcomeWindow, EventArgs.Empty);
			});

			return true;
		}
	}
}

