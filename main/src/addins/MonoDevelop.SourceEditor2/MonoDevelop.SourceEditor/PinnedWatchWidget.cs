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
using Mono.TextEditor;
using MonoDevelop.Debugger;
using Mono.Debugging.Client;
using Gtk;

namespace MonoDevelop.SourceEditor
{
	class PinnedWatchWidget : EventBox
	{
		readonly ObjectValueTreeView valueTree;
		ScrolledWindow sw;
		ObjectValue objectValue;

		MonoTextEditor Editor {
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
				if (objectValue == value)
					return;

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
		
		public PinnedWatchWidget (MonoTextEditor editor, PinnedWatch watch)
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
			valueTree.SizeAllocated += OnTreeSizeChanged;

			sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = PolicyType.Never;
			sw.VscrollbarPolicy = PolicyType.Never;
			sw.Add (valueTree);

			Frame fr = new Frame ();
			fr.ShadowType = ShadowType.Out;
			fr.Add (sw);
			Add (fr);

			ShowAll ();
			
			DebuggingService.PausedEvent += HandleDebuggingServicePausedEvent;
			DebuggingService.ResumedEvent += HandleDebuggingServiceResumedEvent;
		}

		void OnTreeSizeChanged (object s, SizeAllocatedArgs a)
		{
			const int maxHeight = 240;
			var treeHeight = valueTree.SizeRequest ().Height;
			if (treeHeight > maxHeight && sw.VscrollbarPolicy == PolicyType.Never) {
				sw.VscrollbarPolicy = PolicyType.Always;
				sw.HeightRequest = maxHeight;
			} else if (maxHeight > treeHeight && sw.VscrollbarPolicy == PolicyType.Always) {
				sw.VscrollbarPolicy = PolicyType.Never;
				sw.HeightRequest = -1;
			}

			QueueDraw ();
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
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			DebuggingService.PausedEvent -= HandleDebuggingServicePausedEvent;
			DebuggingService.ResumedEvent -= HandleDebuggingServiceResumedEvent;

			valueTree.ButtonPressEvent -= HandleValueTreeButtonPressEvent;
			valueTree.ButtonReleaseEvent -= HandleValueTreeButtonReleaseEvent;
			valueTree.MotionNotifyEvent -= HandleValueTreeMotionNotifyEvent;
			valueTree.SizeAllocated -= OnTreeSizeChanged;
		}


		#region Moving PinWatchWidget inside TextArea
		bool mousePressed = false;
		double originX, originY;

		[GLib.ConnectBefore]
		void HandleValueTreeButtonPressEvent (object o, ButtonPressEventArgs args)
		{
			originX = args.Event.XRoot;
			originY = args.Event.YRoot;
			
			TreePath path;
			TreeViewColumn col;
			int cx, cy;
			valueTree.GetPathAtPos ((int)args.Event.X, (int)args.Event.Y, out path, out col, out cx, out cy);
			//Gdk.Rectangle rect = valueTree.GetCellArea (path, col);
			if (!mousePressed && valueTree.Columns[0] == col) {
				mousePressed = true;
				Editor.TextArea.MoveToTop (this);
			}
		}
		
		[GLib.ConnectBefore]
		void HandleValueTreeButtonReleaseEvent (object o, ButtonReleaseEventArgs args)
		{
			if (mousePressed) {
				mousePressed = false;
			}
		}
		

		[GLib.ConnectBefore]
		void HandleValueTreeMotionNotifyEvent (object o, MotionNotifyEventArgs args)
		{
			if (mousePressed) {
				Watch.OffsetX += (int)(args.Event.XRoot - originX);
				Watch.OffsetY += (int)(args.Event.YRoot - originY);
				
				originX = args.Event.XRoot;
				originY = args.Event.YRoot;
			}
			
		}
		#endregion
	}
}