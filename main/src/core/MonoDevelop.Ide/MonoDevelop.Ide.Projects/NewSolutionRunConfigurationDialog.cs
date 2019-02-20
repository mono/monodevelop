//
// NewSolutionRunConfigurationDialog.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019 
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

namespace MonoDevelop.Ide.Projects
{
	public class NewSolutionRunConfigurationDialog : Dialog
	{
		readonly TextEntry runConfigNameEntry;
		readonly DialogButton createButton;

		public NewSolutionRunConfigurationDialog ()
		{
			Title = GettextCatalog.GetString ("Create Solution Run Configuration");
			Resizable = false;

			var box = new VBox ();
			Content = box;

			var label = new Label (GettextCatalog.GetString ("Solution run configurations let you run multiple projects at once. Please provide a name to\nbe shown in the toolbar for this Solution run configuration.")) {
				CanGetFocus = false,
				Wrap = WrapMode.Word
			};
			box.PackStart (label, expand: true, marginBottom: 12);

			var hbox = new HBox ();
			hbox.PackStart (new Label (GettextCatalog.GetString ("Run configuration name:")) {
				CanGetFocus = false
			});

			runConfigNameEntry = new TextEntry {
				Text = GettextCatalog.GetString ("Multiple Projects")
			};
			runConfigNameEntry.Changed += OnConfigNameChanged;
			hbox.PackStart (runConfigNameEntry, true, true);

			box.PackStart (hbox, true, true);

			createButton = new DialogButton (new Command ("create", GettextCatalog.GetString ("Create Run Configuration")));
			Buttons.Add (Command.Cancel);
			Buttons.Add (createButton);
			DefaultCommand = createButton.Command;
		}

		public string RunConfigurationName => runConfigNameEntry.Text;

		void OnConfigNameChanged (object sender, EventArgs e)
		{
			createButton.Sensitive = !string.IsNullOrEmpty (runConfigNameEntry.Text);
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd.Id == createButton.Command.Id) {
				Respond (cmd);
				return;
			}
			base.OnCommandActivated (cmd);
		}

		protected override void OnShown ()
		{
			base.OnShown ();
			runConfigNameEntry.SetFocus ();
		}

		protected override void Dispose (bool disposing)
		{
			runConfigNameEntry.Changed -= OnConfigNameChanged;
			base.Dispose (disposing);
		}
	}
}
