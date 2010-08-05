// 
// PatchView.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Views
{
	internal class PatchView : BaseView, IAttachableViewContent 
	{
		PatchWidget widget;

		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}

		public static void Show (VersionControlItemList items)
		{
			foreach (VersionControlItem item in items) {
				var document = IdeApp.Workbench.OpenDocument (item.Path);
				ComparisonView.AttachViewContents (document, item);
				document.Window.SwitchView (2);
			}
		}

		public static bool CanShow (Repository repo, FilePath file)
		{
			return repo.IsModified (file);
		}

		public PatchView (ComparisonView comparisonView, VersionControlDocumentInfo info) : base ("Diff")
		{
			widget = new PatchWidget (comparisonView, info);
		}

		#region IAttachableViewContent implementation
		public void Selected ()
		{
		}

		public void Deselected ()
		{
		}

		public void BeforeSave ()
		{
		}

		public void BaseContentChanged ()
		{
		}
		#endregion
	}
}

/*
using System;
using System.IO;
using Gtk;

using MonoDevelop.Components.Diff;
using MonoDevelop.Ide;
using Mono.TextEditor;
using Mono.TextEditor.Utils;

namespace MonoDevelop.VersionControl.Views
{
	internal class DiffView : BaseView
	{
		Document baseDocument, changedDocument;

		HBox box = new HBox (true, 0);
		DiffWidget widget;
		ThreadNotify threadnotify;

		System.IO.FileSystemWatcher rightwatcher;

		double pos = -1;

		public static void Show (Repository repo, string path)
		{
			VersionControlItemList list = new VersionControlItemList ();
			list.Add (new VersionControlItem (repo, null, path, Directory.Exists (path)));
			Show (list, false);
		}

		public static bool Show (VersionControlItemList items, bool test)
		{
			bool found = false;
			foreach (VersionControlItem item in items) {
				if (item.Repository.IsModified (item.Path)) {
					if (test)
						return true;
					found = true;
					Document baseDocument = new Document ();
					baseDocument.MimeType = DesktopService.GetMimeTypeForUri (item.Path);
					baseDocument.Text = item.Repository.GetBaseText (item.Path);
					
					DiffView d = new DiffView (Path.GetFileName (item.Path), baseDocument, item.Path);
					IdeApp.Workbench.OpenDocument (d, true);
				}
			}
			return found;
		}

		public static void Show (string name, string mimeType, string lefttext, string righttext)
		{
			Document baseDoc = new Document ();
			Document changedDoc = new Document ();
			baseDoc.MimeType = changedDoc.MimeType = mimeType;
			
			DiffView d = new DiffView (name, baseDoc, changedDoc);
			IdeApp.Workbench.OpenDocument (d, true);
		}
		
		string changedFile;
		public DiffView (string name, Document baseDocument, string changedFile) : base(name + " Changes")
		{
			this.baseDocument = baseDocument;
			this.changedFile = changedFile;
			this.changedDocument = new Document ();
			changedDocument.MimeType = baseDocument.MimeType;
			Refresh ();
			
			threadnotify = new ThreadNotify (new ReadyEvent (Refresh));
			
			rightwatcher = new System.IO.FileSystemWatcher (Path.GetDirectoryName (changedFile), Path.GetFileName (changedFile));
			rightwatcher.Changed += new FileSystemEventHandler (filechanged);
			rightwatcher.EnableRaisingEvents = true;
		}

		public DiffView (string name, Document baseDocument, Document changedDocument) : base(name + " Changes")
		{
			this.baseDocument = baseDocument;
			this.changedDocument = changedDocument;
			
			Refresh ();
		}

		public override void Dispose ()
		{
			if (this.widget != null) {
				this.widget.Destroy ();
				this.widget = null;
			}
			if (rightwatcher != null) {
				rightwatcher.Dispose ();
				rightwatcher = null;
			}
			box.Destroy ();
			base.Dispose ();
		}


		void filechanged (object src, FileSystemEventArgs args)
		{
			threadnotify.WakeupMain ();
		}

		private void Refresh ()
		{
			box.Show ();
			
			if (changedFile != null)
				this.changedDocument.Text = File.ReadAllText (changedFile);
			
			if (widget != null) {
				widget.CreateDiffs (baseDocument, changedDocument);
			} else {
				widget = new DiffWidget ();
				widget.Font = DesktopService.DefaultMonospaceFont;
				widget.LeftName = "Repository";
				widget.RightName = "Working Copy";
				widget.CreateDiffs (baseDocument, changedDocument);
				
				box.Add (widget);
				box.ShowAll ();
				
				widget.ExposeEvent += new ExposeEventHandler (OnExposed);
			}
		}

		void OnExposed (object o, ExposeEventArgs args)
		{
			if (pos != -1)
				widget.Position = pos;
			pos = -1;
		}

		protected override void SaveAs (string fileName)
		{
			using (StreamWriter writer = new StreamWriter (fileName)) {
				string name = changedDocument.FileName;
				writer.Write (Diff.GetDiffString (widget.Diff, baseDocument, changedDocument, 
					Path.GetFileName (name) + "    (repository)", 
					Path.GetFileName (name) + "    (working copy)"));
			}
		}

		public override Gtk.Widget Control {
			get { return box; }
		}
	}
}
 * */