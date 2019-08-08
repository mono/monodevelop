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
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl
{
	class IgnoreCommand
	{
		public static Task<bool> IgnoreAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken) => IgnoreInternalAsync (items, test, cancellationToken);

		static async Task<bool> IgnoreInternalAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken)
		{
			try {
				if (test) {
					foreach (var item in items) {
						var info = await item.GetVersionInfoAsync (cancellationToken);
						if (info.Status != VersionStatus.Unversioned)
							return false;
					}
					return true;
				}

				if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to ignore the selected files?"),
				                                AlertButton.No, AlertButton.Yes) != AlertButton.Yes)
					return false;

				await new IgnoreWorker (items).StartAsync(cancellationToken).ConfigureAwait (false);
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

		private class IgnoreWorker : VersionControlTask
		{
			VersionControlItemList items;

			public IgnoreWorker (VersionControlItemList items)
			{
				this.items = items;
			}

			protected override string GetDescription()
			{
				return GettextCatalog.GetString ("Ignoring ...");
			}

			protected override async Task RunAsync ()
			{
				foreach (VersionControlItemList list in items.SplitByRepository ()) {
					try {
						await list[0].Repository.IgnoreAsync (list.Paths);
					} catch (Exception ex) {
						LoggingService.LogError ("Ignore operation failed", ex);
						Monitor.ReportError (ex.Message, null);
						return;
					}
				}
				Gtk.Application.Invoke ((o, args) => {
					VersionControlService.NotifyFileStatusChanged (items);
				});
				Monitor.ReportSuccess (GettextCatalog.GetString ("Ignore operation completed."));
			}
		}
	}

	class UnignoreCommand
	{
		public static Task<bool> UnignoreAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken) => UnignoreInternalAsync (items, test, cancellationToken);

		static async Task<bool> UnignoreInternalAsync (VersionControlItemList items, bool test, CancellationToken cancellationToken)
		{
			try {
				// NGit doesn't return a version info for ignored files.
				if (test) {
					foreach (var item in items) {
						var info = await item.GetVersionInfoAsync (cancellationToken);
						if ((info.Status & (VersionStatus.ScheduledIgnore | VersionStatus.Ignored)) == VersionStatus.Unversioned)
							return false;
					}
					return true;
				}

				if (MessageService.AskQuestion (GettextCatalog.GetString ("Are you sure you want to unignore the selected files?"),
				                                AlertButton.No, AlertButton.Yes) != AlertButton.Yes)
					return false;

				await new UnignoreWorker (items).StartAsync(cancellationToken).ConfigureAwait (false);
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

		private class UnignoreWorker : VersionControlTask
		{
			VersionControlItemList items;

			public UnignoreWorker (VersionControlItemList items)
			{
				this.items = items;
			}

			protected override string GetDescription()
			{
				return GettextCatalog.GetString ("Unignoring ...");
			}

			protected override async Task RunAsync ()
			{
				foreach (VersionControlItemList list in items.SplitByRepository ()) {
					try {
						await list[0].Repository.UnignoreAsync (list.Paths);
					} catch (Exception ex) {
						LoggingService.LogError ("Unignore operation failed", ex);
						Monitor.ReportError (ex.Message, null);
						return;
					}
				}

				Gtk.Application.Invoke ((o, args) => {
					VersionControlService.NotifyFileStatusChanged (items);
				});
				Monitor.ReportSuccess (GettextCatalog.GetString ("Unignore operation completed."));
			}
		}
	}
}
