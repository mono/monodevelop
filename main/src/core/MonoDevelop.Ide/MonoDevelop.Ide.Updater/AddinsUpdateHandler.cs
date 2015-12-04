//
// AddinsUpdateHandler.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2011 Xamarin Inc.
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
using MonoDevelop.Core;
using Mono.Addins.Setup;
using Gtk;
using MonoDevelop.Core.ProgressMonitoring;
using Mono.Addins.Gui;
using MonoDevelop.Ide.ProgressMonitoring;
using Mono.Addins;
using MonoDevelop.Core.Setup;
using Mono.TextEditor;

namespace MonoDevelop.Ide.Updater
{
	[Extension("/MonoDevelop/Ide/Updater/UpdateHandlers", Id="DefaultUpdateHandler")]
	public class AddinsUpdateHandler: IUpdateHandler
	{
		AddinRepositoryEntry[] updates;
		static StatusBarIcon updateIcon;
		internal static AddinsUpdateHandler Instance;
		IProgressMonitor updateMonitor;

		public AddinsUpdateHandler ()
		{
			Instance = this;
		}

		public void CheckUpdates (IProgressMonitor monitor, bool automatic)
		{
			updateMonitor = monitor;
			try {
				if (UpdateService.UpdateLevel == UpdateLevel.Test)
					Runtime.AddinSetupService.RegisterMainRepository (UpdateLevel.Test, true);
				using (ProgressStatusMonitor pm = new ProgressStatusMonitor (monitor)) {
					Runtime.AddinSetupService.Repositories.UpdateAllRepositories (pm);
					updates = Runtime.AddinSetupService.Repositories.GetAvailableUpdates ();
					if (updates.Length > 0)
						DispatchService.GuiDispatch (new MessageHandler (WarnAvailableUpdates));
				}
			} finally {
				updateMonitor = null;
			}
		}

		void WarnAvailableUpdates ()
		{
			if (!UpdateService.NotifyAddinUpdates)
				return;

			updateIcon = IdeApp.Workbench.StatusBar.ShowStatusIcon (ImageService.GetIcon ("md-updates", IconSize.Menu));
			string s = GettextCatalog.GetString ("New add-in updates are available:");
			for (int n=0; n<updates.Length && n < 10; n++)
				s += "\n" + updates [n].Addin.Name;

			if (updates.Length > 10)
				s += "\n...";

			updateIcon.ToolTip = s;
			updateIcon.SetAlertMode (20);
			updateIcon.Clicked += OnUpdateClicked;
		}

		void OnUpdateClicked (object s, StatusBarIconClickedEventArgs args)
		{
			if (args.Button != Xwt.PointerButton.Right && args.Button == Xwt.PointerButton.Left) {
				HideAlert ();
				AddinManagerWindow.Run (IdeApp.Workbench.RootWindow);
			}
		}

		public static void ShowManager ()
		{
			IProgressMonitor m = Instance != null ? Instance.updateMonitor : null;
			if (m != null && !m.AsyncOperation.IsCompleted) {
				AggregatedProgressMonitor monitor = new AggregatedProgressMonitor (m);
				monitor.AddSlaveMonitor (new MessageDialogProgressMonitor (true, true, false));
				monitor.AsyncOperation.WaitForCompleted ();
			}
			HideAlert ();

			AddinManagerWindow.Run (IdeApp.Workbench.RootWindow);
		}

		internal void QueryAddinUpdates ()
		{
			IProgressMonitor monitor = updateMonitor;
			if (monitor != null)
				monitor.AsyncOperation.WaitForCompleted ();
		}

		public static void HideAlert ()
		{
			if (updateIcon != null) {
				updateIcon.Dispose ();
				updateIcon = null;
			}
		}
	}
}

