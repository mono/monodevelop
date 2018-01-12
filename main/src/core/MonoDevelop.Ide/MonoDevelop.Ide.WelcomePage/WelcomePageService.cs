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

namespace MonoDevelop.Ide.WelcomePage
{
	public static class WelcomePageService
	{
		static bool visible;
		static WelcomePageFrame welcomePage;

		public static event EventHandler WelcomePageShown;
		public static event EventHandler WelcomePageHidden;

		internal static void Initialize ()
		{
			IdeApp.Workspace.FirstWorkspaceItemOpened += delegate {
				HideWelcomePage ();
			};
			IdeApp.Workspace.LastWorkspaceItemClosed += delegate {
				ShowWelcomePage ();
			};
			IdeApp.Workbench.DocumentOpened += delegate {
				HideWelcomePage ();
			};
			IdeApp.Workbench.DocumentClosed += delegate {
				if (IdeApp.Workbench.Documents.Count == 0 && !IdeApp.Workspace.IsOpen)
					ShowWelcomePage ();
			};
		}

		public static bool WelcomePageVisible {
			get { return visible; }
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
	}
}

