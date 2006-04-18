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

using MonoDevelop.Core.Properties;
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

		private static Gtk.Tooltips tips = new Gtk.Tooltips ();
		
		TextTag tag;
		TextTag bold;
		int ident = 0;
		ArrayList tags = new ArrayList ();
		Stack indents = new Stack ();

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
			buttonStop.SetTooltip (tips, GettextCatalog.GetString ("Stop"), GettextCatalog.GetString ("Stop"));
			toolbar.Insert (buttonStop, -1);

			ToolButton buttonClear = new ToolButton ("gtk-clear");
			buttonClear.Clicked += new EventHandler (OnButtonClearClick);
			buttonClear.SetTooltip (tips, GettextCatalog.GetString ("Clear console"), GettextCatalog.GetString ("Clear console"));
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

			IdeApp.ProjectOperations.CombineOpened += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineOpen));
			IdeApp.ProjectOperations.CombineClosed += (CombineEventHandler) Services.DispatchService.GuiDispatch (new CombineEventHandler (OnCombineClosed));

			this.title = title;
			this.icon = icon;
			
			Control.ShowAll ();
		}

		void IPadContent.Initialize (IPadWindow window)
		{
			this.window = window;
			window.Title = title;
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
			buffer.Clear ();
			window.Title = "<span foreground=\"blue\">" + title + "</span>";
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
		
		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public string DefaultPlacement {
			get { return "Bottom"; }
		}
		
		public void EndProgress ()
		{
			window.Title = title;
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
		}
	}
}
