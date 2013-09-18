//
// CompletionCharactersPanel.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using Xwt;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.CodeCompletion;
using System.Collections.Generic;

namespace MonoDevelop.SourceEditor.OptionPanels
{
	public class CompletionCharactersPanel : VBox, IOptionsPanel
	{
		ListView list;
		ListStore store;

		DataField<string> language = new DataField<string> ();
		DataField<bool> completeOnSpace = new DataField<bool> ();
		DataField<string> completeOnChars = new DataField<string> ();

		#region IOptionsPanel implementation

		void IOptionsPanel.Initialize (OptionsDialog dialog, object dataObject)
		{
			this.ExpandHorizontal = true;
			this.ExpandVertical = true;
			this.HeightRequest = 400;
			list = new ListView ();
			store = new ListStore (language, completeOnSpace, completeOnChars);

			var languageColumn = list.Columns.Add (GettextCatalog.GetString ("Language"), language);
			languageColumn.CanResize = true;

			var checkBoxCellView = new CheckBoxCellView (completeOnSpace);
			checkBoxCellView.Editable = true;
			var completeOnSpaceColumn = list.Columns.Add (GettextCatalog.GetString ("Complete on space"), checkBoxCellView);
			completeOnSpaceColumn.CanResize = true;

			var textCellView = new TextCellView (completeOnChars);
			textCellView.Editable = true;
			var doNotCompleteOnColumn = list.Columns.Add (GettextCatalog.GetString ("Do complete on"), textCellView);
			doNotCompleteOnColumn.CanResize = true;
			list.DataSource = store;
			PackStart (list, true, true);

			var hbox = new HBox ();
			var button = new Button ("Reset to default");
			button.Clicked += delegate {
				FillStore (CompletionCharacters.GetDefaultCompletionCharacters ());
			};	
			hbox.PackEnd (button, false, false);
			PackEnd (hbox, false, true);
			FillStore (CompletionCharacters.GetCompletionCharacters ());
		}

		void FillStore (IEnumerable<CompletionCharacters> completionCharacters)
		{
			store.Clear ();
			foreach (var c in completionCharacters) {
				var row = store.AddRow ();
				store.SetValue (row, language, c.Language);
				store.SetValue (row, completeOnSpace, c.CompleteOnSpace);
				store.SetValue (row, completeOnChars, c.CompleteOnChars);
			}
		}

		Gtk.Widget IOptionsPanel.CreatePanelWidget ()
		{
			return (Gtk.Widget)Xwt.Toolkit.CurrentEngine.GetNativeWidget (this);
		}

		bool IOptionsPanel.IsVisible ()
		{
			return true;
		}

		bool IOptionsPanel.ValidateChanges ()
		{
			return true;
		}

		void IOptionsPanel.ApplyChanges ()
		{
			var chars = new CompletionCharacters[store.RowCount];
			for (int i = 0; i < chars.Length; i++) {
				chars [i] = new CompletionCharacters (
					store.GetValue (i, language),
					store.GetValue (i, completeOnSpace),
					store.GetValue (i, completeOnChars)
				);
				Console.WriteLine (chars[i]);
			}
			CompletionCharacters.SetCompletionCharacters (chars);
		}

		#endregion
	}
}

