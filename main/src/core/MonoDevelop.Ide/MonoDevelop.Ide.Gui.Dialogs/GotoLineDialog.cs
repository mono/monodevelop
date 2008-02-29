// GotoLineDialog.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class GotoLineDialog : Gtk.Dialog
	{
		IEditableTextBuffer positionable;
		
		static GotoLineDialog instance = null;
		public static bool CanShow {
			get {
				return instance != null;
			}
		}
		
		public static void ShowDialog (IEditableTextBuffer positionable)
		{
			if (instance != null)
				return;
			instance = new GotoLineDialog (positionable);
			instance.ShowAll ();
		}
		
		public GotoLineDialog (IEditableTextBuffer positionable)
		{
			this.positionable = positionable;
			this.Build();
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.Destroyed += delegate {
				instance = null;
			};
			
			this.Close += delegate {
				this.Destroy ();
			};
			
			this.buttonClose.Clicked += delegate {
				this.Destroy ();
			};
			
			this.buttonGoToLine.Clicked += delegate {
				try {
					int line = System.Math.Max (1, System.Int32.Parse (entryLineNumber.Text));
					positionable.SetCaretTo (line, 1);
				} catch (System.Exception) { 
				}
				this.Destroy ();
			};
		}
	}
}
