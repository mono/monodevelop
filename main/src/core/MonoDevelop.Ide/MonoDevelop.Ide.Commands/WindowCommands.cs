// WindowCommands.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
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
//
//


using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using Gtk;
using System.Linq;

namespace MonoDevelop.Ide.Commands
{
	public enum WindowCommands
	{
		NextDocument,
		PrevDocument,
		OpenDocumentList,
		OpenWindowList,
		SplitWindowVertically,
		SplitWindowHorizontally,
		UnsplitWindow,
		SwitchSplitWindow,
		SwitchNextDocument,
		SwitchPreviousDocument
	}

	internal class NextDocumentHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			//if no open window or only one
			if (IdeApp.Workbench.Documents.Count < 2) {
				info.Enabled = false;
			}
		}

		protected override void Run ()
		{
			//Select the number of the next document
			int nextDocumentNumber = IdeApp.Workbench.Documents.IndexOf (IdeApp.Workbench.ActiveDocument) + 1;
			if (nextDocumentNumber >= IdeApp.Workbench.Documents.Count) 
				nextDocumentNumber = 0; 

			//Change window
			IdeApp.Workbench.Documents[nextDocumentNumber].Select ();
		}
	}

	internal class PrevDocumentHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			//if no open window or only one
			if (IdeApp.Workbench.Documents.Count < 2)
				info.Enabled = false;
		}

		protected override void Run ()
		{
			//Select the number of the previous document
			int prevDocumentNumber = IdeApp.Workbench.Documents.IndexOf (IdeApp.Workbench.ActiveDocument) - 1;
			if (prevDocumentNumber < 0) 
				prevDocumentNumber = IdeApp.Workbench.Documents.Count - 1; 

			//Change window
			IdeApp.Workbench.Documents[prevDocumentNumber].Select ();
		}
	}

	internal class OpenDocumentListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			if (IdeApp.Workbench.Documents.Count < 10)
				return;

			int i = 0;
			foreach (Document document in IdeApp.Workbench.Documents) {
				if (i < 9) {
					i++;
					continue;
				}

				//Create CommandInfo object
				CommandInfo commandInfo = new CommandInfo ();
				commandInfo.Text = document.Window.Title.Replace ("_", "__");
				if (document == IdeApp.Workbench.ActiveDocument)
					commandInfo.Checked = true;
				commandInfo.Description = GettextCatalog.GetString ("Activate document '{0}'", commandInfo.Text);
				if (document.Window.ShowNotification) {
					commandInfo.UseMarkup = true;
					commandInfo.Text = "<span foreground=" + '"' + "blue" + '"' + ">" + commandInfo.Text + "</span>";
				}

				//Add AccelKey
				if (IdeApp.Workbench.Documents.Count + i < 10) {
					commandInfo.AccelKey = ((Platform.IsMac) ? "Meta" : "Alt") + "|" + ((i + 1) % 10).ToString ();
				}

				//Add menu item
				info.Add (commandInfo, document);

				i++;
			}
		}

		protected override void Run (object dataItem)
		{
			Document document = (Document)dataItem;
			document.Select ();
		}
	}

	internal class OpenDocumentHandlerBase : CommandHandler
	{
		int index;

		// 1-based index
		protected OpenDocumentHandlerBase (int index)
		{
			this.index = index;
		}

		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.Documents.Count >= index) {
				var document = IdeApp.Workbench.Documents [index - 1];

				info.Text = document.Window.Title.Replace ("_", "__");
				info.Checked = document == IdeApp.Workbench.ActiveDocument;
				info.Description = GettextCatalog.GetString ("Activate document '{0}'", info.Text);
				info.DataItem = document;

				if (document.Window.ShowNotification) {
					info.UseMarkup = true;
					info.Text = "<span foreground=" + '"' + "blue" + '"' + ">" + info.Text + "</span>";
				}
				info.Visible = true;
				info.Enabled = true;
			} else {
				info.Visible = false;
				info.Enabled = false;
			}
		}

		protected override void Run ()
		{
			var document = IdeApp.Workbench.Documents [index - 1];
			document.Select ();
		}
	}

	internal class OpenDocument1 : OpenDocumentHandlerBase
	{
		public OpenDocument1 ()
			: base (1)
		{
		}
	}

	internal class OpenDocument2 : OpenDocumentHandlerBase
	{
		public OpenDocument2 ()
			: base (2)
		{
		}
	}

	internal class OpenDocument3 : OpenDocumentHandlerBase
	{
		public OpenDocument3 ()
			: base (3)
		{
		}
	}

	internal class OpenDocument4 : OpenDocumentHandlerBase
	{
		public OpenDocument4 ()
			: base (4)
		{
		}
	}

	internal class OpenDocument5 : OpenDocumentHandlerBase
	{
		public OpenDocument5 ()
			: base (5)
		{
		}
	}

	internal class OpenDocument6 : OpenDocumentHandlerBase
	{
		public OpenDocument6 ()
			: base (6)
		{
		}
	}

	internal class OpenDocument7 : OpenDocumentHandlerBase
	{
		public OpenDocument7 ()
			: base (7)
		{
		}
	}

	internal class OpenDocument8 : OpenDocumentHandlerBase
	{
		public OpenDocument8 ()
			: base (8)
		{
		}
	}

	internal class OpenDocument9 : OpenDocumentHandlerBase
	{
		public OpenDocument9 ()
			: base (9)
		{
		}
	}

	internal class OpenWindowListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			var windows = IdeApp.CommandService.TopLevelWindowStack.ToArray (); // enumerate only once
			if (windows.Length <= 1)
				return;
			int i = 0;
			foreach (Gtk.Window window in windows) {

				//Create CommandInfo object
				CommandInfo commandInfo = new CommandInfo ();
				commandInfo.Text = window.Title.Replace ("_", "__").Replace("-","\u2013").Replace(" \u2013 " + BrandingService.ApplicationName, "");
				if (window.HasToplevelFocus)
					commandInfo.Checked = true;
				commandInfo.Description = GettextCatalog.GetString ("Activate window '{0}'", commandInfo.Text);

				//Add menu item
				info.Add (commandInfo, window);

				i++;
			}
		}

		protected override void Run (object dataItem)
		{
			Window window = (Window)dataItem;
			window.Present ();
		}
	}

	internal class SplitWindowVertically : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				ISplittable splitt = IdeApp.Workbench.ActiveDocument.GetContent<ISplittable> ();
				if (splitt != null) {
					info.Enabled = splitt.EnableSplitHorizontally;
				} else 
					info.Enabled = false; 
			} else 
				info.Enabled = false; 
		}

		protected override void Run ()
		{
			ISplittable splittVertically = IdeApp.Workbench.ActiveDocument.GetContent<ISplittable> ();
			if (splittVertically != null)
				splittVertically.SplitHorizontally ();
		}
	}

	internal class SplitWindowHorizontally : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				ISplittable splitt = IdeApp.Workbench.ActiveDocument.GetContent<ISplittable> ();
				if (splitt != null) {
					info.Enabled = splitt.EnableSplitVertically;
				} else 
					info.Enabled = false; 
			} else 
				info.Enabled = false;
		}

		protected override void Run ()
		{
			ISplittable splittHorizontally = IdeApp.Workbench.ActiveDocument.GetContent<ISplittable> ();
			if (splittHorizontally != null)
				splittHorizontally.SplitVertically ();
		}
	}

	internal class UnsplitWindow : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				ISplittable splitt = IdeApp.Workbench.ActiveDocument.GetContent<ISplittable> ();
				if (splitt != null) {
					info.Enabled = splitt.EnableUnsplit;
				} else 
					info.Enabled = false; 
			} else 
				info.Enabled = false; 
		}

		protected override void Run ()
		{
			ISplittable splittUnsplitt = IdeApp.Workbench.ActiveDocument.GetContent<ISplittable> ();
			if (splittUnsplitt != null)
				splittUnsplitt.Unsplit ();
		}
	}

	internal class SwitchSplitWindow : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				ISplittable splitt = IdeApp.Workbench.ActiveDocument.GetContent<ISplittable> ();
				if (splitt != null) {
					info.Enabled = splitt.EnableUnsplit;
				} else 
					info.Enabled = false; 
			} else 
				info.Enabled = false; 
		}

		protected override void Run ()
		{
			ISplittable splittUnsplitt = IdeApp.Workbench.ActiveDocument.GetContent<ISplittable> ();
			if (splittUnsplitt != null)
				splittUnsplitt.SwitchWindow ();
		}
	}
	
	internal class SwitchNextDocument : CommandHandler
	{
		protected static void Switch (bool next)
		{
			if (!IdeApp.Preferences.EnableDocumentSwitchDialog) {
				IdeApp.CommandService.DispatchCommand (next? WindowCommands.NextDocument : WindowCommands.PrevDocument);
				return;
			}
			
			var toplevel = Window.ListToplevels ().FirstOrDefault (w => w.HasToplevelFocus)
				?? IdeApp.Workbench.RootWindow;

			bool hasContent;
			var sw = new DocumentSwitcher (toplevel, next, out hasContent);
			if (hasContent) {
				sw.Present ();
			} else {
				sw.Destroy ();
			}
		}

		protected override void Run ()
		{
			Switch (true);
		}
	}
	
	internal class SwitchPreviousDocument : SwitchNextDocument
	{
		protected override void Run ()
		{
			Switch (false);
		}
	}

	internal class SwitchNextPad : CommandHandler
	{
		protected static void Switch (bool next)
		{
			if (!IdeApp.Preferences.EnableDocumentSwitchDialog)
				return;

			var toplevel = Window.ListToplevels ().FirstOrDefault (w => w.HasToplevelFocus)
				?? IdeApp.Workbench.RootWindow;

			bool hasContent;
			var sw = new DocumentSwitcher (toplevel, GettextCatalog.GetString ("Pads"), next, out hasContent);
			if (hasContent) {
				sw.Present ();
			} else {
				sw.Destroy ();
			}
		}

		protected override void Run ()
		{
			Switch (true);
		}
	}

	internal class SwitchPreviousPad : SwitchNextPad
	{
		protected override void Run ()
		{
			Switch (false);
		}
	}
}
