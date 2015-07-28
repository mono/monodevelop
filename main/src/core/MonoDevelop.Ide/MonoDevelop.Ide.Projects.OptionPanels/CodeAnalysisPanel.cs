//
// CodeAnalysisProjectPanel.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	public class CodeAnalysisPanel : MultiConfigItemOptionsPanel
	{
		CodeAnalysisPanelWidget widget;

		public CodeAnalysisPanel ()
		{
			AllowMixedConfigurations = true;
		}

		public override bool IsVisible ()
		{
			return ConfiguredProject is DotNetProject;
		}

		public override Widget CreatePanelWidget ()
		{
			return (widget = new CodeAnalysisPanelWidget ());
		}

		public override bool ValidateChanges ()
		{
			return widget.ValidateChanges ();
		}

		public override void LoadConfigData ()
		{
			widget.Load (ConfiguredProject, CurrentConfigurations);
		}

		protected override bool ConfigurationsAreEqual (IEnumerable<ItemConfiguration> configs)
		{
			bool? enabled;
			CodeAnalysisPanelWidget.GetCommonData (configs, out enabled);
			return enabled.HasValue;
		}


		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}

	class CodeAnalysisPanelWidget : VBox
	{
		ItemConfiguration [] configurations;
		CheckButton enabledCheckBox;

		public CodeAnalysisPanelWidget ()
		{
			Build ();
		}

		void Build ()
		{
			enabledCheckBox = new CheckButton ();
			enabledCheckBox.Label = GettextCatalog.GetString ("Enable Code Analysis on Build");
			this.enabledCheckBox.CanFocus = true;
			this.enabledCheckBox.DrawIndicator = true;
			this.enabledCheckBox.UseUnderline = true;
			this.PackStart (enabledCheckBox);
			ShowAll ();
		}

		public void Load (Project project, ItemConfiguration [] configs)
		{
			this.configurations = configs;
			bool? enabled;

			GetCommonData (configs, out enabled);

			if (enabled.HasValue) {
				enabledCheckBox.Inconsistent = false;
				enabledCheckBox.Active = enabled.Value;
			} else {
				enabledCheckBox.Inconsistent = true;
			}
		}

		internal static void GetCommonData (IEnumerable<ItemConfiguration> configs, out bool? enabled)
		{
			enabled = null;

			foreach (DotNetProjectConfiguration conf in configs) {
				var runCodeAnalysisEnabled = conf.ExtendedProperties.Contains ("RunCodeAnalysis") ? (bool)conf.ExtendedProperties ["RunCodeAnalysis"] : false;
				if (!enabled.HasValue) {
					enabled = runCodeAnalysisEnabled;
				} else if (enabled.Value != runCodeAnalysisEnabled) {
					//Different values between different configs, reuturn null as inconsistant
					enabled = null;
					return;
				}
			}
		}

		public bool ValidateChanges ()
		{
			return true;
		}

		public void Store ()
		{
			if (configurations == null)
				return;
			//If Inconsistent, don't do anything
			if (!enabledCheckBox.Inconsistent) {
				foreach (DotNetProjectConfiguration conf in configurations) {
					conf.ExtendedProperties ["RunCodeAnalysis"] = enabledCheckBox.Active;
				}
			}
		}
	}
}

