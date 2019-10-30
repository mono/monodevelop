//
// Scaffolder.cs
//
// Author:
//       jasonimison <jaimison@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Linq;
using Xwt;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class ScaffolderTemplateConfigurePage : ScaffolderWizardPageBase
	{
		IScaffolder scaffolder;

		public ScaffolderTemplateConfigurePage (ScaffolderArgs args) : base (args)
		{
			scaffolder = args.Scaffolder;
			this.SubSubTitle = scaffolder.Name;
		}

		protected override Widget GetMainControl ()
		{
			var table = new Table ();
			//TODO: may as well just make Fields an array or list
			var fields = scaffolder.Fields.ToArray ();

			var rowCount = fields.Count ();
			int rowAdditionCount = 0;
			for(int fieldIndex = 0; fieldIndex < rowCount; fieldIndex++) {
				int rowIndex = fieldIndex + rowAdditionCount;
				var field = fields[fieldIndex];
				var label = new Label ();

				switch (field) {
				case StringField s:
					var input = new TextEntry ();
					//input.HeightRequest = 30;
					//label.Font = label.Font.WithSize (15);
					label.Text = s.DisplayName;
					table.Add (label, 0, rowIndex, hpos:WidgetPlacement.End);
					table.Add (input, 1, rowIndex);
					input.Changed += (sender, args) => s.SelectedValue = input.Text;
					input.SetFocus ();
					break;
				case ComboField comboField:
					ComboBox comboBox;
					if (comboField.IsEditable) {
						comboBox = new ComboBoxEntry ();
                    } else {
						comboBox = new ComboBox ();
                    }

					foreach (var option in comboField.Options) {
						comboBox.Items.Add (option);
					}

					//comboBox.HeightRequest = 35;
					//label.Font = label.Font.WithSize (15);
					label.Text = comboField.DisplayName;

					table.Add (label, 0, rowIndex, hpos:WidgetPlacement.End);
					table.Add (comboBox, 1, rowIndex);
					comboField.SelectedValue = comboField.Options.FirstOrDefault ();
					comboBox.TextInput += (sender, args) => comboField.SelectedValue = comboBox.SelectedText;

					comboBox.SelectionChanged += (sender, args) => comboField.SelectedValue = comboBox.SelectedText;
					comboBox.SelectedIndex = 0;
					break;
				case BoolFieldList boolFieldList:
					label.Text = boolFieldList.DisplayName;
					table.Add (label, 0, rowIndex, hpos:WidgetPlacement.End, vpos:WidgetPlacement.Start);
					var vbox = new VBox ();
                    for(int i = 0; i < boolFieldList.Options.Count; i++) {
						var boolField = boolFieldList.Options [i];
						var checkbox = new CheckBox (boolField.DisplayName);
						//checkbox.HeightRequest = 15;
						checkbox.Active = boolField.Selected;
						checkbox.Sensitive = boolField.Enabled;
						checkbox.Toggled += (sender, args) => boolField.Selected = checkbox.Active;
						vbox.PackStart (checkbox);
                    }
					table.Add (vbox, 1, rowIndex);
					break;
				case FileField fileField:
					var fileSelector = new FileSelector ();
					//fileSelector.HeightRequest = 40;
					//fileSelector.MinHeight = 40;
					table.Add (fileSelector, 0, rowIndex, colspan:2);
					//label.Font = label.Font.WithSize (15);
					label.Text = fileField.DisplayName;
					table.Add (label, 0, rowIndex + 1, colspan:2);
					rowAdditionCount++;
					fileSelector.FileChanged += (sender, args) => fileField.SelectedValue = fileSelector.FileName;
					break;
				}
				
			}
			return table;
		}

		public override int GetHashCode ()
		{
			// Pages are used as dictionary keys in WizardDialog.cs
			return unchecked(
				base.GetHashCode () + 37 * Args.GetHashCode ()
			);
		}
	}
}
