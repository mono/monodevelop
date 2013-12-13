//
// CombineMSBuildWidget.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2013 Marius Ungureanu
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.Formats.MSBuild;
using MonoDevelop.Core;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	class CombineMSBuildOptions : ItemOptionsPanel
	{
		Xwt.CheckBox checkMSBuild;

		public override Gtk.Widget CreatePanelWidget ()
		{
			var box = new Xwt.VBox ();
			box.Spacing = 6;
			box.Margin = 12;

			bool byDefault, require;
			MSBuildProjectService.CheckHandlerUsesMSBuildEngine (ConfiguredProject, out byDefault, out require);
			if (require) {
				box.Visible = false;
				return box.ToGtkWidget ();
			}

			box.PackStart (new Xwt.Label {
				Markup = "<b>Build Engine</b>"
			});

			checkMSBuild = new Xwt.CheckBox (byDefault ?
				GettextCatalog.GetString ("Use MSBuild build engine (recommended for this project type)") :
				GettextCatalog.GetString ("Use MSBuild build engine (unsupported for this project type)")) {
				Active = ConfiguredProject.UseMSBuildEngine ?? byDefault
			};
			var hbox = new Xwt.HBox {
				MarginLeft = 18,
				Spacing = 6
			};
			hbox.PackStart (checkMSBuild);
			box.PackStart (hbox);
			box.Show ();
			return box.ToGtkWidget ();
		}

		public override void ApplyChanges ()
		{
			bool byDefault, require;
			MSBuildProjectService.CheckHandlerUsesMSBuildEngine (ConfiguredProject, out byDefault, out require);
			if (!require) {
				var active = checkMSBuild.Active;
				if (active == byDefault)
					ConfiguredProject.UseMSBuildEngine = null;
				else
					ConfiguredProject.UseMSBuildEngine = active;
			}
		}
	}
}

