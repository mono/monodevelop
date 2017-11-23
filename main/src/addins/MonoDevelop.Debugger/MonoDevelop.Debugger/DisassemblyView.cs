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
using Mono.Debugging.Client;
using MonoDevelop.Ide.Editor;
using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide;
using System.Security.Cryptography;
using Gdk;
using MonoDevelop.Components;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Debugger
{
	public class DisassemblyView: ViewContent, IClipboardHandler
	{
		Gtk.ScrolledWindow sw;
		TextEditor editor;
		int firstLine;
		int lastLine;
		Dictionary<string,int> addressLines = new Dictionary<string,int> ();
		bool autoRefill;
		ICurrentDebugLineTextMarker currentDebugLineMarker;
		bool dragging;
		FilePath currentFile;
		ITextLineMarker asmMarker;
		DebuggerSession session;
		
		List<AssemblyLine> cachedLines = new List<AssemblyLine> ();
		string cachedLinesAddrSpace;
		
		const int FillMarginLines = 50;
		
		public DisassemblyView ()
		{
			ContentName = GettextCatalog.GetString ("Disassembly");
			sw = new Gtk.ScrolledWindow ();
			editor = TextEditorFactory.CreateNewEditor ();
			editor.IsReadOnly = true;
			asmMarker = TextMarkerFactory.CreateAsmLineMarker (editor);

			editor.Options = DefaultSourceEditorOptions.PlainEditor;
			
			sw.AddWithViewport (editor);
			sw.HscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.VscrollbarPolicy = Gtk.PolicyType.Automatic;
			sw.ShowAll ();
			sw.Vadjustment.ValueChanged += OnScrollEditor;
			sw.VScrollbar.ButtonPressEvent += OnPress;
			sw.VScrollbar.ButtonReleaseEvent += OnRelease;
			sw.VScrollbar.Events |= Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask;
			sw.ShadowType = Gtk.ShadowType.In;
			
			sw.Sensitive = false;

			DebuggingService.StoppedEvent += OnStop;
		}

		public override string TabPageLabel {
			get {
				return GettextCatalog.GetString ("Disassembly");
			}
		}
		
		public override Control Control {
			get {
				return sw;
			}
		}

		public override bool IsFile {
			get {
				return false;
			}
		}

		public void Update ()
		{
			autoRefill = false;
			if (currentDebugLineMarker != null) {
				editor.RemoveMarker (currentDebugLineMarker);
				currentDebugLineMarker = null;
			}
			
			if (DebuggingService.CurrentFrame == null) {
				sw.Sensitive = false;
				return;
			}
			
			sw.Sensitive = true;
			var sf = DebuggingService.CurrentFrame;
			if (!string.IsNullOrEmpty (sf.SourceLocation.FileName) && File.Exists (sf.SourceLocation.FileName))
				FillWithSource ();
			else
				Fill ();
		}
		
		public void FillWithSource ()
		{
			cachedLines.Clear ();
			
			StackFrame sf = DebuggingService.CurrentFrame;
			session = sf.DebuggerSession;
			if (currentFile != sf.SourceLocation.FileName) {
				AssemblyLine[] asmLines = sf.DebuggerSession.DisassembleFile (sf.SourceLocation.FileName);
				if (asmLines == null) {
					// Mixed disassemble not supported
					Fill ();
					return;
				}
				currentFile = sf.SourceLocation.FileName;
				addressLines.Clear ();
				editor.Text = string.Empty;
				using (var sr = new StreamReader (sf.SourceLocation.FileName)) {
					string line;
					int sourceLine = 1;
					int na = 0;
					int editorLine = 1;
					var sb = new StringBuilder ();
					var asmLineNums = new List<int> ();
					while ((line = sr.ReadLine ()) != null) {
						InsertSourceLine (sb, editorLine++, line);
						while (na < asmLines.Length && asmLines [na].SourceLine == sourceLine) {
							asmLineNums.Add (editorLine);
							InsertAssemblerLine (sb, editorLine++, asmLines [na++]);
						}
						sourceLine++;
					}
					editor.Text = sb.ToString ();
					foreach (int li in asmLineNums)
						editor.AddMarker (li, asmMarker);
				}
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
			
			editor.MimeType = "text/plain";
			editor.Text = string.Empty;
			InsertLines (0, firstLine, lastLine, out firstLine, out lastLine);
			
			autoRefill = true;
			
			UpdateCurrentLineMarker (true);
		}
		
		void UpdateCurrentLineMarker (bool moveCaret)
		{
			if (currentDebugLineMarker != null) {
				editor.RemoveMarker (currentDebugLineMarker);
				currentDebugLineMarker = null;
			}
			StackFrame sf = DebuggingService.CurrentFrame;
			int line;
			if (addressLines.TryGetValue (GetAddrId (sf.Address, sf.AddressSpace), out line)) {
				var docLine = editor.GetLine (line);
				currentDebugLineMarker = TextMarkerFactory.CreateCurrentDebugLineTextMarker (editor, docLine.Offset, docLine.Length);
				editor.AddMarker (line, currentDebugLineMarker);
				if (moveCaret) {
					editor.CaretLine = line;
					GLib.Timeout.Add (100, delegate {
						editor.CenterToCaret ();
						return false;
					});
				}
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
			
			var loc = editor.PointToLocation (0, 0);
			Gtk.Widget widget = editor;
			var loc2 = editor.PointToLocation (0, widget.Allocation.Height);
			//bool moveCaret = editor.Caret.Line >= loc.Line && editor.Caret.Line <= loc2.Line;
			
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
					editor.CaretLine += num;
				
				double hinc = num * editor.LineHeight;
				sw.Vadjustment.Value += hinc;
				
				UpdateCurrentLineMarker (false);
			}
			if (lastLine != int.MinValue && loc2.Line >= editor.LineCount - FillMarginLines) {
				int num = (loc2.Line - (editor.LineCount - FillMarginLines) + 1) * 2;
				int newFirst;
				InsertLines (editor.Length, lastLine + 1, lastLine + num, out newFirst, out lastLine);
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
			int editorLine = editor.OffsetToLineNumber (offset);
			foreach (AssemblyLine li in lines) {
				if (li.IsOutOfRange)
					continue;
				InsertAssemblerLine (sb, editorLine++, li);
				lineCount++;
			}
			editor.IsReadOnly = false;
			editor.InsertText (offset, sb.ToString ());
			editor.IsReadOnly = true;
			if (offset == 0)
				this.cachedLines.InsertRange (0, lines);
			else
				this.cachedLines.AddRange (lines);
			return lineCount;
		}

		void OnStop (object s, EventArgs args)
		{
			if (session != s)
				return;
			addressLines.Clear ();
			currentFile = null;
			sw.Sensitive = false;
			autoRefill = false;
			editor.Text = string.Empty;
			cachedLines.Clear ();
			session = null;
		}
		
		public override bool IsReadOnly {
			get { return true; }
		}

		
		public override void Dispose ()
		{
			base.Dispose ();
			DebuggingService.StoppedEvent -= OnStop;
			session = null;
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
			editor.EditorActionHost.ClipboardCopy ();
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
			editor.EditorActionHost.SelectAll ();
		}

		bool IClipboardHandler.EnableCut {
			get { return false; }
		}

		bool IClipboardHandler.EnableCopy {
			get { return !editor.IsSomethingSelected; }
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
}
