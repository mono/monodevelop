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
			resultStore = new Gtk.TreeStore (typeof (Gdk.Pixbuf), typeof (string));
			
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
			
			cellRendText = new CellRendererText ();
			cellRendText.Ellipsize = Pango.EllipsizeMode.End;
			this.resultsTreeview.AppendColumn ("", cellRendText, "text", 1);
			
			elementsStore = new Gtk.TreeStore (typeof (Gdk.Pixbuf), typeof (string), typeof (string), typeof (string));
			this.elementsTreeview.Model = this.elementsStore;
			this.elementsTreeview.HeadersVisible = false;
			cellRendText = new CellRendererText ();
			cellRendText.Ellipsize = Pango.EllipsizeMode.End;
			this.elementsTreeview.AppendColumn ("", cellRendText, "text", 1);
			this.elementsTreeview.Selection.Changed += new EventHandler (OnEntrySelected);
			FillElementsBox ();
		}
		
		void OnEntrySelected (object sender, EventArgs args)
		{			
			TreeIter iter;
			this.textview1.Visible = false;
			if (elementsTreeview.Selection.GetSelected (out iter)) {
				string description = elementsStore.GetValue (iter, 2) as string;
				if (!String.IsNullOrEmpty (description)) {
					this.textview1.Buffer.Text = description;
					this.textview1.Visible = true;
				}
			}
		}
		
		
		void PerformQuery (string input, string pattern, RegexOptions options)
		{
			Regex regex = new Regex (pattern, options);
			this.resultStore.Clear ();
			Console.WriteLine (regex.GetGroupNumbers ().Length);
			foreach (Match match in regex.Matches (input)) {
				TreeIter iter = this.resultStore.AppendValues (null, String.Format ("Match '{0}'", match.Value) );
				int i = 0;
				foreach (Group group in match.Groups) {
					if (i > 0) {
						TreeIter groupIter;
						if (group.Success) {
							groupIter = this.resultStore.AppendValues (iter, null, String.Format ("Group '{0}':'{1}'", regex.GroupNameFromNumber (i), group.Value));
							foreach (Capture capture in match.Captures) {
								this.resultStore.AppendValues (groupIter, null, String.Format ("Capture '{0}'", capture.Value));
							}
						} else {
							groupIter = this.resultStore.AppendValues (iter, null, String.Format ("Group '{0}' not found", regex.GroupNameFromNumber (i)));
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
