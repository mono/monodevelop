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
using MonoDevelop.Ide.Gui.Dialogs;
using Xwt;
using MonoDevelop.Core;

namespace MonoDevelop.Debugger
{
	public class DebuggerOptionsPanel : OptionsPanel
	{
		DebuggerOptionsPanelWidget w;

		public override Gtk.Widget CreatePanelWidget ()
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
			var hbox = new HBox ();
			hbox.PackStart (new Label (GettextCatalog.GetString ("Evaluation Timeout:")));
			spinTimeout = new SpinButton ();
			spinTimeout.ClimbRate = 100;
			spinTimeout.Digits = 0;
			spinTimeout.IncrementValue = 100;
			spinTimeout.MaximumValue = 1000000;
			spinTimeout.MinimumValue = 0;
			spinTimeout.Wrap = false;
			spinTimeout.WidthRequest = 80;
			hbox.PackStart (spinTimeout);
			hbox.PackStart (new Label (GettextCatalog.GetString ("ms")));
			PackStart (hbox);
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
		}

		protected virtual void OnCheckAllowEvalToggled (object sender, System.EventArgs e)
		{
			checkAllowToString.Sensitive = checkAllowEval.Active;
		}
	}
}
