using System;
using System.IO;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Gui.Documents;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl.Views
{
	public interface ILogView
	{
	}

	class LogView : BaseView, ILogView
	{
		LogWidget widget;
		VersionInfo vinfo;
		VersionControlDocumentInfo info;

		public LogWidget LogWidget {
			get {
				if (widget == null)
					CreateControlFromInfo ();
				return widget;
			}
		}

		public static bool CanShow (VersionControlItemList items, Revision since)
		{
			return items.All (i => i.VersionInfo.CanLog);
		}

		public LogView (VersionControlDocumentInfo info) : base (GettextCatalog.GetString ("Log"), GettextCatalog.GetString ("Shows the source control log for the current file"))
		{
			this.info = info;
		}

		async void CreateControlFromInfo ()
		{
			var lw = new LogWidget (info);

			try {
				widget = lw;
				info.Updated += OnInfoUpdated;
				lw.History = this.info.History;
				vinfo = await this.info.Item.GetVersionInfoAsync ();
				Init ();
			} catch (Exception e) {
				LoggingService.LogInternalError (e);
			}
		}

		async void OnInfoUpdated (object sender, EventArgs e)
		{
			try {
				widget.History = this.info.History;
				vinfo = await info.Item.GetVersionInfoAsync ();
			} catch (Exception ex) {
				LoggingService.LogInternalError (ex);
			}
		}

		protected override Task<Control> OnGetViewControlAsync (CancellationToken token, DocumentViewContent view)
		{
			LogWidget.SetToolbar (view.GetToolbar ());
			return Task.FromResult<Control> (LogWidget);
		}

		protected override void OnDispose ()
		{
			if (widget != null) {
				widget.Destroy ();
				widget = null;
			}
			if (info != null) {
				info.Updated -= OnInfoUpdated;
				info = null;
			}
			base.OnDispose ();
		}

		public void Init ()
		{
			if (info != null && !info.Started) {
				widget.ShowLoading ();
				info.Start ();
			}
		}

		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Copy)]
		protected void OnCopy ()
		{
			string data = widget.GetSelectedText ();
			if (data == null) {
				return;
			}

			CopyToClipboard (data);
		}

		internal static void CopyToClipboard (string data)
		{
			var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = data;
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = data;
		}
	}

}
