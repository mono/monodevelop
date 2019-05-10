//
// TabsWindowOptionsPanel.cs
//
// Author:
//       jmedrano <josmed@microsoft.com>
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
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	public class TabsWindowOptionsPanel : ItemOptionsPanel
	{
		TabsWindowOptionsWidget widget;

		public TabsWindowOptionsPanel ()
		{
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}

		public override Control CreatePanelWidget ()
		{
			widget = new TabsWindowOptionsWidget (ConfiguredProject, ParentDialog);
			return new XwtControl (widget);
		}

	}

	class TabsWindowOptionsWidget : Widget
	{
		private readonly Project configuredProject;
		private readonly OptionsDialog parentDialog;
		const int margin = 12;

		CheckBox enablePinnedTabsCheckbox;

		public TabsWindowOptionsWidget (Project configuredProject, OptionsDialog parentDialog)
		{
			this.configuredProject = configuredProject;
			this.parentDialog = parentDialog;

			var mainContainer = new VBox ();
			mainContainer.PackStart (new Label { Markup = string.Format ("<b>{0}</b>", GettextCatalog.GetString ("Pinned Tabs")) });

			enablePinnedTabsCheckbox = new CheckBox () { AllowMixed = false };

			var enableTabsContainer = new HBox ();
			mainContainer.PackStart (enableTabsContainer);
			enableTabsContainer.PackStart (enablePinnedTabsCheckbox);
			enableTabsContainer.PackStart (new Label { Text = GettextCatalog.GetString ("Enable pin a tab in document bar") });

			Content = mainContainer;

			enablePinnedTabsCheckbox.State = IdeApp.Preferences.EnablePinnedTabs.Value ? CheckBoxState.On : CheckBoxState.Off;
			enablePinnedTabsCheckbox.Toggled += EnablePinnedTabsCheckbox_Toggled;

			Show ();
		}

		void EnablePinnedTabsCheckbox_Toggled (object sender, EventArgs e)
		{
			Store ();
		}

		internal void Store ()
		{
			IdeApp.Preferences.EnablePinnedTabs.Value = enablePinnedTabsCheckbox.State == CheckBoxState.On;
		}

		protected override void Dispose (bool disposing)
		{
			enablePinnedTabsCheckbox.Clicked -= EnablePinnedTabsCheckbox_Toggled;
			base.Dispose (disposing);
		}
	}

}
