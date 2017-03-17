//
// AddinLoadErrorDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using Gtk;
using System.Reflection;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal partial class AddinLoadErrorDialog: IdeDialog
	{
		public AddinLoadErrorDialog (AddinError[] errors, bool warning)
		{
			Build ();
			Title = BrandingService.ApplicationName;
			
			TreeStore store = new TreeStore (typeof(string));
			errorTree.AppendColumn ("Extension", new CellRendererText (), "text", 0);
			errorTree.Model = store;
			
			bool fatal = false;
			
			foreach (AddinError err in errors) {
				string msg = err.Message;
				if (string.IsNullOrEmpty (msg) && err.Exception != null)
					msg = err.Exception.Message;
				string name = System.IO.Path.GetFileNameWithoutExtension (err.AddinFile);
				if (err.Fatal) name += " (Fatal error)";
				TreeIter it = store.AppendValues (name);
				store.AppendValues (it, "Full Path: " + err.AddinFile);
				store.AppendValues (it, "Error: " + msg);
				if (err.Exception != null) {
					it = store.AppendValues (it, "Exception: " + err.Exception.GetType () + ": " + err.Exception.Message);
					store.AppendValues (it, err.Exception.StackTrace.ToString ());
				}
				if (err.Fatal) fatal = true;
			}

			if (fatal) {
				noButton.Hide ();
				yesButton.Hide ();
				closeButton.Show ();
				messageLabel.Text = GettextCatalog.GetString (
					"{0} cannot start because a fatal error has been detected.",
					BrandingService.ApplicationName
				);
			} else if (warning) {
				noButton.Hide ();
				yesButton.Hide ();
				closeButton.Show ();
				messageLabel.Text = GettextCatalog.GetString (
					"{0} can run without these extensions but the functionality they provide will be missing.",
					BrandingService.ApplicationName
				);
			} else {
				messageLabel.Text = GettextCatalog.GetString (
					"You can start {0} without these extensions, but the functionality they " +
					"provide will be missing. Do you wish to continue?",
					BrandingService.ApplicationName
				);
			}
		}
		
		public new bool Run ()
		{
			return MessageService.ShowCustomDialog (this) == (int)ResponseType.Yes;
		}
	}

	
}

