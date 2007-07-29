//
// RegexToolkit.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using Gtk;

namespace MonoDevelop.RegexToolkit
{
	public partial class RegexToolkit : Gtk.Dialog
	{
		ListStore optionsStore;
		TreeStore resultStore;
		TreeStore elementsStore;
		
		public RegexToolkit()
		{
			this.Build();
			optionsStore = new ListStore (typeof (bool), typeof (string), typeof (Options));
			resultStore = new Gtk.TreeStore (typeof (Gdk.Pixbuf), typeof (string), typeof (int), typeof (int));
			
			FillOptionsBox ();
			
			this.buttonCancel.Clicked += delegate {
				this.Destroy ();
			};
			
			this.buttonOk.Clicked += delegate {
				PerformQuery (this.inputTextview.Buffer.Text,
				              this.regExTextview.Buffer.Text,
				              GetOptions ());
			};
			
			this.optionsTreeview.Model = this.optionsStore;
			this.optionsTreeview.HeadersVisible = false;
			
			CellRendererToggle cellRendToggle = new CellRendererToggle ();
			cellRendToggle.Toggled += new ToggledHandler (OptionToggled);
			cellRendToggle.Activatable = true;
			this.optionsTreeview.AppendColumn ("", cellRendToggle, "active", 0);
			
			CellRendererText cellRendText = new CellRendererText ();
			cellRendText.Ellipsize = Pango.EllipsizeMode.End;
			this.optionsTreeview.AppendColumn ("", cellRendText, "text", 1);
			
			this.resultsTreeview.Model = this.resultStore;
			this.resultsTreeview.HeadersVisible = false;
			this.resultsTreeview.AppendColumn (String.Empty, new CellRendererPixbuf (), "stock_id", 0);
				
			cellRendText = new CellRendererText ();
			cellRendText.Ellipsize = Pango.EllipsizeMode.End;
			this.resultsTreeview.AppendColumn ("", cellRendText, "text", 1);
			
			this.resultsTreeview.RowActivated += delegate (object sender, RowActivatedArgs e) {
				Gtk.TreeIter iter;
				if (resultStore.GetIter (out iter, e.Path)) {
					System.Console.WriteLine (resultStore.GetValue (iter, 2));
					System.Console.WriteLine (resultStore.GetValue (iter, 3));
					int index  = (int)resultStore.GetValue (iter, 2);
					int length = (int)resultStore.GetValue (iter, 3);
					if (index >= 0) {
						this.inputTextview.Buffer.SelectRange (this.inputTextview.Buffer.GetIterAtOffset (index),
						                                       this.inputTextview.Buffer.GetIterAtOffset (index + length));
					} else {
						this.inputTextview.Buffer.SelectRange (this.inputTextview.Buffer.GetIterAtOffset (0), this.inputTextview.Buffer.GetIterAtOffset (0));
					}
				}
			};
			
			elementsStore = new Gtk.TreeStore (typeof (Gdk.Pixbuf), typeof (string), typeof (string), typeof (string));
			this.elementsTreeview.Model = this.elementsStore;
			this.elementsTreeview.HeadersVisible = false;
			cellRendText = new CellRendererText ();
			cellRendText.Ellipsize = Pango.EllipsizeMode.End;
			this.elementsTreeview.AppendColumn ("", cellRendText, "text", 1);
			this.elementsTreeview.Selection.Changed += delegate {
				ShowTooltipForSelectedEntry ();			
			};
			bool shouldUpdateTooltip = false;
			this.elementsTreeview.ScrollEvent += delegate {
				shouldUpdateTooltip = true;
			}; 
			this.elementsTreeview.WidgetEvent += delegate {
				if (shouldUpdateTooltip) {
					ShowTooltipForSelectedEntry ();
					shouldUpdateTooltip = false;
				}
			};
			this.elementsTreeview.RowActivated += delegate (object sender, RowActivatedArgs e) {
				Gtk.TreeIter iter;
				if (elementsStore.GetIter (out iter, e.Path)) {
					string text = elementsStore.GetValue (iter, 3) as string;
					if (!System.String.IsNullOrEmpty (text)) {
						this.regExTextview.Buffer.InsertAtCursor (text);
					}
				}
			};
			
			FillElementsBox ();
		}
		
		void ShowTooltipForSelectedEntry ()
		{
			TreeIter iter;
			if (elementsTreeview.Selection.GetSelected (out iter)) {
				string description = elementsStore.GetValue (iter, 2) as string;
				if (!String.IsNullOrEmpty (description)) {
					Gdk.Rectangle rect = elementsTreeview.GetCellArea (elementsTreeview.Selection.GetSelectedRows () [0], elementsTreeview.GetColumn (0));
					int wx, wy;
					elementsTreeview.TranslateCoordinates (this, rect.X, rect.Bottom, out wx, out wy);
					ShowTooltip (description, wx, wy);
				} else {
					HideTooltipWindow ();
				}
			} else {
				HideTooltipWindow ();
			}
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			HideTooltipWindow ();
		}

		
		CustomTooltipWindow tooltipWindow = null;
		public void HideTooltipWindow ()
		{
			if (tooltipWindow != null) {
				tooltipWindow.Destroy ();
				tooltipWindow = null;
			}
		}
		public void ShowTooltip (string text, int x, int y)
		{
			HideTooltipWindow (); 
			tooltipWindow = new CustomTooltipWindow ();
			tooltipWindow.Tooltip = text;
			int ox, oy;
			this.GdkWindow.GetOrigin (out ox, out oy);
			tooltipWindow.Move (ox + x, oy + y);
			tooltipWindow.ShowAll ();
		}
			
		public class CustomTooltipWindow : Gtk.Window
		{
			string tooltip;
			public string Tooltip {
				get {
					return tooltip;
				}
				set {
					tooltip = value;
					label.Markup = tooltip;
				}
			}
			
			Label label = new Label ();
			public CustomTooltipWindow () : base (Gtk.WindowType.Popup)
			{
				Name = "gtk-tooltips";
				label.Xalign = 0;
				label.Xpad = 3;
				label.Ypad = 3;
				Add (label);
			}
			
			protected override bool OnExposeEvent (Gdk.EventExpose ev)
			{
				base.OnExposeEvent (ev);
				Gtk.Requisition req = SizeRequest ();
				Gtk.Style.PaintFlatBox (this.Style, 
				                        this.GdkWindow, 
				                        Gtk.StateType.Normal, 
				                        Gtk.ShadowType.Out, 
				                        Gdk.Rectangle.Zero, 
				                        this, "tooltip", 0, 0, req.Width, req.Height);
				return true;
			}
		}	

			
			
		void PerformQuery (string input, string pattern, RegexOptions options)
		{
			Regex regex = new Regex (pattern, options);
			this.resultStore.Clear ();
			Console.WriteLine (regex.GetGroupNumbers ().Length);
			foreach (Match match in regex.Matches (input)) {
				TreeIter iter = this.resultStore.AppendValues (Stock.Find, String.Format ("Match '{0}'", match.Value), match.Index, match.Length);
				int i = 0;
				foreach (Group group in match.Groups) {
					if (i > 0) {
						TreeIter groupIter;
						if (group.Success) {
							groupIter = this.resultStore.AppendValues (iter, Stock.Yes, String.Format ("Group '{0}':'{1}'", regex.GroupNameFromNumber (i), group.Value), group.Index, group.Length);
							foreach (Capture capture in match.Captures) {
								this.resultStore.AppendValues (groupIter, null, String.Format ("Capture '{0}'", capture.Value), capture.Index, capture.Length);
							}
						} else {
							groupIter = this.resultStore.AppendValues (iter, Stock.No, String.Format ("Group '{0}' not found", regex.GroupNameFromNumber (i)), -1, -1);
						}

					}
					i++;
				}
			}
		}
		
		RegexOptions GetOptions ()
		{
			RegexOptions result = RegexOptions.None;
			Gtk.TreeIter iter;
			if (this.optionsStore.GetIterFirst (out iter)) { 
				do {
					bool toggled = (bool)this.optionsStore.GetValue (iter, 0);
					if (toggled) {
						result |= ((Options)this.optionsStore.GetValue (iter, 2)).RegexOptions; 
					}
				} while (this.optionsStore.IterNext (ref iter));
			}
			return result;
		}
		
		void OptionToggled (object sender, ToggledArgs e)
		{
			TreeIter iter;
			if (this.optionsStore.GetIterFromString (out iter, e.Path)) {
				bool toggled = (bool)this.optionsStore.GetValue (iter, 0);
				this.optionsStore.SetValue (iter, 0, !toggled);
			}
		}
		
		class Options 
		{
			RegexOptions options;
			string       name;
			
			public string Name {
				get {
					return name;
				}
			}
			
			public RegexOptions RegexOptions {
				get {
					return options;
				}
			}
			
			public Options (RegexOptions options, string name)
			{
				this.options = options;
				this.name    = name;
			}
		}
		
		void FillOptionsBox ()
		{
			Options[] options = {
				new Options (RegexOptions.IgnoreCase, "Ignore case"),
				new Options (RegexOptions.Multiline, "Multi line"),
				new Options (RegexOptions.RightToLeft, "Right to left")
			};
			foreach (Options option in options) {
				this.optionsStore.AppendValues (false, option.Name, option);				
			}
		}
		
		void FillElementsBox ()
		{
			Stream stream = typeof (RegexToolkit).Assembly.GetManifestResourceStream ("RegexElements.xml");
			if (stream == null)
				return;
			XmlReader reader = new XmlTextReader (stream);
			while (reader.Read ()) {
				if (reader.NodeType != XmlNodeType.Element)
					continue;
				switch (reader.LocalName) {
				case "Group":
					TreeIter groupIter = this.elementsStore.AppendValues (null, reader.GetAttribute ("_name"), null, null);
					while (reader.Read ()) {
						if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName == "Group") 
							break;
						switch (reader.LocalName) {
							case "Element":
								
								this.elementsStore.AppendValues (groupIter, null, reader.GetAttribute ("_name"), reader.GetAttribute ("_description"), reader.ReadElementString ());
								break;
						}
					}
					break;
				}
			}
		}
	}
}
