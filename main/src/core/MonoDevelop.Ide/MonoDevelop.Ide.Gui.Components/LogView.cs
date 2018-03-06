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
using MonoDevelop.Ide.Commands;
using System.Linq;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Components
{
	public class LogView : Gtk.VBox
	{
		TextBuffer buffer;
		TextView textEditorControl;
		TextMark endMark;

		TextTag tag;
		TextTag bold;
		TextTag errorTag;
		TextTag consoleLogTag;
		TextTag debugTag;
		int ident = 0;
		List<TextTag> tags = new List<TextTag> ();
		Stack<string> indents = new Stack<string> ();

		readonly Queue<QueuedUpdate> updates = new Queue<QueuedUpdate> ();
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
			readonly CommandEntrySet menuSet;

			public LogTextView (TextBuffer buf) : base (buf)
			{
				menuSet = new CommandEntrySet ();
				SetupMenu ();
			}

			public LogTextView () 
			{
				menuSet = new CommandEntrySet ();
				SetupMenu ();
			}

			void SetupMenu ()
			{
				menuSet.AddItem (EditCommands.Copy);
				menuSet.AddItem (EditCommands.Cut);
				menuSet.AddItem (EditCommands.Paste);
				menuSet.AddItem (EditCommands.SelectAll);
			}

			[CommandHandler (EditCommands.SelectAll)]
			void SelectAllText ()
			{
				TextIter start;
				TextIter end;

				Buffer.GetBounds (out start, out end);
				Buffer.SelectRange (start, end);
			}

			[CommandHandler (EditCommands.Copy)]
			void CopyText ()
			{
				TextIter start;
				TextIter end;

				if (Buffer.HasSelection && Buffer.GetSelectionBounds (out start, out end)) {
					var text = Buffer.GetText (start, end, false);
					var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
					clipboard.Text = text;

					if (Platform.IsLinux) {
						// gtk has different clipboards for CLIPBOARD and PRIMARY only on Linux.
						clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
						clipboard.Text = text;
					}
				}
			}

			[CommandHandler (EditCommands.Cut)]
			void CutText ()
			{
				if (!Buffer.HasSelection)
					return;
				if (Editable) {
					var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
					Buffer.CutClipboard (clipboard, false);
				} else {
					CopyText ();
				}
			}

			[CommandHandler (EditCommands.Paste)]
			void PasteText ()
			{
				if (Editable) {
					var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
					Buffer.PasteClipboard (clipboard);
				}
			}

			internal static bool TryExtractFileAndLine (string lineText, out string file, out int line)
			{
				file = null;
				line = 0;

				int i = lineText.Length - 1, mult = 1;
				int fileNameEnd = 0, fileNameStart = 0;
				int state = 0;
				while (i > 0) {
					char ch = lineText [i];
					switch (state) {
					case 0: // search line number start
						if (ch == '.')
							state = 1;
						else if (ch >= '0' && ch <= '9') {
							state = 1;
							goto case 1;
						} else if (!char.IsWhiteSpace (ch))
							return false;
						break;
					case 1: // read line number
						if (ch >= '0' && ch <= '9') {
							line += (ch - '0') * mult;
							mult *= 10;
						} else if (ch == ' ' || char.IsLetter (ch)) {
							state = 2;
						} else if (ch == ':') {
							state = 3;
							fileNameStart = fileNameEnd = i;
						} else
							return false;
						break;
					case 2: // search file name end
						if (ch == ':') {
							state = 3;
							fileNameStart = fileNameEnd = i;
						} else if (ch != ' ' && !char.IsLetter (ch))
							return false;
						break;
					case 3: // search file name start
						if (ch == ' ') {
							if (lineText [i + 1] == '/' ||  // unix style start
							    (char.IsLetter (lineText [i + 1]) && lineText [i + 2] == ':' && lineText [i + 3] == '\\') // windows style start
							   )
								fileNameStart = i + 1;
						}
						break;
					}

					i--;
				}
				if (state == 3 && fileNameEnd - fileNameStart > 0) {
					file = lineText.Substring (fileNameStart, fileNameEnd - fileNameStart);
					return true;
				}
				return false;
			}

			protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
			{
				if (evnt.Type == Gdk.EventType.ButtonPress && evnt.Button == 3) {
					IdeApp.CommandService.ShowContextMenu (this, evnt, menuSet, this);

					return false;
				} else if (evnt.Type == Gdk.EventType.TwoButtonPress) {
					var cursorPos = Buffer.GetIterAtOffset (Buffer.CursorPosition);
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
					string file;
					int lineNumber;

					if (TryExtractFileAndLine (lineText, out file, out lineNumber)) {
						if (!string.IsNullOrEmpty (file)) {
							bool fileExists;
							try {
								fileExists = File.Exists (file);
							} catch {
								fileExists = false;
							}
							if (fileExists)
								IdeApp.Workbench.OpenDocument (file, null, lineNumber, 1);
						}
					}
				}
				return base.OnButtonPressEvent (evnt);
			}
		}
		HBox searchBox = new HBox ();
		MonoDevelop.Components.SearchEntry searchEntry = new MonoDevelop.Components.SearchEntry ();
		MonoDevelop.Components.CompactScrolledWindow scrollView = new MonoDevelop.Components.CompactScrolledWindow ();

		public LogView ()
		{
			buffer = new TextBuffer (new TextTagTable ());
			textEditorControl = new LogTextView (buffer);
			textEditorControl.Editable = false;
			
			scrollView.ShadowType = ShadowType.None;
			scrollView.Add (textEditorControl);
			PackEnd (scrollView, true, true, 0);

			bold = new TextTag ("bold");
			bold.Weight = Weight.Bold;
			buffer.TagTable.Add (bold);
			
			errorTag = new TextTag ("error");
			errorTag.Foreground = Styles.ErrorForegroundColor.ToHexString (false);
			errorTag.Weight = Weight.Bold;
			buffer.TagTable.Add (errorTag);

			debugTag = new TextTag ("debug");
			debugTag.Foreground = Styles.InformationForegroundColor.ToHexString (false);
			buffer.TagTable.Add (debugTag);

			consoleLogTag = new TextTag ("consoleLog");
			consoleLogTag.Foreground = Styles.SecondaryTextColor.ToHexString (false);
			buffer.TagTable.Add (consoleLogTag);
			
			tag = new TextTag ("0");
			tag.LeftMargin = 10;
			buffer.TagTable.Add (tag);
			tags.Add (tag);
			
			endMark = buffer.CreateMark ("end-mark", buffer.EndIter, false);

			UpdateCustomFont ();
			IdeApp.Preferences.CustomOutputPadFont.Changed += HandleCustomFontChanged;
			
			outputDispatcher = new GLib.TimeoutHandler (outputDispatchHandler);

			InitSearchWidget ();
		}

		#region Searching
		Button buttonSearchForward;
		Button buttonSearchBackward;
		Button closeButton;

		static string currentSearchPattern;

		void InitSearchWidget ()
		{
			searchBox.BorderWidth = 4;
			searchBox.PackStart (new Label (GettextCatalog.GetString ("Search:")), false, false, 4);
			searchBox.PackStart (searchEntry, true, true, 0);
			closeButton = new Button ();
			closeButton.CanFocus = true;
			closeButton.Relief = ReliefStyle.None;
			closeButton.Image = new ImageView ("gtk-close", IconSize.Menu);
			closeButton.Clicked += delegate {
				HideSearchBox ();
			};
			searchBox.PackEnd (closeButton, false, false, 0);

			buttonSearchForward = new Button ();
			buttonSearchForward.CanFocus = true;
			buttonSearchForward.Relief = ReliefStyle.None;
			buttonSearchForward.TooltipText = GettextCatalog.GetString ("Find next {0}", GetShortcut (SearchCommands.FindNext));
			buttonSearchForward.Image = new ImageView ("gtk-go-down", IconSize.Menu);
			buttonSearchForward.Clicked += delegate {
				FindNext ();
			};
			searchBox.PackEnd (buttonSearchForward, false, false, 0);

			buttonSearchBackward = new Button ();
			buttonSearchBackward.CanFocus = true;
			buttonSearchBackward.Relief = ReliefStyle.None;
			buttonSearchBackward.TooltipText = GettextCatalog.GetString ("Find previous {0}", GetShortcut (SearchCommands.FindPrevious));
			buttonSearchBackward.Image = new ImageView ("gtk-go-up", IconSize.Menu);
			buttonSearchBackward.Clicked += delegate {
				FindPrev ();
			};
			searchBox.PackEnd (buttonSearchBackward, false, false, 0);

			searchEntry.Ready = true;
			searchEntry.Visible = true;
			searchEntry.Entry.KeyPressEvent += delegate (object o, KeyPressEventArgs args) {
				switch (args.Event.Key) {
				case Gdk.Key.Escape:
					HideSearchBox ();
					break;
				}
			};

			textEditorControl.KeyPressEvent += delegate (object o, KeyPressEventArgs args) {
				switch (args.Event.Key) {
				case Gdk.Key.Escape:
					HideSearchBox ();
					break;
				}
			};
			searchEntry.Entry.Changed += delegate {
				currentSearchPattern = searchEntry.Entry.Text;
			};
			searchEntry.Entry.Activated += delegate {
				FindNext ();
			};
		}

		void UpdateSearchEntrySearchPattern ()
		{
			searchEntry.Entry.Text = currentSearchPattern ?? "";
		}

		void ShowSearchBox ()
		{
			UpdateSearchEntrySearchPattern ();
			PackStart (searchBox, false, true, 0);
			searchBox.ShowAll ();
			searchEntry.Entry.GrabFocus ();
		}

		void HideSearchBox ()
		{
			Remove (searchBox);
			textEditorControl.GrabFocus ();
		}

		[CommandHandler (SearchCommands.Find)]
		void Find ()
		{
			ShowSearchBox ();
		}

		static StringComparison GetComparer ()
		{
			if (PropertyService.Get ("AutoSetPatternCasing", true)) {
				if (currentSearchPattern != null && currentSearchPattern.Any (Char.IsUpper))
					return StringComparison.Ordinal;
			}

			return StringComparison.OrdinalIgnoreCase;
		}

		[CommandHandler (SearchCommands.FindNext)]
		void FindNext ()
		{
			if (string.IsNullOrEmpty (currentSearchPattern))
				return;
			int searchPosition = buffer.CursorPosition;
			TextIter start;
			TextIter end;

			if (buffer.HasSelection && buffer.GetSelectionBounds (out start, out end))
				searchPosition = end.Offset;

			var comparer = GetComparer ();
			var text = buffer.Text;
			var idx = text.IndexOf (currentSearchPattern, searchPosition, comparer);
			if (idx < 0) {
				idx = text.IndexOf (currentSearchPattern, 0, searchPosition, comparer);
			}

			if (idx >= 0) {
				var iter = buffer.GetIterAtOffset (idx + currentSearchPattern.Length);
				buffer.PlaceCursor (iter);
				buffer.SelectRange (buffer.GetIterAtOffset (idx), iter); 
				textEditorControl.ScrollToIter (iter, 0, false, 0, 0);
			}
		}

		[CommandHandler (SearchCommands.FindNextSelection)]
		void FindNextSelection ()
		{
			SetSearchPatternToSelection ();
			UpdateSearchEntrySearchPattern ();
			FindNext ();
		}

		[CommandHandler (SearchCommands.FindPrevious)]
		void FindPrev ()
		{
			if (string.IsNullOrEmpty (currentSearchPattern))
				return;
			int searchPosition = buffer.CursorPosition;
			TextIter start;
			TextIter end;

			if (buffer.HasSelection && buffer.GetSelectionBounds (out start, out end))
				searchPosition = start.Offset;

			var comparer = GetComparer ();
			var text = buffer.Text;
			var idx = text.LastIndexOf (currentSearchPattern, searchPosition, comparer);
			if (idx < 0) {
				idx = text.LastIndexOf (currentSearchPattern, text.Length, text.Length - searchPosition, comparer);
			}

			if (idx >= 0) {
				var iter = buffer.GetIterAtOffset (idx + currentSearchPattern.Length);
				buffer.PlaceCursor (iter);
				buffer.SelectRange (buffer.GetIterAtOffset (idx), iter); 
				textEditorControl.ScrollToIter (iter, 0, false, 0, 0);
			}

		}

		[CommandHandler (SearchCommands.FindPreviousSelection)]
		void FindPreviousSelection ()
		{
			SetSearchPatternToSelection ();
			UpdateSearchEntrySearchPattern ();
			FindPrev ();
		}

		void SetSearchPatternToSelection ()
		{
			TextIter start;
			TextIter end;

			if (buffer.HasSelection && buffer.GetSelectionBounds (out start, out end))
				currentSearchPattern = buffer.GetText (start, end, false);
		}

		static string GetShortcut (object commandId)
		{
			var key = IdeApp.CommandService.GetCommand (commandId).AccelKey;
			if (string.IsNullOrEmpty (key))
				return "";
			var nextShortcut = KeyBindingManager.BindingToDisplayLabel (key, false);
			return "(" + nextShortcut + ")";
		}
		#endregion

		public OutputProgressMonitor GetProgressMonitor ()
		{
			return GetProgressMonitor (clearConsole: true);
		}

		internal OutputProgressMonitor GetProgressMonitor (bool clearConsole)
		{
			return new LogViewProgressMonitor (this, clearConsole);
		}

		internal TextMark CreateMark ()
		{
			return buffer.CreateMark (null, buffer.EndIter, false);
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
			textEditorControl.ModifyFont (IdeApp.Preferences.CustomOutputPadFont ?? FontService.MonospaceFont);
		}
		
		//mechanism to to batch copy text when large amounts are being dumped
		bool outputDispatchHandler ()
		{
			lock (updates) {
				lastTextWrite = null;
				if (updates.Count == 0) {
					outputDispatcherRunning = false;
					return false;
				}

				if (!outputDispatcherRunning) {
					updates.Clear ();
					return false;
				}

				while (updates.Count > 0) {
					var up = updates.Dequeue ();
					up.Execute (this);
				}
			}
			return true;
		}

		void addQueuedUpdate (QueuedUpdate update)
		{
			lock (updates) {
				if (destroyed)
					return;
				
				updates.Enqueue (update);
				if (!outputDispatcherRunning) {
					GLib.Timeout.Add (50, outputDispatcher);
					outputDispatcherRunning = true;
				}
				lastTextWrite = update as QueuedTextWrite;
			}
		}

		protected void UnsafeBeginTask (LogViewProgressMonitor monitor, string name, int totalWork)
		{
			if (!string.IsNullOrEmpty (name) && monitor != null) {
				var indent = monitor.Indent;
				var t = indent.Indent ();
				buffer.TagTable.Add (t);
				indents.Push (name);
			} else
				indents.Push (null);

			if (name != null)
				UnsafeAddText (null, Environment.NewLine + name + Environment.NewLine, bold);
			else {
				UnsafeAddText (null, Environment.NewLine, null);
			}

			var marker = buffer.CreateMark (name, buffer.EndIter, false);
			if (monitor != null) {
				monitor.Marker = marker;
			}
			UnsafeAddText (null, Environment.NewLine, null);

			// Move the mark to the line before EndIter, so other text inserted at EndIter
			// doesn't move this mark.
			buffer.MoveMark (marker, buffer.GetIterAtOffset (buffer.CharCount - 1));
		}

		public void BeginTask (string name, int totalWork)
		{
			BeginTask (null, name, totalWork);
		}

		internal void BeginTask (LogViewProgressMonitor monitor, string name, int totalWork)
		{
			var bt = new QueuedBeginTask (monitor, name, totalWork);
			addQueuedUpdate (bt);
		}

		public void EndTask ()
		{
			EndTask (null);
		}

		internal void EndTask (LogViewProgressMonitor monitor)
		{
			var et = new QueuedEndTask (monitor);
			addQueuedUpdate (et);
		}
		
		protected void UnsafeEndTask (LogViewProgressMonitor monitor)
		{
			if (indents.Count > 0 && indents.Pop () != null) {
				if (monitor != null) {
					buffer.TagTable.Remove (monitor.Indent.IndentTag);
					monitor.Indent.Unindent ();
				}
			}
		}

		public void WriteText (string text)
		{
			WriteText (null, text);
		}

		public void WriteText (LogViewProgressMonitor monitor, string text)
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

			var qtw = new QueuedTextWrite (monitor, text, null);
			addQueuedUpdate (qtw);
		}

		public void WriteConsoleLogText (string text)
		{
			WriteConsoleLogText (null, text);
		}

		public void WriteConsoleLogText (LogViewProgressMonitor monitor, string text)
		{
			lock (updates) {
				if (lastTextWrite != null && lastTextWrite.Tag == consoleLogTag) {
					lastTextWrite.Write (text);
					return;
				}
			}

			var w = new QueuedTextWrite (monitor, text, consoleLogTag);
			addQueuedUpdate (w);
		}

		public void WriteError (string text)
		{
			WriteError (null, text);
		}

		public void WriteError (LogViewProgressMonitor monitor, string text)
		{
			var w = new QueuedTextWrite (monitor, text, errorTag);
			addQueuedUpdate (w);
		}

		public void WriteDebug (int level, string category, string message)
		{
			WriteDebug (null, level, category, message);
		}

		public void WriteDebug (LogViewProgressMonitor monitor, int level, string category, string message)
		{
			//TODO: Give user ability to filter levels and categories
			if (string.IsNullOrEmpty (category))
				addQueuedUpdate (new QueuedTextWrite (monitor, message, debugTag));
			else
				addQueuedUpdate (new QueuedTextWrite (monitor, category + ": " + message, debugTag));
		}

		bool ShouldAutoScroll ()
		{
			if (scrollView == null || scrollView.Vadjustment == null)
				return false;

			// we need to account for the page size as well for some reason
			return scrollView.Vadjustment.Value + scrollView.Vadjustment.PageSize >= scrollView.Vadjustment.Upper;
		}

		protected void UnsafeAddText (TextMark mark, string text, TextTag extraTag)
		{
			//don't allow the pad to hold more than MAX_BUFFER_LENGTH chars
			int overrun = (buffer.CharCount + text.Length) - MAX_BUFFER_LENGTH;

			if (overrun > 0) {
				TextIter start = buffer.StartIter;
				TextIter end = buffer.GetIterAtOffset (overrun);
				buffer.Delete (ref start, ref end);
			}

			bool scrollToEnd = ShouldAutoScroll ();

			TextIter it;
			if (mark != null) {
				it = buffer.GetIterAtMark (mark);
			} else {
				it = buffer.EndIter;
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

		bool destroyed = false;
		protected override void OnDestroyed ()
		{
			lock (updates) {
				destroyed = true;
				updates.Clear ();
				lastTextWrite = null;
			}
			IdeApp.Preferences.CustomOutputPadFont.Changed -= HandleCustomFontChanged;

			base.OnDestroyed ();
		}
		
		abstract class QueuedUpdate
		{
			public LogViewProgressMonitor Monitor { get; protected set; }
			public abstract void Execute (LogView pad);
		}
		
		class QueuedTextWrite : QueuedUpdate
		{
			readonly System.Text.StringBuilder Text;
			public TextTag Tag;

			public override void Execute (LogView pad)
			{
				pad.UnsafeAddText (Monitor?.Marker, Text.ToString (), Tag);
			}
			
			public QueuedTextWrite (LogViewProgressMonitor monitor, string text, TextTag tag)
			{
				Monitor = monitor;
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
		
		class QueuedBeginTask : QueuedUpdate
		{
			public string Name;
			public int TotalWork;

			public override void Execute (LogView pad)
			{
				pad.UnsafeBeginTask (Monitor, Name, TotalWork);
			}

			public QueuedBeginTask (LogViewProgressMonitor monitor, string name, int totalWork)
			{
				Monitor = monitor;
				TotalWork = totalWork;
				Name = name;
			}
		}
		
		class QueuedEndTask : QueuedUpdate
		{
			public override void Execute (LogView pad)
			{
				pad.UnsafeEndTask (Monitor);
			}

			public QueuedEndTask (LogViewProgressMonitor monitor)
			{
				Monitor = monitor;
			}
		}
	}

	public class LogViewProgressMonitor : OutputProgressMonitor
	{
		LogView outputPad;

		LogTextWriter internalLogger = new LogTextWriter ();
		NotSupportedTextReader inputReader = new NotSupportedTextReader ();
		OperationConsole console;
		
		internal LogView LogView {
			get { return outputPad; }
		}

		internal class IndentTracker {
			List<TextTag> tags = new List<TextTag> ();
			public TextTag IndentTag;

			int indent = 0;
			public TextTag Indent ()
			{
				TextTag tag;

				indent++;
				if (indent >= tags.Count) {
					tag = new TextTag (indent.ToString ());
					tag.LeftMargin = 10 + 15 * (indent - 1);
					tags.Add (tag);
				} else {
					tag = tags [indent];
				}

				IndentTag = tag;
				return tag;
			}

			public void Unindent ()
			{
				if (indent >= 0) {
					indent--;
					IndentTag = tags [indent];
				}
			}
		}

		internal IndentTracker Indent { get; set; }
		internal TextMark Marker { get; set; }

		internal LogViewProgressMonitor (LogView pad, bool clearConsole): base (Runtime.MainSynchronizationContext)
		{
			outputPad = pad;

			Indent = new IndentTracker ();

			if (clearConsole)
				outputPad.Clear ();
			internalLogger.TextWritten += WriteConsoleLogText;
			console = new LogViewProgressConsole (this);
		}

		public override OperationConsole Console {
			get { return console; }
		}

		internal void Cancel ()
		{
			CancellationTokenSource.Cancel ();
		}

		protected override void OnWriteLog (string message)
		{
			outputPad.WriteText (this, message);
			base.OnWriteLog (message);
		}

		protected override void OnWriteErrorLog (string message)
		{
			outputPad.WriteText (this, message);
			base.OnWriteErrorLog (message);
		}

		protected override void OnBeginTask (string name, int totalWork, int stepWork)
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.BeginTask (this, name, totalWork);
			base.OnBeginTask (name, totalWork, stepWork);
		}

		protected override void OnEndTask (string name, int totalWork, int stepWork)
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.EndTask (this);
			base.OnEndTask (name, totalWork, stepWork);
		}

		void WriteConsoleLogText (string text)
		{
			outputPad.WriteConsoleLogText (this, text);
		}

		Exception GetDisposedException ()
		{
			return new InvalidOperationException ("Output progress monitor already disposed.");
		}
		
		protected override void OnCompleted ()
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.WriteText (this, "\n");

			foreach (string msg in SuccessMessages)
				outputPad.WriteText (this, msg + "\n");

			foreach (string msg in Warnings)
				outputPad.WriteText (this, msg + "\n");

			foreach (ProgressError msg in Errors)
				outputPad.WriteError (this, msg.DisplayMessage + "\n");
			
			base.OnCompleted ();

			outputPad = null;

			if (Completed != null)
				Completed (this, EventArgs.Empty);
		}

		public override void Dispose ()
		{
			base.Dispose ();
			console.Dispose ();
		}

		internal event EventHandler Completed;

		class LogViewProgressConsole: OperationConsole
		{
			LogViewProgressMonitor monitor;

			public LogViewProgressConsole (LogViewProgressMonitor monitor)
			{
				this.monitor = monitor;
				CancellationSource = monitor.CancellationTokenSource;
			}

			public override TextReader In {
				get {
					return monitor.inputReader;
				}
			}
			public override TextWriter Out {
				get {
					return monitor.Log;
				}
			}
			public override TextWriter Error {
				get {
					return monitor.ErrorLog;
				}
			}
			public override TextWriter Log {
				get {
					return monitor.internalLogger;
				}
			}

			public override void Debug (int level, string category, string message)
			{
				monitor.outputPad.WriteDebug (level, category, message);
			}

			public override void Dispose ()
			{
				if (monitor != null) {
					var m = monitor; // Avoid recursive dispose, since the monitor also disposes this console
					monitor = null;
					m.Dispose ();
				}
				base.Dispose ();
			}
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

