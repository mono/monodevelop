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
using LibGit2Sharp;
using System.Threading;
using System.Threading.Tasks;

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
		CancellationTokenSource cts = new CancellationTokenSource ();

		public GitSelectRevisionDialog (GitRepository repo)
		{
			Title = GettextCatalog.GetString ("Select a revision");

			var vbox = new Xwt.VBox ();
			vbox.MinHeight = 400;
			vbox.MinWidth = 800;

			vbox.PackStart(new Xwt.Label (GettextCatalog.GetString ("Tag Name")));

			tagNameEntry = new Xwt.TextEntry { Name = "tagNameEntry" };
			tagNameEntry.Changed += delegate {
				CheckSensitive ();
			};
			vbox.PackStart (tagNameEntry);

			vbox.PackStart (new Xwt.Label (GettextCatalog.GetString ("Tag Message")));

			tagMessageEntry = new Xwt.TextEntry { Name = "tagMessageEntry" };
			vbox.PackStart (tagMessageEntry);

			revisionList = new Xwt.ListView ();
			messageField = new Xwt.DataField<string> ();
			dateField = new Xwt.DataField<string> ();
			authorField = new Xwt.DataField<string> ();
			shaField = new Xwt.DataField<string> ();
			revisionField = new Xwt.DataField<Revision> ();

			revisionStore = new Xwt.ListStore (messageField, dateField, authorField, shaField, revisionField);
			revisionList.DataSource = revisionStore;

			messageColumn = new Xwt.ListViewColumn (GettextCatalog.GetString ("Message"), new Xwt.TextCellView (messageField)) {
				CanResize = true,
				Alignment = Xwt.Alignment.Center,
			};

			revisionList.Columns.Add (messageColumn);
			dateColumn = new Xwt.ListViewColumn (GettextCatalog.GetString ("Date"), new Xwt.TextCellView (dateField)) {
				CanResize = true,
				Alignment = Xwt.Alignment.Center,
			};
			revisionList.Columns.Add (dateColumn);
			authorColumn = new Xwt.ListViewColumn (GettextCatalog.GetString ("Author"), new Xwt.TextCellView (authorField)) {
				CanResize = true,
				Alignment = Xwt.Alignment.Center,
			};
			revisionList.Columns.Add (authorColumn);
			shaColumn = new Xwt.ListViewColumn (GettextCatalog.GetString ("Revision"), new Xwt.TextCellView (shaField)) {
				CanResize = true,
				Alignment = Xwt.Alignment.Center,
			};
			revisionList.Columns.Add (shaColumn);

			Task.Factory.StartNew (async () => {
				const int sliceSize = 150;

				var history = repo.GetHistory (repo.RootPath, null);

				int slices = history.Length / sliceSize;
				for (int i = 0; i < slices; ++i) {
					await Runtime.RunInMainThread (() => {
						for (int n = 0; n < sliceSize; ++n) {
							if (cts.IsCancellationRequested)
								return;

							int row = revisionStore.AddRow ();
							var rev = history [row];

							revisionStore.SetValue (row, messageField, rev.ShortMessage);
							revisionStore.SetValue (row, dateField, ParseDate (rev.Time));
							revisionStore.SetValue (row, authorField, rev.Author);
							revisionStore.SetValue (row, shaField, ((GitRevision)rev).ShortName);
							revisionStore.SetValue (row, revisionField, rev);
						}
					});
				}
			}, cts.Token);

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

		protected override void Dispose (bool disposing)
		{
			if (cts != null) {
				cts.Cancel ();
				cts.Dispose ();
				cts = null;
			}
			base.Dispose (disposing);
		}

		void CheckSensitive ()
		{
			if (!String.IsNullOrWhiteSpace (tagNameEntry.Text) && Reference.IsValidName ("refs/tags/" + tagNameEntry.Text) &&
				revisionList.SelectedRow != -1) {
				buttonOk.Sensitive = true;
				return;
			}
			buttonOk.Sensitive = false;
		}

		static string ParseDate (DateTime date)
		{
			var sb = new StringBuilder (date.ToShortDateString ());
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

