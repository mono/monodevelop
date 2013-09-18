//
// GitSelectRevisionDialog.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Therzok
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
using System.Text;
using MonoDevelop.VersionControl.Views;

namespace MonoDevelop.VersionControl.Git
{
	class GitSelectRevisionDialog : Xwt.Dialog
	{
		readonly Xwt.TextEntry tagNameEntry;
		readonly Xwt.TextEntry tagMessageEntry;

		readonly Xwt.ListView revisionList;

		readonly Xwt.ListStore revisionStore;
		readonly Xwt.DataField<string> messageField;
		readonly Xwt.ListViewColumn messageColumn;
		readonly Xwt.DataField<string> dateField;
		readonly Xwt.ListViewColumn dateColumn;
		readonly Xwt.DataField<string> authorField;
		readonly Xwt.ListViewColumn authorColumn;
		readonly Xwt.DataField<string> shaField;
		readonly Xwt.ListViewColumn shaColumn;
		readonly Xwt.DataField<Revision> revisionField;
		readonly Xwt.DialogButton buttonOk;

		public GitSelectRevisionDialog (GitRepository repo)
		{
			Title = GettextCatalog.GetString ("Select a revision");

			Xwt.VBox vbox = new Xwt.VBox ();
			vbox.MinWidth = 350;
			vbox.Spacing = 6;
			vbox.Margin = 12;

			vbox.PackStart(new Xwt.Label (GettextCatalog.GetString ("Tag Name")));

			tagNameEntry = new Xwt.TextEntry ();
			tagNameEntry.Changed += delegate {
				CheckSensitive ();
			};
			vbox.PackStart (tagNameEntry);

			vbox.PackStart (new Xwt.Label (GettextCatalog.GetString ("Tag Message")));

			tagMessageEntry = new Xwt.TextEntry ();
			vbox.PackStart (tagMessageEntry);

			revisionList = new Xwt.ListView ();
			messageField = new Xwt.DataField<string> ();
			dateField = new Xwt.DataField<string> ();
			authorField = new Xwt.DataField<string> ();
			shaField = new Xwt.DataField<string> ();
			revisionField = new Xwt.DataField<Revision> ();

			revisionStore = new Xwt.ListStore (messageField, dateField, authorField, shaField, revisionField);
			revisionList.DataSource = revisionStore;

			var message = new Xwt.TextCellView (messageField);
			message.Ellipsize = Xwt.EllipsizeMode.End;
			messageColumn = new Xwt.ListViewColumn (GettextCatalog.GetString ("Message"), message);
			revisionList.Columns.Add (messageColumn);

			dateColumn = new Xwt.ListViewColumn (GettextCatalog.GetString ("Date"), new Xwt.TextCellView (dateField));
			revisionList.Columns.Add (dateColumn);
			authorColumn = new Xwt.ListViewColumn (GettextCatalog.GetString ("Author"), new Xwt.TextCellView (authorField));
			revisionList.Columns.Add (authorColumn);
			shaColumn = new Xwt.ListViewColumn (GettextCatalog.GetString ("Revision"), new Xwt.TextCellView (shaField));
			revisionList.Columns.Add (shaColumn);

			var history = repo.GetHistory (repo.RootPath, null);
			for (int i = 0; i < history.Length; ++i) {
				var rev = history [i];

				revisionStore.AddRow ();
				// Temporary fix until we have ellipsize.
				revisionStore.SetValue (i, messageField, rev.ShortMessage.Length < 40 ? rev.ShortMessage : rev.ShortMessage.Substring (0, 39));
				revisionStore.SetValue (i, dateField, ParseDate (rev.Time));
				revisionStore.SetValue (i, authorField, rev.Author);
				revisionStore.SetValue (i, shaField, ((GitRevision)rev).ShortName);
				revisionStore.SetValue (i, revisionField, rev);
			}

			revisionList.SelectionChanged += delegate {
				CheckSensitive ();
			};

			vbox.PackStart (revisionList, true, true);

			Content = vbox;

			buttonOk = new Xwt.DialogButton (Xwt.Command.Ok) {
				Sensitive = false
			};
			Buttons.Add (buttonOk);
			Buttons.Add (new Xwt.DialogButton (Xwt.Command.Cancel));
		}

		void CheckSensitive ()
		{
			if (!String.IsNullOrWhiteSpace (tagNameEntry.Text) && GitUtil.IsValidBranchName (tagNameEntry.Text) &&
				revisionList.SelectedRow != -1) {
				buttonOk.Sensitive = true;
				return;
			}
			buttonOk.Sensitive = false;
		}

		static string ParseDate (DateTime date)
		{
			StringBuilder sb = new StringBuilder (date.ToShortDateString ());
			sb.AppendFormat (" {0}", date.ToString ("HH:MM"));

			return sb.ToString ();
		}

		internal string TagName {
			get { return tagNameEntry.Text; }
		}

		internal string TagMessage {
			get { return tagMessageEntry.Text; }
		}

		internal Revision SelectedRevision {
			get { return revisionStore.GetValue (revisionList.SelectedRow, revisionField); }
		}
	}
}

