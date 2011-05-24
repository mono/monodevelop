// 
// QuickFixWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide;
using Mono.TextEditor;
using MonoDevelop.Components.Commands;
using System.Collections.Generic;

namespace MonoDevelop.QuickFix
{
	public class QuickFixWidget : Gtk.EventBox
	{
		TextEditor editor;
		List<QuickFix> fixes;
		
		public QuickFixWidget (TextEditor editor, List<QuickFix> fixes)
		{
			this.editor = editor;
			this.fixes = fixes;
			Events = Gdk.EventMask.AllEventsMask;
			Gtk.Image image = new Gtk.Image ();
			image.Pixbuf = ImageService.GetPixbuf ("md-text-editor-behavior", Gtk.IconSize.Menu);
			Add (image);
			this.SetSizeRequest (image.Pixbuf.Width, (int)editor.LineHeight);
			ShowAll ();
		}
		
		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				Gtk.Menu menu = new Gtk.Menu ();
				
				foreach (QuickFix fix in fixes) {
					Gtk.MenuItem menuItem = new Gtk.MenuItem (fix.MenuText);
					menuItem.Activated += delegate {
						fix.Run ();
					};
					menu.Add (menuItem);
				}
				menu.ShowAll ();
				int dx, dy;
				this.ParentWindow.GetOrigin (out dx, out dy);
				dx += ((TextEditorContainer.EditorContainerChild)(this.editor.Parent as TextEditorContainer) [this]).X;
				dy += ((TextEditorContainer.EditorContainerChild)(this.editor.Parent as TextEditorContainer) [this]).Y;
					
				menu.Popup (null, null, delegate (Gtk.Menu menu2, out int x, out int y, out bool pushIn) {
					x = dx; 
					y = dy + Allocation.Height; 
					pushIn = false;
				}, 0, Gtk.Global.CurrentEventTime);
				menu.SelectFirst (true);
			}
			return base.OnButtonPressEvent (evnt);
		}
		
	}
}

