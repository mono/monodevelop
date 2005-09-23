// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <owner name="Lluis Sanchez" email="lluis@novell.com"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.CodeDom.Compiler;
using System.IO;
using System.Diagnostics;
using MonoDevelop.Services;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui;

using Gtk;
using Pango;

namespace MonoDevelop.Gui.Pads
{	
	public class DefaultMonitorPad : IPadContent
	{
		Gtk.TextBuffer buffer;
		Gtk.TextView textEditorControl;
		Gtk.ScrolledWindow scroller;
		Gtk.HBox hbox;
		ToolButton buttonStop;

		private static Gtk.Tooltips tips = new Gtk.Tooltips ();
		
		TextTag tag;
		TextTag bold;
		int ident = 0;
		ArrayList tags = new ArrayList ();
		Stack indents = new Stack ();

		string markupTitle;
		string title;
		string icon;
		string id;

		private IAsyncOperation asyncOperation;

		public DefaultMonitorPad (string title, string icon)
		{
			buffer = new Gtk.TextBuffer (new Gtk.TextTagTable ());
			textEditorControl = new Gtk.TextView (buffer);
			textEditorControl.Editable = false;
			scroller = new Gtk.ScrolledWindow ();
			scroller.ShadowType = ShadowType.In;
			scroller.Add (textEditorControl);

			Toolbar toolbar = new Toolbar ();
			toolbar.IconSize = IconSize.SmallToolbar;
			toolbar.Orientation = Orientation.Vertical;
			toolbar.ToolbarStyle = ToolbarStyle.Icons;

			buttonStop = new ToolButton ("gtk-stop");
			buttonStop.Clicked += new EventHandler (OnButtonStopClick);
			buttonStop.SetTooltip (tips, "Stop", "Stop");
			toolbar.Insert (buttonStop, -1);

			ToolButton buttonClear = new ToolButton ("gtk-clear");
			buttonClear.Clicked += new EventHandler (OnButtonClearClick);
			buttonClear.SetTooltip (tips, "Clear console", "Clear console");
			toolbar.Insert (buttonClear, -1);

			hbox = new HBox (false, 5);
			hbox.PackStart (scroller, true, true, 0);
			hbox.PackEnd (toolbar, false, false, 0);
			
			bold = new TextTag ("bold");
			bold.Weight = Pango.Weight.Bold;
			buffer.TagTable.Add (bold);
			
			tag = new TextTag ("0");
			tag.Indent = 10;
			buffer.TagTable.Add (tag);
			tags.Add (tag);

			Runtime.ProjectService.CombineOpened += (CombineEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineOpen));
			Runtime.ProjectService.CombineClosed += (CombineEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineClosed));

			this.title = title;
			this.icon = icon;
			this.markupTitle = title;
			
			Control.ShowAll ();
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
			buffer.Clear();
		}

		void OnButtonStopClick (object sender, EventArgs e)
		{
			asyncOperation.Cancel ();
		}

		void OnCombineOpen (object sender, CombineEventArgs e)
		{
			buffer.Clear ();
		}

		void OnCombineClosed (object sender, CombineEventArgs e)
		{
			buffer.Clear ();
		}

		public void BeginProgress (string title)
		{
			this.title = title;
			this.markupTitle = "<span foreground=\"blue\">" + title + "</span>";
			
			buffer.Clear ();
			OnTitleChanged (null);
			buttonStop.Sensitive = true;
		}
		
		public void BeginTask (string name, int totalWork)
		{
			if (name != null && name.Length > 0) {
				Indent ();
				indents.Push (name);
			} else
				indents.Push (null);

			if (name != null) {
				TextIter it = buffer.EndIter;
				string txt = "\n" + name + "\n";
				buffer.InsertWithTags (ref it, txt, tag, bold);
			}
		}
		
		public void EndTask ()
		{
			if (indents.Count > 0 && indents.Pop () != null)
				Unindent ();
		}
		
		public void WriteText (string text)
		{
			AddText (text);
//			buffer.MoveMark (buffer.InsertMark, buffer.EndIter);
			if (text.EndsWith ("\n"))
				textEditorControl.ScrollMarkOnscreen (buffer.InsertMark);
		}
		
		public virtual Gtk.Widget Control {
			get { return hbox; }
		}
		
		public string Title {
			get { return markupTitle; }
		}
		
		public string Icon {
			get { return icon; }
		}
		
		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public string DefaultPlacement {
			get { return "Bottom"; }
		}
		
		public void EndProgress ()
		{
			markupTitle = title;
			OnTitleChanged (null);
			buttonStop.Sensitive = false;
		}
		
		void AddText (string s)
		{
			TextIter it = buffer.EndIter;
			buffer.InsertWithTags (ref it, s, tag);
		}
		
		void Indent ()
		{
			ident++;
			if (ident >= tags.Count) {
				tag = new TextTag (ident.ToString ());
				tag.Indent = 10 + 15 * (ident - 1);
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
		}
	
		public void RedrawContent()
		{
			OnTitleChanged(null);
			OnIconChanged(null);
		}
		
		protected virtual void OnTitleChanged(EventArgs e)
		{
			if (TitleChanged != null) {
				TitleChanged(this, e);
			}
		}

		protected virtual void OnIconChanged(EventArgs e)
		{
			if (IconChanged != null) {
				IconChanged(this, e);
			}
		}

		public event EventHandler TitleChanged;
		public event EventHandler IconChanged;
	}
}
