// 
// CodeSegmentEditorWindow.cs
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
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.Editor.Highlighting;
namespace Mono.TextEditor
{
	class CodeSegmentEditorWindow : Gtk.Window
	{
		MonoTextEditor codeSegmentEditor = new MonoTextEditor ();
		
		internal ISyntaxHighlighting SyntaxMode {
			get {
				return codeSegmentEditor.Document.SyntaxMode;
			}
			set {
				codeSegmentEditor.Document.SyntaxMode = value;
			}
		}
		
		public string Text {
			get {
				return codeSegmentEditor.Document.Text;
			}
			set {
				codeSegmentEditor.Document.Text = value;
			}
		}
		
		public CodeSegmentEditorWindow (MonoTextEditor editor) : base (Gtk.WindowType.Toplevel)
		{
			Gtk.ScrolledWindow scrolledWindow = new Gtk.ScrolledWindow ();
			scrolledWindow.Child = codeSegmentEditor;
			scrolledWindow.ShadowType = Gtk.ShadowType.In;
			Child = scrolledWindow;
			codeSegmentEditor.Realize ();
			((SimpleEditMode)codeSegmentEditor.CurrentMode).AddBinding (Gdk.Key.Escape, Close, true);
			TextEditorOptions options = new TextEditorOptions (true);
			options.FontName = editor.Options.FontName;
			options.EditorThemeName = editor.Options.EditorThemeName;
			options.ShowRuler =  false;
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.Zoom = 0.8;
			codeSegmentEditor.Document.MimeType = editor.MimeType;
			codeSegmentEditor.Document.IsReadOnly = true;
			codeSegmentEditor.Options = options;
			
			codeSegmentEditor.KeyPressEvent += delegate(object o, Gtk.KeyPressEventArgs args) {
				if (args.Event.Key == Gdk.Key.Escape)
					Destroy ();
				
			};
			Gtk.Widget parent = editor.Parent;
			while (parent != null && !(parent is Gtk.Window))
				parent = parent.Parent;
			if (parent is Gtk.Window)
				this.TransientFor = (Gtk.Window)parent;
			this.SkipTaskbarHint = true;
			this.Decorated = false;
			Gdk.Pointer.Grab (this.GdkWindow, true, Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask, null, null, Gtk.Global.CurrentEventTime);
			Gtk.Grab.Add (this);
			GrabBrokenEvent += delegate {
				Destroy ();
			};
			codeSegmentEditor.GrabFocus ();
		}
		
		public void Close (TextEditorData data)
		{
			Destroy ();
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			Destroy ();
			return base.OnFocusOutEvent (evnt);
		}

		protected override void OnDestroyed ()
		{
			Gtk.Grab.Remove (this);
			Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
			base.OnDestroyed ();
		}
	}
}

