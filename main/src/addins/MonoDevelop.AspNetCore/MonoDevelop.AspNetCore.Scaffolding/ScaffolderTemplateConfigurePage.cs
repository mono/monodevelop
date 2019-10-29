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
			var vbox = new VBox ();
			foreach (var field in scaffolder.Fields) {
				var hbox = new HBox ();
				var label = new Label ();

				switch (field) {
				case StringField s:
					var input = new TextEntry ();
					input.HeightRequest = 30;
					hbox.PackEnd (input);
					label.Font = label.Font.WithSize (15);
					label.Text = s.DisplayName;
					hbox.PackEnd (label);
					vbox.PackStart (hbox);
					input.Changed += (sender, args) => s.SelectedValue = input.Text;
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

					comboBox.HeightRequest = 30;
					hbox.PackEnd (comboBox);
					label.Font = label.Font.WithSize (15);
					label.Text = comboField.DisplayName;
					hbox.PackEnd (label);
					vbox.PackStart (hbox);

					comboField.SelectedValue = comboField.Options.FirstOrDefault ();
					comboBox.TextInput += (sender, args) => comboField.SelectedValue = comboBox.SelectedText;

					comboBox.SelectionChanged += (sender, args) => comboField.SelectedValue = comboBox.SelectedText;
					comboBox.SelectedIndex = 0;
					break;
				}
			}
			return vbox;
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
