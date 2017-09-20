//
// ConvertToEnumDialog.cs
//
// Author:
//       Luís Reis <luiscubal@gmail.com>
//
// Copyright (c) 2013 Luís Reis
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
using System.Linq;
using MonoDevelop.Core;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CSharp.Refactoring.CodeActions
{
	class ConvertToEnumDialog : Xwt.Dialog
	{
		Xwt.TextEntry enumNameEntry;
		Xwt.ListView variableList;
		Xwt.ListStore variableStore;
		Xwt.DataField<bool> enabledField;
		Xwt.ListViewColumn enabledColumn;
		Xwt.DataField<string> oldNameField;
		Xwt.ListViewColumn oldNameColumn;
		Xwt.DataField<string> newNameField;
		Xwt.ListViewColumn newNameColumn;
		List<VariableInitializer> variables;

		public ConvertToEnumDialog(string proposedEnumName, List<VariableInitializer> variables, List<VariableInitializer> defaultActiveVariables, Dictionary<string, string> newNames) {
			this.variables = variables;

			Title = GettextCatalog.GetString("Convert fields to enumeration");

			Xwt.VBox vbox = new Xwt.VBox ();

			vbox.PackStart(new Xwt.Label(GettextCatalog.GetString("Name of enum")));

			enumNameEntry = new Xwt.TextEntry ();
			enumNameEntry.Text = proposedEnumName;
			vbox.PackStart (enumNameEntry);

			vbox.PackStart (new Xwt.Label (GettextCatalog.GetString ("Variables to include")));

			variableList = new Xwt.ListView ();
			enabledField = new Xwt.DataField<bool> ();
			oldNameField = new Xwt.DataField<string> ();
			newNameField = new Xwt.DataField<string> ();

			variableStore = new Xwt.ListStore (enabledField, oldNameField, newNameField);
			variableList.DataSource = variableStore;

			enabledColumn = new Xwt.ListViewColumn (GettextCatalog.GetString ("Included"), new Xwt.CheckBoxCellView(enabledField) { Editable = true });
			variableList.Columns.Add (enabledColumn);
			oldNameColumn = new Xwt.ListViewColumn(GettextCatalog.GetString("Field name"), new Xwt.TextCellView(oldNameField) { Editable = false });
			variableList.Columns.Add (oldNameColumn);
			newNameColumn = new Xwt.ListViewColumn(GettextCatalog.GetString("Enum name"), new Xwt.TextCellView(newNameField) { Editable = true, });
			variableList.Columns.Add (newNameColumn);

			for (int i = 0; i < variables.Count; ++i) {
				var variable = variables[i];

				variableStore.AddRow ();
				variableStore.SetValue (i, enabledField, defaultActiveVariables.Contains(variable));
				variableStore.SetValue (i, oldNameField, variable.Name);

				variableStore.SetValue (i, newNameField, newNames [variable.Name]);
			}

			vbox.PackStart (variableList, true, true);

			vbox.PackStart (new Xwt.Label (GettextCatalog.GetString ("Warning: This may take a while...")));

			Content = vbox;

			Buttons.Add (new Xwt.DialogButton (Xwt.Command.Ok));
			Buttons.Add (new Xwt.DialogButton (Xwt.Command.Cancel));
		}

		public string EnumName {
			get { return enumNameEntry.Text; }
		}

		public List<VariableInitializer> SelectedVariables
		{
			get {
				return variables.Where((variable, idx) => variableStore.GetValue<bool>(idx, enabledField)).ToList();
			}
		}

		public Dictionary<string, string> NewNames
		{
			get {
				var newNames = new Dictionary<string, string> ();

				for (int i = 0; i < variableStore.RowCount; ++i) {
					newNames[variableStore.GetValue<string>(i, oldNameField)] = variableStore.GetValue<string>(i, newNameField);
				}

				return newNames;
			}
		}
	}
}

