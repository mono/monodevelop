//
// TextEditorDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.ComponentModel;

namespace MonoDevelop.Components.PropertyGrid.PropertyEditors
{
	public class TextEditorDialog: IDisposable
	{
		Gtk.TextView textview;
		Gtk.Dialog dialog;
		
		public TextEditorDialog ()
		{
			Gtk.ScrolledWindow sc = new Gtk.ScrolledWindow ();
			sc.HscrollbarPolicy = Gtk.PolicyType.Automatic;
			sc.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			sc.ShadowType = Gtk.ShadowType.In;
			sc.BorderWidth = 6;
			
			textview = new Gtk.TextView ();
			sc.Add (textview);
			
			dialog = new Gtk.Dialog ();
			dialog.AddButton (Gtk.Stock.Cancel, Gtk.ResponseType.Cancel);
			dialog.AddButton (Gtk.Stock.Ok, Gtk.ResponseType.Ok);
			dialog.VBox.Add (sc);
		}

		public Gtk.Window TransientFor {
			set { dialog.TransientFor = value; }
		}
		
		public string Text {
			get { return textview.Buffer.Text; }
			set { textview.Buffer.Text = value; }
		}
		
		public int Run ()
		{
			dialog.DefaultWidth = 500;
			dialog.DefaultHeight = 400;
			dialog.ShowAll ();
			return MonoDevelop.Ide.MessageService.ShowCustomDialog (dialog, dialog.TransientFor);
		}
		
		public void Dispose ()
		{
			dialog.Dispose ();
		}
	}
}
