//
// RegexLibraryWindow.cs
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

using Gtk;

using MonoDevelop.Core;

namespace MonoDevelop.RegexToolkit
{
	
	
	public partial class RegexLibraryWindow : Gtk.Window
	{
		ListStore store;
		Expression[] expressions;
		
		public RegexLibraryWindow() : base(Gtk.WindowType.Toplevel)
		{
			this.Build();
			this.TransientFor = MonoDevelop.Ide.Gui.IdeApp.Workbench.RootWindow;
			
			this.buttonCancel.Clicked += delegate {
				Destroy ();
			};
			this.buttonOk.Clicked += delegate {
				SynchronizeExpressions ();
			};
			
			store = new ListStore (typeof (string), typeof (string), typeof (Expression));
			expressionsTreeview.Model = store;
			
			this.expressionsTreeview.AppendColumn (GettextCatalog.GetString ("Title"), new CellRendererText (), "text", 0);
			this.expressionsTreeview.AppendColumn (GettextCatalog.GetString ("Rating"), new CellRendererText (), "text", 1);
			
			this.expressionsTreeview.Selection.Changed += delegate {
				ShowSelectedEntry ();			
			};
			this.searchEntry.Changed += delegate {
				FilterItems (searchEntry.Text);
			};
			LoadRegexes ();
			UpdateExpressions ();
		}
		
		public override void Destroy ()
		{
			if (store != null) {
				store.Dispose ();
				store = null;
			}
			base.Destroy ();
		}

		void ShowSelectedEntry ()
		{
			TreeIter iter;
			if (expressionsTreeview.Selection.GetSelected (out iter)) {
				Expression expression = store.GetValue (iter, 2) as Expression;
				if (expression == null) 
					return;
				authorEntry.Text = expression.AuthorName;
				sourceEntry.Text = expression.Source;
				patternEntry.Text = expression.Pattern;
				descriptionTextview.Buffer.Text = expression.Description;
				matchingEntry.Text = expression.MatchingText;
				nonMatchingEntry.Text = expression.NonMatchingText;
			}
		}
		void FilterItems (string pattern)
		{
			if (expressions == null)
				return;
			store.Clear ();
			foreach (Expression expr in expressions) {
				if ((expr.Description + expr.AuthorName + expr.Title + expr.Source).ToUpper ().Contains (pattern.ToUpper ())) {
					store.AppendValues (expr.Title, expr.Rating.ToString (), expr);
				}
			}
		}
		void UpdateExpressions ()
		{
			if (expressions == null)
				return;
			store.Clear ();
			foreach (Expression expr in expressions) {
				store.AppendValues (expr.Title, expr.Rating.ToString (), expr);
			}
		}
		
		void SynchronizeExpressions ()
		{
			UpdateInProgressDialog updateDialog = new UpdateInProgressDialog ();
			updateDialog.Parent = this;
			updateDialog.Run ();
			if (updateDialog.Expressions != null) {
				this.expressions = updateDialog.Expressions;
				WriteRegexes ();
				UpdateExpressions ();
			}
		}
		
#region I/O
		const string version         = "1.0";
		const string libraryFileName = "MonoDevelop.RegexToolkit.library.xml";
		
		static string LibraryLocation {
			get {
				return System.IO.Path.Combine (PropertyService.ConfigPath, libraryFileName);
			}
		}
		const string Node = "RegexLibrary";
		const string VersionAttribute = "version";
		const string ExpressionNode = "Expression";
		
		const string AuthorAttribute = "author";
		const string DescriptionAttribute = "description";
		const string SourceAttribute = "source";
		const string PatternAttribute = "pattern";
		const string RatingAttribute = "rating";
		const string TitleAttribute = "title";
		const string MatchingAttribute = "matching";
		const string NonMatchingAttribute = "nonmatching";
		
		void LoadRegexes ()
		{
			XmlReader reader = null;
			List<Expression> expressionList = new List<Expression> (); 
			try {
				reader = XmlTextReader.Create (LibraryLocation);
				while (reader.Read ()) {
					if (reader.IsStartElement ()) {
						switch (reader.LocalName) {
						case ExpressionNode:
							Expression newExpression = new Expression ();
							newExpression.AuthorName = reader.GetAttribute (AuthorAttribute);
							newExpression.Source = reader.GetAttribute (SourceAttribute);
							newExpression.Description = reader.GetAttribute (DescriptionAttribute);
							newExpression.Pattern = reader.GetAttribute (PatternAttribute);
							newExpression.Title = reader.GetAttribute (TitleAttribute);
							try {
								newExpression.Rating = Int32.Parse (reader.GetAttribute (RatingAttribute));
							} catch {
								newExpression.Rating = -1;
							}
							newExpression.MatchingText = reader.GetAttribute (MatchingAttribute);
							newExpression.NonMatchingText = reader.GetAttribute (NonMatchingAttribute);
							expressionList.Add (newExpression);
							break;
						}
					}
				}
			} catch (Exception e) {
				LoggingService.LogError (e.ToString ());
			} finally {
				if (reader != null)
					reader.Close ();
			}
			this.expressions = expressionList.ToArray ();
		}
		
		void WriteRegexes ()
		{
			Stream stream = new FileStream (LibraryLocation, FileMode.Create);
			XmlWriter writer = new XmlTextWriter (stream, Encoding.UTF8);
			try {
				writer.Settings.Indent = true;
				writer.WriteStartElement (Node);
				writer.WriteAttributeString (VersionAttribute, version);
				
				foreach (Expression expr in expressions) {
					writer.WriteStartElement (ExpressionNode);
					writer.WriteAttributeString (AuthorAttribute, expr.AuthorName);
					writer.WriteAttributeString (SourceAttribute, expr.Source);
					writer.WriteAttributeString (DescriptionAttribute, expr.Description);
					writer.WriteAttributeString (PatternAttribute, expr.Pattern);
					writer.WriteAttributeString (RatingAttribute, expr.Rating.ToString ());
					writer.WriteAttributeString (TitleAttribute, expr.Title);
					writer.WriteAttributeString (MatchingAttribute, expr.MatchingText);
					writer.WriteAttributeString (NonMatchingAttribute, expr.NonMatchingText);
					writer.WriteEndElement (); // ExpressionNode
				}
				
				writer.WriteEndElement (); // Node
			} finally {
				writer.Close ();
				stream.Close ();
			}
		}
#endregion
	}
}
