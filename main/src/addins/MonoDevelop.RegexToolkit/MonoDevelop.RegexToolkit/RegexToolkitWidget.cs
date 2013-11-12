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
namespace MonoDevelop.RegexToolkit
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class RegexToolkitWidget : Bin
	{
		ListStore optionsStore;
		TreeStore resultStore;
		
		Thread regexThread;
		
		public RegexToolkitWidget ()
		{
			this.Build ();
			optionsStore = new ListStore (typeof(bool), typeof(string), typeof(Options));
			resultStore = new TreeStore (typeof(string), typeof(string), typeof(int), typeof(int));
			
			FillOptionsBox ();
			
			this.buttonStart.Sensitive = false;
			this.entryRegEx.Changed += UpdateStartButtonSensitivity;
			this.inputTextview.Buffer.Changed += UpdateStartButtonSensitivity;
			
			this.buttonStart.Clicked += delegate {
				if (regexThread != null && regexThread.IsAlive) {
					regexThread.Abort ();
					regexThread.Join ();
					SetButtonStart (GettextCatalog.GetString ("Start Regular E_xpression"), "gtk-media-play");
					regexThread = null;
					return;
				}
				
				regexThread = new Thread (() => PerformQuery (inputTextview.Buffer.Text, this.entryRegEx.Text, this.entryReplace.Text, GetOptions ()));
				
				regexThread.IsBackground = true;
				regexThread.Name = "regex thread";
				regexThread.Start ();
				SetButtonStart (GettextCatalog.GetString ("Stop e_xecution"), "gtk-media-stop");
				
				SetFindMode (!checkbuttonReplace.Active);
			};
			
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
			var pix = new CellRendererPixbuf ();
			
			col.PackStart (pix, false);
			col.AddAttribute (pix, "stock_id", 0);
			col.PackStart (cellRendText, true);
			col.AddAttribute (cellRendText, "text", 1);
			
			this.resultsTreeview.RowActivated += delegate(object sender, RowActivatedArgs e) {
				TreeIter iter;
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
			this.checkbuttonReplace.Toggled += delegate {
				this.entryReplace.Sensitive = this.checkbuttonReplace.Active;
			};
			this.vbox4.WidthRequest = 380;
			this.scrolledwindow5.HeightRequest = 150;
			this.scrolledwindow1.HeightRequest = 150;
			Show ();
		}

		public void InsertText (string text)
		{
			entryRegEx.InsertText (text);
		}
		
		void PerformQuery (string input, string pattern, string replacement, RegexOptions options)
		{
			try {
				var regex = new Regex (pattern, options);
				Application.Invoke (delegate {
					resultStore.Clear ();
					var matches = regex.Matches (input);
					foreach (Match match in matches) {
						TreeIter iter = resultStore.AppendValues (Stock.Find, String.Format (GettextCatalog.GetString ("Match '{0}'"), match.Value), match.Index, match.Length);
						int i = 0;
						foreach (Group group in match.Groups) {
							TreeIter groupIter;
							if (group.Success) {
								groupIter = resultStore.AppendValues (iter, Stock.Apply, String.Format (GettextCatalog.GetString ("Group '{0}':'{1}'"), regex.GroupNameFromNumber (i), group.Value), group.Index, group.Length);
								foreach (Capture capture in match.Captures) {
									resultStore.AppendValues (groupIter, null, String.Format (GettextCatalog.GetString ("Capture '{0}'"), capture.Value), capture.Index, capture.Length);
								}
							} else {
								groupIter = resultStore.AppendValues (iter, Stock.Cancel, String.Format (GettextCatalog.GetString ("Group '{0}' not found"), regex.GroupNameFromNumber (i)), -1, -1);
							}
							i++;
						}
					}
					if (matches.Count == 0) {
						resultStore.AppendValues (Stock.Find, GettextCatalog.GetString ("No matches"));
					}
					if (expandMatches.Active) {
						resultsTreeview.ExpandAll ();
					}
					if (!String.IsNullOrEmpty (replacement))
						replaceResultTextview.Buffer.Text = regex.Replace (input, replacement);
				});
			} catch (ThreadAbortException) {
				Thread.ResetAbort ();
			} catch (ArgumentException) {
				Application.Invoke (delegate {
					IdeApp.Workbench.StatusBar.ShowError (GettextCatalog.GetString ("Invalid expression"));
				});
			} finally {
				regexThread = null;
				Application.Invoke (delegate {
					SetButtonStart (GettextCatalog.GetString ("Start Regular E_xpression"), "gtk-media-play");
				});
			}
		}

		void SetButtonStart (string text, string icon)
		{
			((Label)((HBox)((Alignment)buttonStart.Child).Child).Children [1]).Text = text;
			((Label)((HBox)((Alignment)buttonStart.Child).Child).Children [1]).UseUnderline = true;
			((Image)((HBox)((Alignment)buttonStart.Child).Child).Children [0]).Pixbuf = global::Stetic.IconLoader.LoadIcon (this, icon, IconSize.Menu);
		}
		
		
		void SetFindMode (bool findMode)
		{
			notebook2.ShowTabs = !findMode;
			if (findMode)
				notebook2.Page = 0;
		}
		
		void UpdateStartButtonSensitivity (object sender, EventArgs args)
		{
			buttonStart.Sensitive = entryRegEx.Text.Length > 0 && inputTextview.Buffer.CharCount > 0;
			IdeApp.Workbench.StatusBar.ShowReady ();
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (optionsStore != null) {
				optionsStore.Dispose ();
				optionsStore = null;
			}
			if (resultStore != null) {
				resultStore.Dispose ();
				resultStore = null;
			}
		}
		
		
		RegexOptions GetOptions ()
		{
			RegexOptions result = RegexOptions.None;
			TreeIter iter;
			if (optionsStore.GetIterFirst (out iter)) { 
				do {
					bool toggled = (bool)optionsStore.GetValue (iter, 0);
					if (toggled) {
						result |= ((Options)optionsStore.GetValue (iter, 2)).RegexOptions; 
					}
				} while (optionsStore.IterNext (ref iter));
			}
			return result;
		}
		
		void OptionToggled (object sender, ToggledArgs e)
		{
			TreeIter iter;
			if (optionsStore.GetIterFromString (out iter, e.Path)) {
				bool toggled = (bool)optionsStore.GetValue (iter, 0);
				optionsStore.SetValue (iter, 0, !toggled);
			}
		}
		
		class Options
		{
			readonly RegexOptions options;
			readonly string       name;
			
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
				optionsStore.AppendValues (false, option.Name, option);
			}
		}
		
	}
}

