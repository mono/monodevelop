// 
// PinnedWatchWidget.cs
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
using System.Linq;
using Mono.TextEditor;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Tasks;
using System.Collections.Generic;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor
{
	public class PinnedWatchWidget : Gtk.EventBox
	{
		ObjectValueTreeView valueTree;
		ObjectValue objectValue;

		TextEditor Editor {
			get; set;
		}
		
		public PinnedWatch Watch {
			get; private set;
		}
		
		public ObjectValue ObjectValue {
			get {
				return objectValue;
			}
			set {
				if (objectValue != null && value != null) {
					valueTree.ReplaceValue (objectValue, value);
				} else {
					valueTree.ClearValues ();
					if (value != null)
						valueTree.AddValue (value);
				}

				objectValue = value;
			}
		}
		
		public PinnedWatchWidget (TextEditor editor, PinnedWatch watch)
		{
			objectValue = watch.Value;
			Editor = editor;
			Watch = watch;

			valueTree = new ObjectValueTreeView ();
			valueTree.AllowAdding = false;
			valueTree.AllowEditing = true;
			valueTree.AllowPinning = true;
			valueTree.HeadersVisible = false;
			valueTree.CompactView = true;
			valueTree.PinnedWatch = watch;
			if (objectValue != null)
				valueTree.AddValue (objectValue);
			
			valueTree.ButtonPressEvent += HandleValueTreeButtonPressEvent;
			valueTree.ButtonReleaseEvent += HandleValueTreeButtonReleaseEvent;
			valueTree.MotionNotifyEvent += HandleValueTreeMotionNotifyEvent;
			
			Gtk.Frame fr = new Gtk.Frame ();
			fr.ShadowType = Gtk.ShadowType.Out;
			fr.Add (valueTree);
			Add (fr);
			HandleEditorOptionsChanged (null, null);
			ShowAll ();
			//unpin.Hide ();
			Editor.EditorOptionsChanged += HandleEditorOptionsChanged;
			
			DebuggingService.PausedEvent += HandleDebuggingServicePausedEvent;
			DebuggingService.ResumedEvent += HandleDebuggingServiceResumedEvent;
		}

		void HandleDebuggingServiceResumedEvent (object sender, EventArgs e)
		{
			valueTree.ChangeCheckpoint ();
			valueTree.AllowEditing = false;
			valueTree.AllowExpanding = false;
		}

		void HandleDebuggingServicePausedEvent (object sender, EventArgs e)
		{
			valueTree.AllowExpanding = true;
			valueTree.AllowEditing = true;
		}

		void HandleEditorOptionsChanged (object sender, EventArgs e)
		{
//			HeightRequest = Math.Max (Math.Max (Editor.LineHeight, image.HeightRequest), 18);
			/*
			Pango.FontDescription fontDescription = Pango.FontDescription.FromString (Editor.Options.FontName);
			fontDescription.Family = "Sans";
			fontDescription.Size = (int)(fontDescription.Size * Editor.Options.Zoom);
			label.ModifyFont (fontDescription);
			valueLabel.ModifyFont (fontDescription);*/
			
//			label.ModifyFont (Editor.Options.Font);
//			valueLabel.ModifyFont (Editor.Options.Font);
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			Editor.EditorOptionsChanged -= HandleEditorOptionsChanged;
			DebuggingService.PausedEvent -= HandleDebuggingServicePausedEvent;
			DebuggingService.ResumedEvent -= HandleDebuggingServiceResumedEvent;
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
		}
		

		[GLib.ConnectBeforeAttribute]
		void HandleValueTreeButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			originX = args.Event.XRoot;
			originY = args.Event.YRoot;
			
			Gtk.TreePath path;
			Gtk.TreeViewColumn col;
			int cx, cy;
			valueTree.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path, out col, out cx, out cy);
			Gdk.Rectangle rect = valueTree.GetCellArea (path, col);
			if (!mousePressed && valueTree.Columns[0] == col && cx >= rect.Left) {
				mousePressed = true;
				Editor.MoveToTop (this);
//				Gdk.Pointer.Grab (this.GdkWindow, true, Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask, null, null, Gtk.Global.CurrentEventTime);
//				Gtk.Grab.Add (this);
			}
		}
		
		[GLib.ConnectBeforeAttribute]
		void HandleValueTreeButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (mousePressed) {
//				Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
//				Gtk.Grab.Remove (this);
				mousePressed = false;
			}
		}
		

		[GLib.ConnectBeforeAttribute]
		void HandleValueTreeMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			if (mousePressed) {
				Watch.OffsetX += (int)(args.Event.XRoot - originX);
				Watch.OffsetY += (int)(args.Event.YRoot - originY);
				
				originX = args.Event.XRoot;
				originY = args.Event.YRoot;
			}
			
		}
		
		bool mousePressed = false;
		double originX, originY;
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			bool result = base.OnEnterNotifyEvent (evnt);
/*			this.unpin.ShowAll ();
			QueueResize ();*/
			return result;
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			bool result = base.OnLeaveNotifyEvent (evnt);
			/*
			this.unpin.HideAll ();
			QueueResize ();*/
			
			return result;
		}
	}
}