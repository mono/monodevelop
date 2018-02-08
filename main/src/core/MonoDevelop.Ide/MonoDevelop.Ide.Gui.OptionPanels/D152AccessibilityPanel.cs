//
// D152AccessibilityPanel.cs
//
// Author:
//       iain <>
//
// Copyright (c) 2017 
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

#if MAC
using System;
using MonoDevelop.Components;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;

using Gtk;

using Foundation;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	public class D152AccessibilityPanel : OptionsPanel
	{
		D152AccessibilityPanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			return widget = new D152AccessibilityPanelWidget ();
		}

		public override void ApplyChanges ()
		{
			widget.Save ();
		}
	}

	class D152AccessibilityPanelWidget : VBox
	{
		bool originalSetting;
		CheckButton enabled;

		HSeparator separatorRestart;
		Table tableRestart;
		Button btnRestart;
		ImageView imageRestart;
		Label labelRestart;

		static string EnabledKey = "com.monodevelop.AccessibilityEnabled";

		public D152AccessibilityPanelWidget () : base (false, 6)
		{
			NSUserDefaults defaults = NSUserDefaults.StandardUserDefaults;

			enabled = new CheckButton ("Enable Accessibility");
			enabled.Active = originalSetting = defaults.BoolForKey (EnabledKey);
			enabled.Visible = true;

			enabled.Toggled += ShowQuitOption;
			PackStart (enabled, false, false, 0);

			separatorRestart = new HSeparator ();
			PackStart (this.separatorRestart, false, false, 0);

			tableRestart = new Table (2, 3, false);
			tableRestart.RowSpacing = 6;
			tableRestart.ColumnSpacing = 6;

			btnRestart = new Button ();
			btnRestart.CanFocus = true;
			btnRestart.UseUnderline = true;
			btnRestart.Clicked += RestartClicked;

			tableRestart.Attach (btnRestart, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

			imageRestart = new global::MonoDevelop.Components.ImageView ();
			imageRestart.IconId = "md-information";
			imageRestart.IconSize = ((global::Gtk.IconSize)(1));

			tableRestart.Attach (imageRestart, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			labelRestart = new global::Gtk.Label ();
			tableRestart.Attach (labelRestart, 1, 3, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

			PackStart (tableRestart, false, false, 0);

			labelRestart.LabelProp = GettextCatalog.GetString ("These preferences will take effect next time you start {0}", BrandingService.ApplicationName);
			btnRestart.Label = GettextCatalog.GetString ("Restart {0}", BrandingService.ApplicationName);

			Visible = true;
		}

		public void Save ()
		{
			NSUserDefaults defaults = NSUserDefaults.StandardUserDefaults;
			defaults.SetBool (enabled.Active, EnabledKey);
			defaults.Synchronize ();
		}

		void ShowQuitOption (object sender, EventArgs args)
		{
			if (enabled.Active != originalSetting) {
				tableRestart.ShowAll ();
				separatorRestart.Show ();
			} else {
				tableRestart.Hide ();
				separatorRestart.Hide ();
			}
		}

		void RestartClicked (object sender, System.EventArgs e)
		{
			Save ();
			IdeApp.Restart (true);
		}
	}
}

#endif
