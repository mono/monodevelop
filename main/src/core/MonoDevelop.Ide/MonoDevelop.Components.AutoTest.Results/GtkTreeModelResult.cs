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
	public class GtkTreeModelResult : AppResult
	{
		Widget ParentWidget;
		TreeModel TModel;
		int Column;
		TreeIter? resultIter;
		string DesiredText;

		public GtkTreeModelResult (Widget parent, TreeModel treeModel, int column)
		{
			ParentWidget = parent;
			TModel = treeModel;
			Column = column;
		}

		public GtkTreeModelResult (Widget parent, TreeModel treeModel, int column, TreeIter iter)
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
				newList.Add (new GtkTreeModelResult (ParentWidget, TModel, Column, currentIter));
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
			// FIXME: Same as select?
			return true;
		}

		public override bool TypeKey (char key, string state)
		{
			return false;
		}

		public override bool EnterText (string text)
		{
			return false;
		}

		public override bool Toggle (bool active)
		{
			return false;
		}
	}
}

