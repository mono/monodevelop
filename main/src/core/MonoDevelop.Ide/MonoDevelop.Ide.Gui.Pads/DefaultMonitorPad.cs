// DefaultMonitorPad.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.CodeDom.Compiler;
using System.IO;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

using Gtk;
using Pango;

namespace MonoDevelop.Ide.Gui.Pads
{	
	internal class DefaultMonitorPad : IPadContent
	{
		IPadWindow window;
		Gtk.TextBuffer buffer;
		Gtk.TextView textEditorControl;
		Gtk.ScrolledWindow scroller;
		Gtk.HBox hbox;
		ToolButton buttonStop;
		ToggleToolButton buttonPin;
		TextMark endMark;

		private static Gtk.Tooltips tips = new Gtk.Tooltips ();
		
		TextTag tag;
		TextTag bold;
		TextTag errorTag;
		TextTag consoleLogTag;
		int ident = 0;
		ArrayList tags = new ArrayList ();
		Stack indents = new Stack ();

		string originalTitle;
		string icon;
		string id;
		int instanceNum;
		string typeTag;

		private IAsyncOperation asyncOperation;
		
		Queue updates = new Queue ();
		QueuedTextWrite lastTextWrite;
		GLib.TimeoutHandler outputDispatcher;
		bool outputDispatcherRunning = false;
		
		const int MAX_BUFFER_LENGTH = 200 * 1024; 

		public DefaultMonitorPad (string typeTag, string icon, int instanceNum)
		{
			this.instanceNum = instanceNum;
			this.typeTag = typeTag;
			
			this.icon = icon;
			
			buffer = new Gtk.TextBuffer (new Gtk.TextTagTable ());
			textEditorControl = new Gtk.TextView (buffer);
			textEditorControl.Editable = false;
			scroller = new Gtk.ScrolledWindow ();
			scroller.ShadowType = ShadowType.None;
			scroller.Add (textEditorControl);

			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.Menu;
			toolbar.Orientation = Orientation.Vertical;
			toolbar.ToolbarStyle = ToolbarStyle.Icons;
			toolbar.ShowArrow = true;

			buttonStop = new ToolButton ("gtk-stop");
			buttonStop.Clicked += new EventHandler (OnButtonStopClick);
			buttonStop.SetTooltip (tips, GettextCatalog.GetString ("Stop"), GettextCatalog.GetString ("Stop"));
			toolbar.Insert (buttonStop, -1);

			ToolButton buttonClear = new ToolButton ("gtk-clear");
			buttonClear.Clicked += new EventHandler (OnButtonClearClick);
			buttonClear.SetTooltip (tips, GettextCatalog.GetString ("Clear console"), GettextCatalog.GetString ("Clear console"));
			toolbar.Insert (buttonClear, -1);

			buttonPin = new ToggleToolButton ("md-pin-up");
			buttonPin.Clicked += new EventHandler (OnButtonPinClick);
			buttonPin.SetTooltip (tips, GettextCatalog.GetString ("Pin output pad"), GettextCatalog.GetString ("Pin output pad"));
			toolbar.Insert (buttonPin, -1);

			hbox = new HBox (false, 5);
			hbox.PackStart (scroller, true, true, 0);
			hbox.PackEnd (toolbar, false, false, 0);
			
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

			IdeApp.Workspace.FirstWorkspaceItemOpened += OnCombineOpen;
			IdeApp.Workspace.LastWorkspaceItemClosed += OnCombineClosed;

			Control.ShowAll ();
			
			outputDispatcher = new GLib.TimeoutHandler (outputDispatchHandler);
		}
		
		//mechanism to to batch copy text when large amounts are being dumped
		bool outputDispatchHandler ()
		{
			lock (updates.SyncRoot) {
				lastTextWrite = null;
				if (updates.Count == 0) {
					outputDispatcherRunning = false;
					return false;
				} else if (!outputDispatcherRunning) {
					updates.Clear ();
					return false;
				} else {
					while (updates.Count > 0) {
						QueuedUpdate up = (QueuedUpdate) updates.Dequeue ();
						up.Execute (this);
					}
				}
			}
			return true;
		}

		void IPadContent.Initialize (IPadWindow window)
		{
			this.window = window;
			window.Icon = icon;
		}
		
		public IAsyncOperation AsyncOperation {
			get {
				return asyncOperation;
			}
			set {
				asyncOperation = value;
			}
		}

		void OnButtonClearClick (object sender, EventArgs e)
		{
			lock (updates.SyncRoot) outputDispatcherRunning = false;
			buffer.Clear();
		}

		void OnButtonStopClick (object sender, EventArgs e)
		{
			asyncOperation.Cancel ();
		}

		void OnCombineOpen (object sender, EventArgs e)
		{
			lock (updates.SyncRoot) outputDispatcherRunning = false;
			buffer.Clear ();
		}

		void OnCombineClosed (object sender, EventArgs e)
		{
			lock (updates.SyncRoot) outputDispatcherRunning = false;
			buffer.Clear ();
		}
		
		void OnButtonPinClick (object sender, EventArgs e)
		{
			if (buttonPin.Active)
				buttonPin.StockId = "md-pin-down";
			else
				buttonPin.StockId = "md-pin-up";
		}
		
		public bool AllowReuse {
			get { return !buttonStop.Sensitive && !buttonPin.Active; }
		}
		
		void addQueuedUpdate (QueuedUpdate update)
		{
			lock (updates.SyncRoot) {
				updates.Enqueue (update);
				if (!outputDispatcherRunning) {
					GLib.Timeout.Add (50, outputDispatcher);
					outputDispatcherRunning = true;
				}
				lastTextWrite = update as QueuedTextWrite;
			}
		}

		public void BeginProgress (string title)
		{
			lock (updates.SyncRoot) {
				updates.Clear ();
				lastTextWrite = null;
			}
			
			Gtk.Application.Invoke (delegate {
				originalTitle = window.Title;
				buffer.Clear ();
				window.Title = "<span foreground=\"blue\">" + originalTitle + "</span>";
				buttonStop.Sensitive = true;
			});
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
			lock (updates.SyncRoot) {
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
			if (lastTextWrite != null)
				text = "\n" + text;
			QueuedTextWrite w = new QueuedTextWrite (text, consoleLogTag);
			addQueuedUpdate (w);
		}
		
		public void WriteError (string text)
		{
			QueuedTextWrite w = new QueuedTextWrite (text, errorTag);
			addQueuedUpdate (w);
		}
		
		void WriteText (string text, TextTag extraTag)
		{
			QueuedTextWrite w = new QueuedTextWrite (text, extraTag);
			addQueuedUpdate (w);
		}
		
		public virtual Gtk.Widget Control {
			get { return hbox; }
		}
		
		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public string DefaultPlacement {
			get { return "Bottom"; }
		}

		public string TypeTag {
			get {
				return typeTag;
			}
		}

		public int InstanceNum {
			get {
				return instanceNum;
			}
		}

		public void EndProgress ()
		{
			Gtk.Application.Invoke (delegate {
				window.Title = originalTitle;
				buttonStop.Sensitive = false;
			});
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
				tag = (TextTag) tags [ident];
			}
		}
		
		void Unindent ()
		{
			if (ident >= 0) {
				ident--;
				tag = (TextTag) tags [ident];
			}
		}
		
		public virtual void Dispose ()
		{
			lock (updates.SyncRoot) {
				updates.Clear ();
				lastTextWrite = null;
			}
		}
	
		public void RedrawContent()
		{
		}
		
		private abstract class QueuedUpdate
		{
			public abstract void Execute (DefaultMonitorPad pad);
		}
		
		private class QueuedTextWrite : QueuedUpdate
		{
			private System.Text.StringBuilder Text;
			public TextTag Tag;
			public override void Execute (DefaultMonitorPad pad)
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
			public override void Execute (DefaultMonitorPad pad)
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
			public override void Execute (DefaultMonitorPad pad)
			{
				pad.UnsafeEndTask ();
			}
		}
	}
}
