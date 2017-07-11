// 
// RegexToolkitWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Text.RegularExpressions;
using System.Threading;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.IO;
using System.Xml;
using MonoDevelop.Components;

namespace MonoDevelop.RegexToolkit
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class RegexToolkitWidget : Gtk.Bin
	{
		ListStore optionsStore;
		TreeStore resultStore;
		
		Thread regexThread;
		public string Regex { 
			get {
				return entryRegEx.Text;
			}
			set {
				entryRegEx.Text = value;
			}
		}

		public RegexToolkitWidget ()
		{
			this.Build ();
			optionsStore = new ListStore (typeof(bool), typeof(string), typeof(Options));
			resultStore = new Gtk.TreeStore (typeof(string), typeof(string), typeof(int), typeof(int));
			
			FillOptionsBox ();
			
			this.entryRegEx.Changed += UpdateStartButtonSensitivity;
			this.inputTextview.Buffer.Changed += UpdateStartButtonSensitivity;

			SetFindMode (true);
			
			var cellRendText = new CellRendererText ();
			cellRendText.Ellipsize = Pango.EllipsizeMode.End;
			
			this.optionsTreeview.Model = this.optionsStore;
			this.optionsTreeview.HeadersVisible = false;
			
			CellRendererToggle cellRendToggle = new CellRendererToggle ();
			cellRendToggle.Toggled += new ToggledHandler (OptionToggled);
			cellRendToggle.Activatable = true;
			this.optionsTreeview.AppendColumn ("", cellRendToggle, "active", 0);
			this.optionsTreeview.AppendColumn ("", cellRendText, "text", 1);
			
			this.resultsTreeview.Model = this.resultStore;
			this.resultsTreeview.HeadersVisible = false;
			var col = new TreeViewColumn ();
			this.resultsTreeview.AppendColumn (col);
			var pix = new CellRendererImage ();
			
			col.PackStart (pix, false);
			col.AddAttribute (pix, "stock_id", 0);
			col.PackStart (cellRendText, true);
			col.AddAttribute (cellRendText, "text", 1);
			
			this.resultsTreeview.RowActivated += delegate(object sender, RowActivatedArgs e) {
				Gtk.TreeIter iter;
				if (resultStore.GetIter (out iter, e.Path)) {
					int index = (int)resultStore.GetValue (iter, 2);
					int length = (int)resultStore.GetValue (iter, 3);
					if (index >= 0) {
						this.inputTextview.Buffer.SelectRange (this.inputTextview.Buffer.GetIterAtOffset (index),
						                                       this.inputTextview.Buffer.GetIterAtOffset (index + length));
					} else {
						this.inputTextview.Buffer.SelectRange (this.inputTextview.Buffer.GetIterAtOffset (0), this.inputTextview.Buffer.GetIterAtOffset (0));
					}
				}
			};
			
			this.entryReplace.Sensitive = this.checkbuttonReplace.Active = false;
			this.entryReplace.Changed += delegate {
				UpdateRegex ();
			};
			this.checkbuttonReplace.Toggled += delegate {
				this.entryReplace.Sensitive = this.checkbuttonReplace.Active;
				UpdateRegex ();
			};
			this.expandMatches.Toggled += delegate {
				UpdateRegex ();
			};
			this.vbox4.WidthRequest = 380;
			this.scrolledwindow5.HeightRequest = 150;
			this.scrolledwindow1.HeightRequest = 150;
			Show ();
		}

		void UpdateRegex ()
		{
			if (regexThread != null && regexThread.IsAlive) {
				regexThread.Abort ();
				regexThread.Join ();
				regexThread = null;
			}

			regexThread = new Thread (delegate () {
				PerformQuery (inputTextview.Buffer.Text, this.entryRegEx.Text, this.entryReplace.Text, GetOptions ());
			});

			regexThread.IsBackground = true;
			regexThread.Name = "regex thread";
			regexThread.Start ();
			SetFindMode (!checkbuttonReplace.Active);
		}

		public void InsertText (string text)
		{
			this.entryRegEx.InsertText (text);
		}
		
		void PerformQuery (string input, string pattern, string replacement, RegexOptions options)
		{
			try {
				Regex regex = new Regex (pattern, options);
				Application.Invoke ((o, args) => {
					this.resultStore.Clear ();
					var matches = regex.Matches (input);
					foreach (Match match in matches) {
						TreeIter iter = this.resultStore.AppendValues (Stock.Find, String.Format (GettextCatalog.GetString ("Match '{0}'"), match.Value), match.Index, match.Length);
						int i = 0;
						foreach (Group group in match.Groups) {
							TreeIter groupIter;
							if (group.Success) {
								groupIter = this.resultStore.AppendValues (iter, Stock.Apply, String.Format (GettextCatalog.GetString ("Group '{0}':'{1}'"), regex.GroupNameFromNumber (i), group.Value), group.Index, group.Length);
								foreach (Capture capture in match.Captures) {
									this.resultStore.AppendValues (groupIter, null, String.Format (GettextCatalog.GetString ("Capture '{0}'"), capture.Value), capture.Index, capture.Length);
								}
							} else {
								groupIter = this.resultStore.AppendValues (iter, Stock.Cancel, String.Format (GettextCatalog.GetString ("Group '{0}' not found"), regex.GroupNameFromNumber (i)), -1, -1);
							}
							i++;
						}
					}
					if (matches.Count == 0) {
						this.resultStore.AppendValues (Stock.Find, GettextCatalog.GetString ("No matches"));
					}
					if (this.expandMatches.Active) {
						this.resultsTreeview.ExpandAll ();
					}
					if (!String.IsNullOrEmpty (replacement))
						this.replaceResultTextview.Buffer.Text = regex.Replace (input, replacement);
				});
			} catch (ThreadAbortException) {
				Thread.ResetAbort ();
			} catch (ArgumentException) {
				Application.Invoke ((o, args) => {
					Ide.IdeApp.Workbench.StatusBar.ShowError (GettextCatalog.GetString ("Invalid expression"));
				});
			} finally {
				regexThread = null;
			}
		}


		
		void SetFindMode (bool findMode)
		{
			this.notebook2.ShowTabs = !findMode;
			if (findMode)
				this.notebook2.Page = 0;
		}
		
		void UpdateStartButtonSensitivity (object sender, EventArgs args)
		{
			Ide.IdeApp.Workbench.StatusBar.ShowReady ();
			UpdateRegex ();
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
				UpdateRegex ();
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
				this.name = name;
			}
		}
		
		void FillOptionsBox ()
		{
			Options[] options = {
				new Options (RegexOptions.IgnorePatternWhitespace, GettextCatalog.GetString ("Ignore Whitespace")),
				new Options (RegexOptions.IgnoreCase, GettextCatalog.GetString ("Ignore case")),
				new Options (RegexOptions.Singleline, GettextCatalog.GetString ("Single line")),
				new Options (RegexOptions.Multiline, GettextCatalog.GetString ("Multi line")),
				new Options (RegexOptions.ExplicitCapture, GettextCatalog.GetString ("Explicit Capture")),
				new Options (RegexOptions.RightToLeft, GettextCatalog.GetString ("Right to left"))
			};
			foreach (Options option in options) {
				this.optionsStore.AppendValues (false, option.Name, option);
			}
		}
		
	}
}

