// 
// LogView.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using MonoDevelop.Ide.Gui.Pads;
using Gtk;
using Pango;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Execution;
using System.IO;
using System.Text.RegularExpressions;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui.Components
{
	public class LogView : MonoDevelop.Components.CompactScrolledWindow
	{
		Gtk.TextBuffer buffer;
		Gtk.TextView textEditorControl;
		TextMark endMark;

		TextTag tag;
		TextTag bold;
		TextTag errorTag;
		TextTag consoleLogTag;
		int ident = 0;
		List<TextTag> tags = new List<TextTag> ();
		Stack<string> indents = new Stack<string> ();

		Queue<QueuedUpdate> updates = new Queue<QueuedUpdate> ();
		QueuedTextWrite lastTextWrite;
		GLib.TimeoutHandler outputDispatcher;
		bool outputDispatcherRunning = false;
		
		const int MAX_BUFFER_LENGTH = 4000 * 1024; 

		/// <summary>
		/// The log text view allows the user to jump to the source of an error/warning
		/// by double clicking on the line in the text view.
		/// </summary>
		public class LogTextView : TextView
		{
			public LogTextView (Gtk.TextBuffer buf) : base (buf)
			{
			}

			public LogTextView () 
			{
			}

			static Regex lineRegex = new Regex ("\\b.*\\s(?<file>[/\\\\].*):(line\\s)?(?<line>\\d+)\\s*$", RegexOptions.Compiled);
			protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
			{
				if (evnt.Type == Gdk.EventType.TwoButtonPress) {
					var cursorPos = base.Buffer.GetIterAtOffset (this.Buffer.CursorPosition);

					TextIter iterStart;
					TextIter iterEnd;
					string lineText;
					try {
						iterStart = Buffer.GetIterAtLine (cursorPos.Line);
						iterEnd = Buffer.GetIterAtOffset (iterStart.Offset + iterStart.CharsInLine);

						lineText = Buffer.GetText (iterStart, iterEnd, true);
					} catch (Exception e) {
						LoggingService.LogError ("Error in getting text of the current line.", e);
						return base.OnButtonPressEvent (evnt);
					}

					var match = lineRegex.Match (lineText);
					if (match.Success) {
						string file = match.Groups["file"].Value;
						string line = match.Groups["line"].Value;
						if (!string.IsNullOrEmpty (file) && !string.IsNullOrEmpty (line)) {
							bool fileExists;
							try {
								fileExists = File.Exists (file);
							} catch {
								fileExists = false;
							}
							if (fileExists) {
								int lineNumber;
								try {
									lineNumber = int.Parse (line);
								} catch (Exception) {
									lineNumber = 1;
								}
								IdeApp.Workbench.OpenDocument (file, lineNumber, 1);
							}
						}
					}
				}
				return base.OnButtonPressEvent (evnt);
			}
		}

		public LogView ()
		{
			buffer = new Gtk.TextBuffer (new Gtk.TextTagTable ());
			textEditorControl = new LogTextView (buffer);
			textEditorControl.Editable = false;
			
			ShadowType = ShadowType.None;
			Add (textEditorControl);

			bold = new TextTag ("bold");
			bold.Weight = Pango.Weight.Bold;
			buffer.TagTable.Add (bold);
			
			errorTag = new TextTag ("error");
			errorTag.Foreground = "red";
			errorTag.Weight = Pango.Weight.Bold;
			buffer.TagTable.Add (errorTag);
			
			consoleLogTag = new TextTag ("consoleLog");
			consoleLogTag.Foreground = "darkgrey";
			buffer.TagTable.Add (consoleLogTag);
			
			tag = new TextTag ("0");
			tag.LeftMargin = 10;
			buffer.TagTable.Add (tag);
			tags.Add (tag);
			
			endMark = buffer.CreateMark ("end-mark", buffer.EndIter, false);

			UpdateCustomFont ();
			IdeApp.Preferences.CustomOutputPadFontChanged += HandleCustomFontChanged;
			
			outputDispatcher = new GLib.TimeoutHandler (outputDispatchHandler);
		}

		[CommandHandler (Ide.Commands.EditCommands.Copy)]
		void CopyText ()
		{
			TextIter start;
			TextIter end;
			if (buffer.HasSelection && buffer.GetSelectionBounds (out start, out end)) {
				var text = buffer.GetText (start, end, false);
				var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
				clipboard.Text = text;

				clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
				clipboard.Text = text;
			}
		}
		
		public LogViewProgressMonitor GetProgressMonitor ()
		{
			return new LogViewProgressMonitor (this);
		}

		public void Clear ()
		{
			lock (updates) {
				updates.Clear ();
				lastTextWrite = null;
				outputDispatcherRunning = false;
			}

			buffer.Clear();
		}
		
		void HandleCustomFontChanged (object sender, EventArgs e)
		{
			UpdateCustomFont ();
		}
		
		void UpdateCustomFont ()
		{
			textEditorControl.ModifyFont (IdeApp.Preferences.CustomOutputPadFont ?? FontService.DefaultMonospaceFontDescription);
		}
		
		//mechanism to to batch copy text when large amounts are being dumped
		bool outputDispatchHandler ()
		{
			lock (updates) {
				lastTextWrite = null;
				if (updates.Count == 0) {
					outputDispatcherRunning = false;
					return false;
				} else if (!outputDispatcherRunning) {
					updates.Clear ();
					return false;
				} else {
					while (updates.Count > 0) {
						QueuedUpdate up = updates.Dequeue ();
						up.Execute (this);
					}
				}
			}
			return true;
		}

		void addQueuedUpdate (QueuedUpdate update)
		{
			lock (updates) {
				updates.Enqueue (update);
				if (!outputDispatcherRunning) {
					GLib.Timeout.Add (50, outputDispatcher);
					outputDispatcherRunning = true;
				}
				lastTextWrite = update as QueuedTextWrite;
			}
		}

		protected void UnsafeBeginTask (string name, int totalWork)
		{
			if (name != null && name.Length > 0) {
				Indent ();
				indents.Push (name);
			} else
				indents.Push (null);

			if (name != null) {
				UnsafeAddText (Environment.NewLine + name + Environment.NewLine, bold);
			}
		}
		
		public void BeginTask (string name, int totalWork)
		{
			QueuedBeginTask bt = new QueuedBeginTask (name, totalWork);
			addQueuedUpdate (bt);
		}
		
		public void EndTask ()
		{
			QueuedEndTask et = new QueuedEndTask ();
			addQueuedUpdate (et);
		}
		
		protected void UnsafeEndTask ()
		{
			if (indents.Count > 0 && indents.Pop () != null)
				Unindent ();
		}
		
		public void WriteText (string text)
		{
			//raw text has an extra optimisation here, as we can append it to existing updates
			lock (updates) {
				if (lastTextWrite != null) {
					if (lastTextWrite.Tag == null) {
						lastTextWrite.Write (text);
						return;
					}
				}
			}
			QueuedTextWrite qtw = new QueuedTextWrite (text, null);
			addQueuedUpdate (qtw);
		}
		
		public void WriteConsoleLogText (string text)
		{
			lock (updates) {
				if (lastTextWrite != null && lastTextWrite.Tag == consoleLogTag) {
					lastTextWrite.Write (text);
					return;
				}
			}
			QueuedTextWrite w = new QueuedTextWrite (text, consoleLogTag);
			addQueuedUpdate (w);
		}
		
		public void WriteError (string text)
		{
			QueuedTextWrite w = new QueuedTextWrite (text, errorTag);
			addQueuedUpdate (w);
		}
		
		protected void UnsafeAddText (string text, TextTag extraTag)
		{
			//don't allow the pad to hold more than MAX_BUFFER_LENGTH chars
			int overrun = (buffer.CharCount + text.Length) - MAX_BUFFER_LENGTH;
			if (overrun > 0) {
				TextIter start = buffer.StartIter;
				TextIter end = buffer.GetIterAtOffset (overrun);
				buffer.Delete (ref start, ref end);
			}
			
			TextIter it = buffer.EndIter;
			ScrolledWindow window = textEditorControl.Parent as ScrolledWindow;
			bool scrollToEnd = true;
			if (window != null) {
				scrollToEnd = window.Vadjustment.Value >= window.Vadjustment.Upper - 2 * window.Vadjustment.PageSize;
			}
			if (extraTag != null)
				buffer.InsertWithTags (ref it, text, tag, extraTag);
			else
				buffer.InsertWithTags (ref it, text, tag);
			
			if (scrollToEnd) {
				it.LineOffset = 0;
				buffer.MoveMark (endMark, it);
				textEditorControl.ScrollToMark (endMark, 0, false, 0, 0);
			}
		}
		
		void Indent ()
		{
			ident++;
			if (ident >= tags.Count) {
				tag = new TextTag (ident.ToString ());
				tag.LeftMargin = 10 + 15 * (ident - 1);
				buffer.TagTable.Add (tag);
				tags.Add (tag);
			} else {
				tag = tags [ident];
			}
		}
		
		void Unindent ()
		{
			if (ident >= 0) {
				ident--;
				tag = tags [ident];
			}
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			
			lock (updates) {
				updates.Clear ();
				lastTextWrite = null;
			}
			IdeApp.Preferences.CustomOutputPadFontChanged -= HandleCustomFontChanged;
		}
		
		private abstract class QueuedUpdate
		{
			public abstract void Execute (LogView pad);
		}
		
		private class QueuedTextWrite : QueuedUpdate
		{
			private System.Text.StringBuilder Text;
			public TextTag Tag;
			public override void Execute (LogView pad)
			{
				pad.UnsafeAddText (Text.ToString (), Tag);
			}
			
			public QueuedTextWrite (string text, TextTag tag)
			{
				Text = new System.Text.StringBuilder (text);
				Tag = tag;
			}
			
			public void Write (string s)
			{
				Text.Append (s);
				if (Text.Length > MAX_BUFFER_LENGTH)
					Text.Remove (0, Text.Length - MAX_BUFFER_LENGTH);
			}
		}
		
		private class QueuedBeginTask : QueuedUpdate
		{
			public string Name;
			public int TotalWork;
			public override void Execute (LogView pad)
			{
				pad.UnsafeBeginTask (Name, TotalWork);
			}
			
			public QueuedBeginTask (string name, int totalWork)
			{
				TotalWork = totalWork;
				Name = name;
			}
		}
		
		private class QueuedEndTask : QueuedUpdate
		{
			public override void Execute (LogView pad)
			{
				pad.UnsafeEndTask ();
			}
		}
	}

	public class LogViewProgressMonitor : NullProgressMonitor, IConsole
	{
		LogView outputPad;
		event EventHandler stopRequested;
		
		LogTextWriter logger = new LogTextWriter ();
		LogTextWriter internalLogger = new LogTextWriter ();
		NotSupportedTextReader inputReader = new NotSupportedTextReader ();
		
		public LogView LogView {
			get { return this.outputPad; }
		}
		
		public LogViewProgressMonitor (LogView pad)
		{
			outputPad = pad;
			outputPad.Clear ();
			logger.TextWritten += outputPad.WriteText;
			internalLogger.TextWritten += outputPad.WriteConsoleLogText;
		}
		
		public override void BeginTask (string name, int totalWork)
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.BeginTask (name, totalWork);
			base.BeginTask (name, totalWork);
		}
		
		public override void EndTask ()
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.EndTask ();
			base.EndTask ();
		}
		
		protected override void OnCompleted ()
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.WriteText ("\n");
			
			foreach (string msg in Messages)
				outputPad.WriteText (msg + "\n");
			
			foreach (string msg in Warnings)
				outputPad.WriteText (msg + "\n");
			
			foreach (ProgressError msg in Errors)
				outputPad.WriteError (msg.Message + "\n");
			
			base.OnCompleted ();
			
			outputPad = null;
		}
		
		Exception GetDisposedException ()
		{
			return new InvalidOperationException ("Output progress monitor already disposed.");
		}
		
		protected override void OnCancelRequested ()
		{
			base.OnCancelRequested ();
			if (stopRequested != null)
				stopRequested (this, null);
		}
		
		public override TextWriter Log {
			get { return logger; }
		}
		
		TextWriter IConsole.Log {
			get { return internalLogger; }
		}
		
		TextReader IConsole.In {
			get { return inputReader; }
		}
		
		TextWriter IConsole.Out {
			get { return logger; }
		}
		
		TextWriter IConsole.Error {
			get { return logger; }
		} 
		
		bool IConsole.CloseOnDispose {
			get { return false; }
		}
		
		event EventHandler IConsole.CancelRequested {
			add { stopRequested += value; }
			remove { stopRequested -= value; }
		}
	}

	
	class NotSupportedTextReader: TextReader
	{
		bool userWarned;
		
		void WarnUser ()
		{
			if (userWarned)
				return;
			userWarned = true;
			string title = GettextCatalog.GetString ("Console input not supported");
			string desc = GettextCatalog.GetString (
				"Console input is not supported when using the {0} output console. If your application needs to read " +
				"data from the standard input, please set the 'Run in External Console' option in the project options.",
				BrandingService.ApplicationName
			);
			MessageService.ShowWarning (title, desc);
		}
		
		public override int Peek ()
		{
			WarnUser ();
			return -1;
		}
		
		public override int ReadBlock (char[] buffer, int index, int count)
		{
			WarnUser ();
			return base.ReadBlock(buffer, index, count);
		}
		
		public override int Read (char[] buffer, int index, int count)
		{
			WarnUser ();
			return base.Read(buffer, index, count);
		}
		
		public override int Read ()
		{
			WarnUser ();
			return base.Read();
		}
		
		public override string ReadLine ()
		{
			WarnUser ();
			return base.ReadLine();
		}
		
		public override string ReadToEnd ()
		{
			WarnUser ();
			return base.ReadToEnd();
		}
	}
}

