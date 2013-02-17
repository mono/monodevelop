// DisassemblyView.cs
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
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using TextEditor = Mono.TextEditor.TextEditor;
using Mono.TextEditor;
using Mono.Debugging.Client;
using Mono.TextEditor.Highlighting;

namespace MonoDevelop.Debugger
{
	public class DisassemblyView: AbstractViewContent, IClipboardHandler
	{
		Gtk.ScrolledWindow sw;
		Mono.TextEditor.TextEditor editor;
		int firstLine;
		int lastLine;
		Dictionary<string,int> addressLines = new Dictionary<string,int> ();
		bool autoRefill;
		CurrentDebugLineTextMarker currentDebugLineMarker;
		bool dragging;
		FilePath currentFile;
		AsmLineMarker asmMarker = new AsmLineMarker ();
		
		List<AssemblyLine> cachedLines = new List<AssemblyLine> ();
		string cachedLinesAddrSpace;
		
		const int FillMarginLines = 50;
		
		public DisassemblyView ()
		{
			UntitledName = GettextCatalog.GetString ("Disassembly");
			sw = new Gtk.ScrolledWindow ();
			editor = new Mono.TextEditor.TextEditor ();
			editor.Document.ReadOnly = true;
			
			editor.Options = new MonoDevelop.Ide.Gui.CommonTextEditorOptions () {
				ShowLineNumberMargin = false,
			};
			
			sw.Add (editor);
			sw.HscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.ShowAll ();
			sw.Vadjustment.ValueChanged += OnScrollEditor;
			sw.VScrollbar.ButtonPressEvent += OnPress;
			sw.VScrollbar.ButtonReleaseEvent += OnRelease;
			sw.VScrollbar.Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask;
			sw.ShadowType = Gtk.ShadowType.In;
			
			sw.Sensitive = false;
			
			currentDebugLineMarker = new CurrentDebugLineTextMarker (editor);
			DebuggingService.StoppedEvent += OnStop;
		}
		
		public override string TabPageLabel {
			get {
				return GettextCatalog.GetString ("Disassembly");
			}
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
			
			editor.Document.RemoveMarker (currentDebugLineMarker);
			
			if (DebuggingService.CurrentFrame == null) {
				sw.Sensitive = false;
				return;
			}
			
			sw.Sensitive = true;
			
			StackFrame sf = DebuggingService.CurrentFrame;
			if (!string.IsNullOrEmpty (sf.SourceLocation.FileName) && File.Exists (sf.SourceLocation.FileName))
				FillWithSource ();
			else
				Fill ();
		}
		
		public void FillWithSource ()
		{
			cachedLines.Clear ();
			
			StackFrame sf = DebuggingService.CurrentFrame;
			
			if (currentFile != sf.SourceLocation.FileName) {
				AssemblyLine[] asmLines = DebuggingService.DebuggerSession.DisassembleFile (sf.SourceLocation.FileName);
				if (asmLines == null) {
					// Mixed disassemble not supported
					Fill ();
					return;
				}
				currentFile = sf.SourceLocation.FileName;
				addressLines.Clear ();
				editor.Document.Text = string.Empty;
				StreamReader sr = new StreamReader (sf.SourceLocation.FileName);
				string line;
				int sourceLine = 1;
				int na = 0;
				int editorLine = 1;
				StringBuilder sb = new StringBuilder ();
				List<int> asmLineNums = new List<int> ();
				while ((line = sr.ReadLine ()) != null) {
					InsertSourceLine (sb, editorLine++, line);
					while (na < asmLines.Length && asmLines [na].SourceLine == sourceLine) {
						asmLineNums.Add (editorLine);
						InsertAssemblerLine (sb, editorLine++, asmLines [na++]);
					}
					sourceLine++;
				}
				editor.Document.Text = sb.ToString ();
				foreach (int li in asmLineNums)
					editor.Document.AddMarker (li, asmMarker);
			}
			int aline;
			if (!addressLines.TryGetValue (GetAddrId (sf.Address, sf.AddressSpace), out aline))
				return;
			UpdateCurrentLineMarker (true);
		}
		
		string GetAddrId (long addr, string addrSpace)
		{
			return addrSpace + " " + addr;
		}
		
		void InsertSourceLine (StringBuilder sb, int line, string text)
		{
			sb.Append (text).Append ('\n');
		}
		
		void InsertAssemblerLine (StringBuilder sb, int line, AssemblyLine asm)
		{
			sb.AppendFormat ("{0:x8}   {1}\n", asm.Address, asm.Code);
			addressLines [GetAddrId (asm.Address, asm.AddressSpace)] = line;
		}

		public void Fill ()
		{
			currentFile = null;
			StackFrame sf = DebuggingService.CurrentFrame;
			if (cachedLines.Count > 0 && cachedLinesAddrSpace == sf.AddressSpace) {
				if (sf.Address >= cachedLines [0].Address && sf.Address <= cachedLines [cachedLines.Count - 1].Address) {
					// The same address range can be reused
					autoRefill = true;
					UpdateCurrentLineMarker (true);
					return;
				}
			}
			
			// New address view
			
			cachedLinesAddrSpace = sf.AddressSpace;
			cachedLines.Clear ();
			addressLines.Clear ();
			
			firstLine = -150;
			lastLine = 150;
			
			editor.Document.MimeType = "text/plain";
			editor.Document.Text = string.Empty;
			InsertLines (0, firstLine, lastLine, out firstLine, out lastLine);
			
			autoRefill = true;
			
			UpdateCurrentLineMarker (true);
		}
		
		void UpdateCurrentLineMarker (bool moveCaret)
		{
			editor.Document.RemoveMarker (currentDebugLineMarker);
			StackFrame sf = DebuggingService.CurrentFrame;
			int line;
			if (addressLines.TryGetValue (GetAddrId (sf.Address, sf.AddressSpace), out line)) {
				editor.Document.AddMarker (line, currentDebugLineMarker);
				if (moveCaret) {
					editor.Caret.Line = line;
					GLib.Timeout.Add (100, delegate {
						editor.CenterToCaret ();
						return false;
					});
				}
				editor.QueueDraw ();
			}
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
			
			DocumentLocation loc = editor.PointToLocation (0, 0);
			DocumentLocation loc2 = editor.PointToLocation (0, editor.Allocation.Height);
			bool moveCaret = editor.Caret.Line >= loc.Line && editor.Caret.Line <= loc2.Line;
			
			if (firstLine != int.MinValue && loc.Line < FillMarginLines) {
				int num = (FillMarginLines - loc.Line) * 2;
				int newLast;
				num = InsertLines (0, firstLine - num, firstLine - 1, out firstLine, out newLast);
				
				// Shift line numbers in the addresses dictionary
				var newLines = new Dictionary<string, int> ();
				foreach (var pair in addressLines)
					newLines [pair.Key] = pair.Value + num;
				addressLines = newLines;
				
				//if (moveCaret)
					editor.Caret.Line += num;
				
				double hinc = num * editor.LineHeight;
				sw.Vadjustment.Value += hinc;
				
				UpdateCurrentLineMarker (false);
			}
			if (lastLine != int.MinValue && loc2.Line >= editor.Document.LineCount - FillMarginLines) {
				int num = (loc2.Line - (editor.Document.LineCount - FillMarginLines) + 1) * 2;
				int newFirst;
				InsertLines (editor.Document.TextLength, lastLine + 1, lastLine + num, out newFirst, out lastLine);
			}
		}
		
		int InsertLines (int offset, int start, int end, out int newStart, out int newEnd)
		{
			StringBuilder sb = new StringBuilder ();
			StackFrame ff = DebuggingService.CurrentFrame;
			List<AssemblyLine> lines = new List<AssemblyLine> (ff.Disassemble (start, end - start + 1));
			
			int i = lines.FindIndex (al => !al.IsOutOfRange);
			if (i == -1) {
				newStart = int.MinValue;
				newEnd = int.MinValue;
				return 0;
			}
			
			newStart = i == 0 ? start : int.MinValue;
			lines.RemoveRange (0, i);
			
			int j = lines.FindLastIndex (al => !al.IsOutOfRange);
			newEnd = j == lines.Count - 1 ? end : int.MinValue;
			lines.RemoveRange (j + 1, lines.Count - j - 1);
			
			int lineCount = 0;
			int editorLine = editor.GetTextEditorData ().OffsetToLineNumber (offset);
			foreach (AssemblyLine li in lines) {
				if (li.IsOutOfRange)
					continue;
				InsertAssemblerLine (sb, editorLine++, li);
				lineCount++;
			}
			editor.Insert (offset, sb.ToString ());
			editor.Document.CommitUpdateAll ();
			if (offset == 0)
				this.cachedLines.InsertRange (0, lines);
			else
				this.cachedLines.AddRange (lines);
			return lineCount;
		}
		
		void OnStop (object s, EventArgs args)
		{
			addressLines.Clear ();
			currentFile = null;
			sw.Sensitive = false;
			autoRefill = false;
			editor.Document.Text = string.Empty;
			cachedLines.Clear ();
		}
		
		public override bool IsReadOnly {
			get { return true; }
		}

		
		public override void Dispose ()
		{
			base.Dispose ();
			DebuggingService.StoppedEvent -= OnStop;
		}
		
		[CommandHandler (DebugCommands.StepOver)]
		protected void OnStepOver ()
		{
			DebuggingService.DebuggerSession.NextInstruction ();
		}
		
		[CommandHandler (DebugCommands.StepInto)]
		protected void OnStepInto ()
		{
			DebuggingService.DebuggerSession.StepInstruction ();
		}
		
		[CommandUpdateHandler (DebugCommands.StepOver)]
		[CommandUpdateHandler (DebugCommands.StepInto)]
		protected void OnUpdateStep (CommandInfo ci)
		{
			var cf = DebuggingService.CurrentFrame;
			ci.Enabled =  cf != null && addressLines.ContainsKey (GetAddrId (cf.Address, cf.AddressSpace));
		}

		#region IClipboardHandler implementation

		void IClipboardHandler.Cut ()
		{
			throw new NotSupportedException ();
		}

		void IClipboardHandler.Copy ()
		{
			editor.RunAction (ClipboardActions.Copy);
		}

		void IClipboardHandler.Paste ()
		{
			throw new NotSupportedException ();
		}

		void IClipboardHandler.Delete ()
		{
			throw new NotSupportedException ();
		}

		void IClipboardHandler.SelectAll ()
		{
			editor.RunAction (SelectionActions.SelectAll);
		}

		bool IClipboardHandler.EnableCut {
			get { return false; }
		}

		bool IClipboardHandler.EnableCopy {
			get { return !editor.SelectionRange.IsEmpty; }
		}

		bool IClipboardHandler.EnablePaste {
			get { return false; }
		}

		bool IClipboardHandler.EnableDelete {
			get { return false; }
		}

		bool IClipboardHandler.EnableSelectAll {
			get { return true; }
		}

		#endregion
	}
	
	class AsmLineMarker: TextLineMarker
	{
		public override ChunkStyle GetStyle (ChunkStyle baseStyle)
		{
			ChunkStyle st = new ChunkStyle (baseStyle);
			st.Foreground = new Cairo.Color (125, 125, 125);
			return st;
		}
	}
}
