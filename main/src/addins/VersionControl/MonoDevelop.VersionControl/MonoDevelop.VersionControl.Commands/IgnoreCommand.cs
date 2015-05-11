//
// IgnoreCommand.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Commands
{
	class IgnoreCommand
	{
		public static bool Ignore (List<VersionControlItem> items, bool test)
		{
			if (IgnoreInternal (items, test)) 
				return true;
			return false;
		}

		static bool IgnoreInternal (List<VersionControlItem> items, bool test)
		{
			try {
				if (test)
					return items.All (x => x.VersionInfo.Status == VersionStatus.Unversioned);

				if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to ignore the selected files?"),
				                                AlertButton.No, AlertButton.Yes) != AlertButton.Yes)
					return false;

				new IgnoreWorker (items).Start();
				return true;
			}
			catch (Exception ex) {
				if (test)
					LoggingService.LogError (ex.ToString ());
				else
					MessageService.ShowError (GettextCatalog.GetString ("Version control command failed."), ex);
				return false;
			}
		}

		private class IgnoreWorker : Task
		{
			List<VersionControlItem> items;

			public IgnoreWorker (List<VersionControlItem> items)
			{
				this.items = items;
			}

			protected override string GetDescription()
			{
				return GettextCatalog.GetString ("Ignoring ...");
			}

			protected override void Run ()
			{
				foreach (var list in items.SplitByRepository ())
					list.Key.Ignore (list.Value.GetPaths ());

				Monitor.ReportSuccess (GettextCatalog.GetString ("Ignore operation completed."));
				Gtk.Application.Invoke (delegate {
					foreach (VersionControlItem item in items)
						if (!item.IsDirectory)
							FileService.NotifyFileChanged (item.Path);

					VersionControlService.NotifyFileStatusChanged (items);
				});
			}
		}
	}

	class UnignoreCommand
	{
		public static bool Unignore (List<VersionControlItem> items, bool test)
		{
			if (UnignoreInternal (items, test))
				return true;
			return false;
		}

		static bool UnignoreInternal (List<VersionControlItem> items, bool test)
		{
			try {
				// NGit doesn't return a version info for ignored files.
				if (test)
					return items.All (x => (x.VersionInfo.Status & (VersionStatus.ScheduledIgnore | VersionStatus.Ignored)) != 0);

				if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to unignore the selected files?"),
				                                AlertButton.No, AlertButton.Yes) != AlertButton.Yes)
					return false;

				new UnignoreWorker (items).Start();
				return true;
			}
			catch (Exception ex) {
				if (test)
					LoggingService.LogError (ex.ToString ());
				else
					MessageService.ShowError (GettextCatalog.GetString ("Version control command failed."), ex);
				return false;
			}
		}

		private class UnignoreWorker : Task
		{
			List<VersionControlItem> items;

			public UnignoreWorker (List<VersionControlItem> items)
			{
				this.items = items;
			}

			protected override string GetDescription()
			{
				return GettextCatalog.GetString ("Unignoring ...");
			}

			protected override void Run ()
			{
				foreach (var list in items.SplitByRepository ())
					list.Key.Unignore (list.Value.GetPaths ());

				Monitor.ReportSuccess (GettextCatalog.GetString ("Unignore operation completed."));
				Gtk.Application.Invoke (delegate {
					foreach (VersionControlItem item in items)
						if (!item.IsDirectory)
							FileService.NotifyFileChanged (item.Path);

					VersionControlService.NotifyFileStatusChanged (items);
				});
			}
		}
	}
}

