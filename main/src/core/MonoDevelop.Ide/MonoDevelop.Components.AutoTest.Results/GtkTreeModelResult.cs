//
// GtkTreeModelResult.cs
//
// Author:
//       iain holmes <iain@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc
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
using System.Collections.Generic;
using Gtk;

namespace MonoDevelop.Components.AutoTest.Results
{
	public class GtkTreeModelResult : GtkWidgetResult
	{
		Widget ParentWidget;
		TreeModel TModel;
		int Column;
		TreeIter? resultIter;
		string DesiredText;

		public GtkTreeModelResult (Widget parent, TreeModel treeModel, int column) : base (parent)
		{
			ParentWidget = parent;
			TModel = treeModel;
			Column = column;
		}

		public GtkTreeModelResult (Widget parent, TreeModel treeModel, int column, TreeIter iter) : base (parent)
		{
			ParentWidget = parent;
			TModel = treeModel;
			Column = column;
			resultIter = iter;
		}
			
		public override AppResult Marked (string mark)
		{
			return null;
		}

		public override AppResult CheckType (Type desiredType)
		{
			return null;
		}

		public override AppResult Model (string column)
		{
			return null;
		}

		bool CheckForText (TreeModel model, TreeIter iter, bool exact)
		{
			string modelText = model.GetValue (iter, Column) as string;
			if (modelText == null) {
				return false;
			}

			if (exact) {
				return modelText == DesiredText;
			} else {
				return (modelText.IndexOf (DesiredText) > -1);
			}
		}

		public override AppResult Text (string text, bool exact)
		{
			DesiredText = text;

			if (resultIter.HasValue) {
				return CheckForText (TModel, (TreeIter) resultIter, exact) ? this : null;
			}

			TModel.Foreach ((m, p, i) => {
				if (CheckForText (m, i, exact)) {
					resultIter = i;
					return true;
				}

				return false;
			});

			return resultIter.HasValue ? this : null;
		}

		public override AppResult Property (string propertyName, object value)
		{
			if (resultIter != null && resultIter.HasValue) {
				var objectToCompare = TModel.GetValue (resultIter.Value, Column);
				return MatchProperty (propertyName, objectToCompare, value);
			}

			return null;
		}

		public override List<AppResult> NextSiblings ()
		{
			if (!resultIter.HasValue) {
				return null;
			}

			List<AppResult> newList = new List<AppResult> ();
			TreeIter currentIter = (TreeIter) resultIter;

			while (TModel.IterNext (ref currentIter)) {
				newList.Add (new GtkTreeModelResult (ParentWidget, TModel, Column, currentIter) { SourceQuery = this.SourceQuery });
			}

			return newList;
		}

		public override List<AppResult> Children (bool recursive = true)
		{
			if (resultIter == null || !resultIter.HasValue) {
				List<AppResult> children = new List<AppResult> ();
				TModel.Foreach ((m, p, i) => {
					children.Add (new GtkTreeModelResult (ParentWidget, TModel, Column, i));
					return false;
				});
				return children;
			}

			TreeIter currentIter = (TreeIter) resultIter;
			if (!TModel.IterHasChild (currentIter))
			{
				return null;
			}

			List<AppResult> newList = new List<AppResult> ();
			for (int i = 0; i < TModel.IterNChildren (currentIter); i++) {
				TreeIter childIter;
				if (TModel.IterNthChild (out childIter, currentIter, i)) {
					newList.Add (new GtkTreeModelResult (ParentWidget, TModel, Column, childIter));
				}
			}
			return newList;
		}

		public override bool Select ()
		{
			if (!resultIter.HasValue) {
				return false;
			}

			if (ParentWidget is TreeView) {
				TreeView treeView = (TreeView) ParentWidget;
				treeView.Selection.SelectIter ((TreeIter) resultIter);
			} else if (ParentWidget is ComboBox) {
				ComboBox comboBox = (ComboBox) ParentWidget;
				comboBox.SetActiveIter ((TreeIter) resultIter);
			}

			return true;
		}

		public override bool Click ()
		{
			if (ParentWidget is TreeView && resultIter.HasValue) {
				var path = TModel.GetPath (resultIter.Value);
				var tree = ParentWidget as TreeView;
				return tree.ExpandRow (path, true);
			}

			return false;
		}

		public override bool TypeKey (char key, string state = "")
		{
			return false;
		}

		public override bool TypeKey (string keyString, string state = "")
		{
			throw new NotImplementedException ();
		}

		public override bool EnterText (string text)
		{
			return false;
		}

		public override bool Toggle (bool active)
		{
			return false;
		}

		public override void Flash (System.Action completionHandler)
		{
			completionHandler ();
		}
	}
}

