// 
// DebuggerOptionsPanelWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using Mono.Debugging.Client;

using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Debugger
{
	public class DebuggerOptionsPanel : OptionsPanel
	{
		DebuggerOptionsPanelWidget w;

		public override Control CreatePanelWidget ()
		{
			w = new DebuggerOptionsPanelWidget ();

			return (Gtk.Widget)Toolkit.CurrentEngine.GetNativeWidget (w);
		}

		public override void ApplyChanges ()
		{
			w.Store ();
		}
	}

	public class DebuggerOptionsPanelWidget : VBox
	{
		DebuggerSessionOptions options;
		CheckBox checkStepIntoExternalCode;
		ComboBox comboAutomaticSourceDownload;
		CheckBox checkStepOverPropertiesAndOperators;
		CheckBox checkAllowEval;
		CheckBox checkAllowToString;
		CheckBox checkShowBaseGroup;
		CheckBox checkGroupPrivate;
		CheckBox checkGroupStatic;
		SpinButton spinTimeout;
		CheckBox enableLogging;
		CheckBox useNewTreeView;
		Label evalLabel;

		void Build ()
		{
			PackStart (new Label { Markup = "<b>" + GettextCatalog.GetString ("Scope") + "</b>" });
			checkStepOverPropertiesAndOperators = new CheckBox (GettextCatalog.GetString ("Step over properties and operators"));
			PackStart (checkStepOverPropertiesAndOperators);
			checkStepIntoExternalCode = new CheckBox (GettextCatalog.GetString ("Step into external code")) {
				ExpandVertical = false,
				MarginBottom = 0,
				HeightRequest = 15
			};
			PackStart (checkStepIntoExternalCode);

			var label = new Label {
				Text = GettextCatalog.GetString ("The debugger will step into code and hit exceptions in dependencies that aren’t considered part of your project, like packages and references."),
				Font = IdeServices.FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11).ToXwtFont (),
				TextColor = Ide.Gui.Styles.SecondaryTextColor,
				MarginLeft = 30,
				Wrap = WrapMode.Word,
				WidthRequest = 400,
				HeightRequest = 20,
				ExpandVertical = false
			};

			PackStart (label, hpos:WidgetPlacement.Start);
			var autodownloadHBox = new HBox { MarginLeft = 50 };

			var downloadLabel = new Label (GettextCatalog.GetString ("Download External Code:"));
			autodownloadHBox.PackStart (downloadLabel);
			comboAutomaticSourceDownload = new ComboBox ();
			comboAutomaticSourceDownload.Items.Add(AutomaticSourceDownload.Ask, GettextCatalog.GetString ("Ask"));
			comboAutomaticSourceDownload.Items.Add(AutomaticSourceDownload.Always, GettextCatalog.GetString ("Always"));
			comboAutomaticSourceDownload.Items.Add(AutomaticSourceDownload.Never, GettextCatalog.GetString ("Never"));
			autodownloadHBox.PackStart (comboAutomaticSourceDownload);
			PackStart (autodownloadHBox);

			checkStepIntoExternalCode.Toggled += (sender, obj) => comboAutomaticSourceDownload.Sensitive = checkStepIntoExternalCode.Active;
			PackStart (new Label { Markup = "<b>" + GettextCatalog.GetString ("Inspection") + "</b>", MarginTop=25 });
			checkAllowEval = new CheckBox (GettextCatalog.GetString ("Allow implicit property evaluation and method invocation"));
			checkAllowEval.Toggled += OnCheckAllowEvalToggled;
			PackStart (checkAllowEval);
			checkAllowToString = new CheckBox (GettextCatalog.GetString ("Call string-conversion function on objects in variables windows"));
			checkAllowToString.MarginLeft = 18;
			PackStart (checkAllowToString);
			checkShowBaseGroup = new CheckBox (GettextCatalog.GetString ("Show inherited class members in a base class group"));
			PackStart (checkShowBaseGroup);
			checkGroupPrivate = new CheckBox (GettextCatalog.GetString ("Group non-public members"));
			PackStart (checkGroupPrivate);
			checkGroupStatic = new CheckBox (GettextCatalog.GetString ("Group static members"));
			PackStart (checkGroupStatic);
			var evalBox = new HBox ();
			evalLabel = new Label (GettextCatalog.GetString ("Evaluation Timeout:"));
			evalBox.PackStart (evalLabel);
			spinTimeout = new SpinButton ();
			spinTimeout.ClimbRate = 100;
			spinTimeout.Digits = 0;
			spinTimeout.IncrementValue = 100;
			spinTimeout.MaximumValue = 1000000;
			spinTimeout.MinimumValue = 0;
			spinTimeout.Wrap = false;
			spinTimeout.WidthRequest = 80;
			evalBox.PackStart (spinTimeout);
			evalBox.PackStart (new Label (GettextCatalog.GetString ("ms")));
			PackStart (evalBox);
			PackStart (new Label { Markup = "<b>" + GettextCatalog.GetString ("Advanced options") + "</b>", MarginTop=25 });
			enableLogging = new CheckBox (GettextCatalog.GetString ("Enable diagnostic logging", BrandingService.ApplicationName));
			PackStart (enableLogging);
			useNewTreeView = new CheckBox (GettextCatalog.GetString ("Use the new Locals/Watch window tree view"));
			PackStart (useNewTreeView);

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			checkStepIntoExternalCode.SetCommonAccessibilityAttributes ("DebuggerPanel.projectCodeOnly", "",
			                                                       GettextCatalog.GetString ("Check to step into framework code"));
			checkStepOverPropertiesAndOperators.SetCommonAccessibilityAttributes ("DebuggerPanel.stepOverProperties", "",
			                                                                      GettextCatalog.GetString ("Check to step over properties and operators"));
			checkAllowEval.SetCommonAccessibilityAttributes ("DebuggerPanel.allowEval", "",
			                                                 GettextCatalog.GetString ("Check to allow implicit property evaluation and method invocation"));
			checkAllowToString.SetCommonAccessibilityAttributes ("DebuggerPanel.allowToString", "",
			                                                     GettextCatalog.GetString ("Check to call string-conversion functions on objects in the Variables windows"));
			checkShowBaseGroup.SetCommonAccessibilityAttributes ("DebuggerPanel.showBaseGroup", "",
			                                                     GettextCatalog.GetString ("Check to show inherited class members in a base class group"));
			checkGroupPrivate.SetCommonAccessibilityAttributes ("DebuggerPanel.groupPrivate", "",
			                                                    GettextCatalog.GetString ("Check to group non-public members in the Variables windows"));
			checkGroupStatic.SetCommonAccessibilityAttributes ("DebuggerPanel.groupStatic", "",
			                                                   GettextCatalog.GetString ("Check to group static members in the Variables windows"));
			spinTimeout.SetCommonAccessibilityAttributes ("DebuggerPanel.timeout", evalLabel,
			                                              GettextCatalog.GetString ("Set the length of time the evaluation will wait before giving up"));
			enableLogging.SetCommonAccessibilityAttributes ("DebuggerPanel.enableLogging", "",
			                                                GettextCatalog.GetString ("Check to enable some diagnostic logging"));
		}

		public DebuggerOptionsPanelWidget ()
		{
			Build ();

			options = DebuggingService.GetUserOptions ();
			checkStepIntoExternalCode.Active = !options.ProjectAssembliesOnly;
			comboAutomaticSourceDownload.SelectedItem = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.AutomaticSourceDownload", AutomaticSourceDownload.Ask);

			checkStepOverPropertiesAndOperators.Active = options.StepOverPropertiesAndOperators;
			checkAllowEval.Active = options.EvaluationOptions.AllowTargetInvoke;
			checkAllowToString.Active = options.EvaluationOptions.AllowToStringCalls;
			checkShowBaseGroup.Active = !options.EvaluationOptions.FlattenHierarchy;
			checkGroupPrivate.Active = options.EvaluationOptions.GroupPrivateMembers;
			checkGroupStatic.Active = options.EvaluationOptions.GroupStaticMembers;
			checkAllowToString.Sensitive = checkAllowEval.Active;
			spinTimeout.Value = options.EvaluationOptions.EvaluationTimeout;
			enableLogging.Active = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.DebuggerLogging", false);
			useNewTreeView.Active = PropertyService.Get ("MonoDevelop.Debugger.UseNewTreeView", false);
		}

		public void Store ()
		{
			var ops = options.EvaluationOptions;

			ops.AllowTargetInvoke = checkAllowEval.Active;
			ops.AllowToStringCalls = checkAllowToString.Active;
			ops.FlattenHierarchy = !checkShowBaseGroup.Active;
			ops.GroupPrivateMembers = checkGroupPrivate.Active;
			ops.GroupStaticMembers = checkGroupStatic.Active;
			ops.EvaluationTimeout = (int)spinTimeout.Value;

			options.StepOverPropertiesAndOperators = checkStepOverPropertiesAndOperators.Active;
			options.ProjectAssembliesOnly = !checkStepIntoExternalCode.Active;
			options.AutomaticSourceLinkDownload = (AutomaticSourceDownload)comboAutomaticSourceDownload.SelectedItem;
			options.EvaluationOptions = ops;

			DebuggingService.SetUserOptions (options);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.DebuggerLogging", enableLogging.Active);
			PropertyService.Set ("MonoDevelop.Debugger.UseNewTreeView", useNewTreeView.Active);
		}

		protected virtual void OnCheckAllowEvalToggled (object sender, EventArgs e)
		{
			checkAllowToString.Sensitive = checkAllowEval.Active;
		}
	}
}
