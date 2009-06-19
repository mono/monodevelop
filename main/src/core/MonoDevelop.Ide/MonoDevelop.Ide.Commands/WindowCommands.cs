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


using System;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core.Gui;
using System.IO;
using Gtk;

namespace MonoDevelop.Ide.Commands
{
	public enum WindowCommands
	{
		NextWindow,
		PrevWindow,
		OpenWindowList,
		SplitWindowVertically,
		SplitWindowHorizontally,
		UnsplitWindow,
		SwitchSplitWindow
	}

	internal class NextWindowHandler : CommandHandler
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

	internal class PrevWindowHandler : CommandHandler
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

	internal class OpenWindowListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			int i = 0;
			foreach (Document document in IdeApp.Workbench.Documents) {

				//Create CommandInfo object
				CommandInfo commandInfo = new CommandInfo ();
				commandInfo.Text = document.Window.Title.Replace ("_", "__");
				if (document == IdeApp.Workbench.ActiveDocument) 
					commandInfo.Checked = true; 
				commandInfo.Description = GettextCatalog.GetString ("Activate window '{0}'", commandInfo.Text);
				if (document.Window.ShowNotification) {
					commandInfo.UseMarkup = true;
					document.Window.Title = "<span foreground=" + '"' + "blue" + '"' + ">" + commandInfo.Text + "</span>";
				}

				//Add AccelKey
				if (i < 10) {
					commandInfo.AccelKey = ((PropertyService.IsMac) ? "Meta" : "Alt") + "|" + ((i + 1) % 10).ToString ();
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

}
