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
		ToggleToolButton buttonPin;

		private static Gtk.Tooltips tips = new Gtk.Tooltips ();
		
		TextTag tag;
		TextTag bold;
		int ident = 0;
		ArrayList tags = new ArrayList ();
		Stack indents = new Stack ();

		string originalTitle;
		string icon;
		string id;
		int instanceNum;
		string typeTag;

		private IAsyncOperation asyncOperation;

		public DefaultMonitorPad (string typeTag, string icon, int instanceNum)
		{
			this.instanceNum = instanceNum;
			this.typeTag = typeTag;
			
			this.icon = icon;
			
			buffer = new Gtk.TextBuffer (new Gtk.TextTagTable ());
			textEditorControl = new Gtk.TextView (buffer);
			textEditorControl.Editable = false;
			scroller = new Gtk.ScrolledWindow ();
			scroller.ShadowType = ShadowType.In;
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
			
			tag = new TextTag ("0");
			tag.LeftMargin = 10;
			buffer.TagTable.Add (tag);
			tags.Add (tag);

			IdeApp.ProjectOperations.CombineOpened += (CombineEventHandler) DispatchService.GuiDispatch (new CombineEventHandler (OnCombineOpen));
			IdeApp.ProjectOperations.CombineClosed += (CombineEventHandler) DispatchService.GuiDispatch (new CombineEventHandler (OnCombineClosed));

			Control.ShowAll ();
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

		public void BeginProgress (string title)
		{
			originalTitle = window.Title;
			buffer.Clear ();
			window.Title = "<span foreground=\"blue\">" + originalTitle + "</span>";
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
			window.Title = originalTitle;
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
		}
	
		public void RedrawContent()
		{
		}
	}
}
