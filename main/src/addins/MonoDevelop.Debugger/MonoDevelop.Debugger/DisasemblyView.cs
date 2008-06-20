// DisasemblyView.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
//
//

using System;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using TextEditor = Mono.TextEditor.TextEditor;
using Mono.TextEditor;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public class DisasemblyView: AbstractViewContent
	{
		Gtk.ScrolledWindow sw;
		TextEditor editor;
		int firstLine;
		int lastLine;
		List<AssemblyLine> lines = new List<AssemblyLine> ();
		bool autoRefill;
		int lastDebugLine = -1;
		LineBackgroundMarker currentDebugLineMarker = new LineBackgroundMarker (new Gdk.Color (255, 255, 0));
		bool dragging;
		
		const int FillMarginLines = 50;
		
		public DisasemblyView ()
		{
			UntitledName = GettextCatalog.GetString ("Disassembly");
			sw = new Gtk.ScrolledWindow ();
			editor = new TextEditor ();
			editor.ReadOnly = true;
			TextEditorOptions options = new TextEditorOptions ();
			options.CopyFrom (TextEditorOptions.Options);
			options.ShowEolMarkers = false;
			options.ShowFoldMargin = false;
//			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowLineNumberMargin = false;
			editor.Options = options;
			
			sw.Add (editor);
			sw.HscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.ShowAll ();
			sw.Vadjustment.ValueChanged += OnScrollEditor;
			sw.VScrollbar.ButtonPressEvent += OnPress;
			sw.VScrollbar.ButtonReleaseEvent += OnRelease;
			sw.VScrollbar.Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask;
			
			sw.Sensitive = false;
		}
		
		public override Gtk.Widget Control {
			get {
				return sw;
			}
		}
		
		public override void Load (string fileName)
		{
		}
		
		public void Update ()
		{
			autoRefill = false;
			
			if (lastDebugLine != -1)
				editor.Document.GetLine (lastDebugLine).RemoveMarker (currentDebugLineMarker);
			
			if (IdeApp.Services.DebuggingService.CurrentFrame == null) {
				sw.Sensitive = false;
				lastDebugLine = -1;
				return;
			}
			
			sw.Sensitive = true;
			StackFrame sf = IdeApp.Services.DebuggingService.CurrentFrame;
			if (lines.Count > 0) {
				if (sf.Address >= lines [0].Address && sf.Address <= lines [lines.Count - 1].Address) {
					// The same address range can be reused
					autoRefill = true;
					for (int n=0; n<lines.Count; n++) {
						if (lines [n].Address == sf.Address) {
							lastDebugLine = n;
							editor.Caret.Line = n;
							editor.Document.GetLine (lastDebugLine).AddMarker (currentDebugLineMarker);
							editor.QueueDraw ();
							return;
						}
					}
				}
			}
			
			// New address view
			
			lines.Clear ();
			firstLine = -150;
			lastLine = 150;
			
			editor.Document.MimeType = "text/plain";
			InsertLines (0, firstLine, lastLine);
			
			autoRefill = true;
			lastDebugLine = lastLine;
			editor.Caret.Line = 150;
			
			editor.Document.GetLine (lastDebugLine).AddMarker (currentDebugLineMarker);
			editor.QueueDraw ();
		}
		
		[GLib.ConnectBefore]
		void OnPress (object s, EventArgs a)
		{
			dragging = true;
		}
		
		[GLib.ConnectBefore]
		void OnRelease (object s, EventArgs a)
		{
			dragging = false;
			OnScrollEditor (null, null);
		}
		
		void OnScrollEditor (object s, EventArgs args)
		{
			if (!autoRefill || dragging)
				return;
			
			DocumentLocation loc = editor.VisualToDocumentLocation (0, 0);
			DocumentLocation loc2 = editor.VisualToDocumentLocation (0, editor.Allocation.Height);
			bool moveCaret = editor.Caret.Line >= loc.Line && editor.Caret.Line <= loc2.Line;
			
			if (loc.Line < FillMarginLines) {
				if (lastDebugLine != -1)
					editor.Document.GetLine (lastDebugLine).RemoveMarker (currentDebugLineMarker);
				int num = (FillMarginLines - loc.Line) * 2;
				InsertLines (0, firstLine - num, firstLine - 1);
				firstLine -= num;
				
				if (moveCaret)
					editor.Caret.Line += num;
				
				int hinc = num * editor.LineHeight;
				sw.Vadjustment.Value += (double) hinc;
				
				if (lastDebugLine != -1) {
					lastDebugLine += num;
					editor.Document.GetLine (lastDebugLine).AddMarker (currentDebugLineMarker);
					editor.QueueDraw ();
				}
			}
			if (loc2.Line >= editor.Document.LineCount - FillMarginLines) {
				int num = (loc2.Line - (editor.Document.LineCount - FillMarginLines) + 1) * 2;
				InsertLines (editor.Document.Length, lastLine + 1, lastLine + num);
				lastLine += num;
			}
		}
		
		void InsertLines (int offset, int start, int end)
		{
			StringBuilder sb = new StringBuilder ();
			StackFrame ff = IdeApp.Services.DebuggingService.CurrentFrame;
			AssemblyLine[] lines = ff.Disassemble (start, end - start + 1);
			foreach (AssemblyLine li in lines) {
				sb.AppendFormat ("0x{0:x}   {1}\n", li.Address, li.Code);
			}
			editor.Document.Insert (offset, sb.ToString ());
			editor.Document.CommitUpdateAll ();
			if (offset == 0)
				this.lines.InsertRange (0, lines);
			else
				this.lines.AddRange (lines);
		}

		
		public override bool IsReadOnly {
			get { return true; }
		}

		
		public override void Dispose ()
		{
			base.Dispose ();
			sw.Destroy ();
		}
		
		[CommandHandler (DebugCommands.StepOver)]
		protected void OnStepOver ()
		{
			IdeApp.Services.DebuggingService.DebuggerSession.NextInstruction ();
		}
		
		[CommandHandler (DebugCommands.StepInto)]
		protected void OnStepInto ()
		{
			IdeApp.Services.DebuggingService.DebuggerSession.StepInstruction ();
		}
		
		[CommandUpdateHandler (DebugCommands.StepOver)]
		[CommandUpdateHandler (DebugCommands.StepInto)]
		protected void OnUpdateStep (CommandInfo ci)
		{
			ci.Enabled = lastDebugLine != -1;
		}
	}
}
