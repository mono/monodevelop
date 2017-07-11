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
using Mono.Debugging.Client;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Gui.Dialogs;
using Xwt;
using MonoDevelop.Core;

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
		CheckBox checkProjectCodeOnly;
		CheckBox checkStepOverPropertiesAndOperators;
		CheckBox checkAllowEval;
		CheckBox checkAllowToString;
		CheckBox checkShowBaseGroup;
		CheckBox checkGroupPrivate;
		CheckBox checkGroupStatic;
		SpinButton spinTimeout;
		CheckBox enableLogging;
		Label evalLabel;

		void Build ()
		{
			checkProjectCodeOnly = new CheckBox (GettextCatalog.GetString ("Debug project code only; do not step into framework code."));
			PackStart (checkProjectCodeOnly);
			checkStepOverPropertiesAndOperators = new CheckBox (GettextCatalog.GetString ("Step over properties and operators"));
			PackStart (checkStepOverPropertiesAndOperators);
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
			PackStart (new Label () {
				Markup = "<b>" + GettextCatalog.GetString ("Advanced options") + "</b>"
			});
			enableLogging = new CheckBox (GettextCatalog.GetString ("Enable diagnostic logging", BrandingService.ApplicationName));
			PackStart (enableLogging);

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			checkProjectCodeOnly.SetCommonAccessibilityAttributes ("DebuggerPanel.projectCodeOnly", "",
			                                                       GettextCatalog.GetString ("Check to only debug the project code and not step into framework code"));
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
			checkProjectCodeOnly.Active = options.ProjectAssembliesOnly;
			checkStepOverPropertiesAndOperators.Active = options.StepOverPropertiesAndOperators;
			checkAllowEval.Active = options.EvaluationOptions.AllowTargetInvoke;
			checkAllowToString.Active = options.EvaluationOptions.AllowToStringCalls;
			checkShowBaseGroup.Active = !options.EvaluationOptions.FlattenHierarchy;
			checkGroupPrivate.Active = options.EvaluationOptions.GroupPrivateMembers;
			checkGroupStatic.Active = options.EvaluationOptions.GroupStaticMembers;
			checkAllowToString.Sensitive = checkAllowEval.Active;
			spinTimeout.Value = options.EvaluationOptions.EvaluationTimeout;
			enableLogging.Active = PropertyService.Get ("MonoDevelop.Debugger.DebuggingService.DebuggerLogging", false);
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
			options.ProjectAssembliesOnly = checkProjectCodeOnly.Active;
			options.EvaluationOptions = ops;

			DebuggingService.SetUserOptions (options);
			PropertyService.Set ("MonoDevelop.Debugger.DebuggingService.DebuggerLogging", enableLogging.Active);
		}

		protected virtual void OnCheckAllowEvalToggled (object sender, System.EventArgs e)
		{
			checkAllowToString.Sensitive = checkAllowEval.Active;
		}
	}
}
