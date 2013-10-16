//
// BreakpointsPropertiesDialog.cs
//
// Author:
//       Therzok <teromario@yahoo.com>
//
// Copyright (c) 2013 Therzok
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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.Debugger
{
	enum ConditionalHitWhen {
		ConditionIsTrue,
		ExpressionChanges
	}

	class BreakpointPropertiesDialog2 : Xwt.Dialog
	{
		// For button sensitivity.
		Xwt.DialogButton buttonOk;

		// Groupings for sensitivity
		Xwt.HBox hboxFunction = new Xwt.HBox ();
		Xwt.HBox hboxLocation = new Xwt.HBox ();
		Xwt.HBox hboxLineColumn = new Xwt.HBox ();
		Xwt.HBox hboxException = new Xwt.HBox ();

		// Stop-type radios.
		readonly Xwt.RadioButton stopOnFunction = new Xwt.RadioButton (GettextCatalog.GetString ("When a function is entered"));
		readonly Xwt.RadioButton stopOnLocation = new Xwt.RadioButton (GettextCatalog.GetString ("When a location is reached"));
		readonly Xwt.RadioButton stopOnException = new Xwt.RadioButton (GettextCatalog.GetString ("When an exception is thrown"));

		// Text entries
		readonly Xwt.TextEntry entryFunctionName = new Xwt.TextEntry ();
		readonly Xwt.TextEntry entryLocationFile = new Xwt.TextEntry ();
		readonly Xwt.TextEntry entryLocationLine = new Xwt.TextEntry ();
		readonly Xwt.TextEntry entryLocationColumn = new Xwt.TextEntry ();
		readonly Xwt.TextEntry entryExceptionType = new Xwt.TextEntry ();
		readonly Xwt.TextEntry entryConditionalExpression = new Xwt.TextEntry ();
		readonly Xwt.TextEntry entryPrintExpression = new Xwt.TextEntry ();

		// Warning icon
		readonly Xwt.ImageView warningFunction = new Xwt.ImageView (Xwt.StockIcons.Warning);
		readonly Xwt.ImageView warningLocation = new Xwt.ImageView (Xwt.StockIcons.Warning);
		readonly Xwt.ImageView warningException = new Xwt.ImageView (Xwt.StockIcons.Warning);
		readonly Xwt.ImageView warningCondition = new Xwt.ImageView (Xwt.StockIcons.Warning);

		// Combobox + Pager
		readonly Xwt.SpinButton ignoreHitCount = new Xwt.SpinButton ();
		readonly Xwt.ComboBox ignoreHitType = new Xwt.ComboBox ();
		readonly Xwt.ComboBox conditionalHitType = new Xwt.ComboBox ();

		// Optional checkboxes.
		readonly Xwt.CheckBox checkIncludeSubclass = new Xwt.CheckBox (GettextCatalog.GetString ("Include subclasses"));
		readonly Xwt.CheckBox checkPrintExpression = new Xwt.CheckBox (GettextCatalog.GetString ("Print an expression to the debugger output"));
		readonly Xwt.CheckBox checkResumeExecution = new Xwt.CheckBox (GettextCatalog.GetString ("Resume execution automatically"));

		BreakEvent be;
		string[] parsedParamTypes;
		string parsedFunction;
		readonly HashSet<string> classes = new HashSet<string> ();

		public BreakpointPropertiesDialog2 (BreakEvent be)
		{
			this.be = be;

			LoadExceptionList ();
			Initialize ();
			SetInitialData ();
			SetLayout ();
		}

		void Initialize ()
		{
			// TODO: Make dialog for exceptions.
			Title = GettextCatalog.GetString (be == null ? "New Breakpoint" : "Breakpoint Properties");
			var buttonLabel = GettextCatalog.GetString (be == null ? "Add breakpoint" : "Modify breakpoint");

			var stopGroup = new Xwt.RadioButtonGroup ();
			stopOnFunction.Group = stopGroup;
			stopOnLocation.Group = stopGroup;
			stopOnException.Group = stopGroup;

			ignoreHitType.Items.Add (HitCountMode.None, GettextCatalog.GetString ("always"));
			ignoreHitType.Items.Add (HitCountMode.LessThan, GettextCatalog.GetString ("when hit count is less than"));
			ignoreHitType.Items.Add (HitCountMode.LessThanOrEqualTo, GettextCatalog.GetString ("when hit count is less than or equal to"));
			ignoreHitType.Items.Add (HitCountMode.EqualTo, GettextCatalog.GetString ("when hit count is equal to"));
			ignoreHitType.Items.Add (HitCountMode.GreaterThan, GettextCatalog.GetString ("when hit count is greater than"));
			ignoreHitType.Items.Add (HitCountMode.GreaterThanOrEqualTo, GettextCatalog.GetString ("when hit count is greater than or equal to"));
			ignoreHitType.Items.Add (HitCountMode.MultipleOf, GettextCatalog.GetString ("when hit count is a multiple of"));

			ignoreHitCount.IncrementValue = 1;

			conditionalHitType.Items.Add (ConditionalHitWhen.ConditionIsTrue, GettextCatalog.GetString ("is true"));
			conditionalHitType.Items.Add (ConditionalHitWhen.ExpressionChanges, GettextCatalog.GetString ("expression changes"));

			buttonOk = new Xwt.DialogButton (buttonLabel, Xwt.Command.Ok) {
				Sensitive = false
			};

			// Register events.
			stopGroup.ActiveRadioButtonChanged += OnUpdateControls;
			entryFunctionName.Changed += OnUpdateControls;
			entryLocationFile.Changed += OnUpdateControls;
			entryLocationLine.Changed += OnUpdateControls;
			entryLocationColumn.Changed += OnUpdateControls;

			entryConditionalExpression.Changed += OnUpdateControls;
			ignoreHitType.SelectionChanged += OnUpdateControls;
			checkPrintExpression.Toggled += OnUpdateControls;

			buttonOk.Clicked += OnSave;
		}

		void SetInitialFunctionBreakpointData (FunctionBreakpoint fb)
		{
			stopOnFunction.Active = true;
			if (fb.ParamTypes != null) {
				// FIXME: support non-C# syntax based on fb.Language
				entryFunctionName.Text = fb.FunctionName + " (" + String.Join (", ", fb.ParamTypes) + ")";
			} else
				entryFunctionName.Text = fb.FunctionName;
		}

		void SetInitialBreakpointData (Breakpoint bp)
		{
			stopOnLocation.Active = true;
			entryLocationFile.Text = bp.FileName;
			entryLocationLine.Text = bp.Line.ToString ();
			entryLocationColumn.Text = bp.Column.ToString ();

			if (!String.IsNullOrEmpty (bp.ConditionExpression)) {
				entryConditionalExpression.Text = bp.ConditionExpression;
				conditionalHitType.SelectedItem = bp.BreakIfConditionChanges ? ConditionalHitWhen.ExpressionChanges : ConditionalHitWhen.ConditionIsTrue;
			}

			Project project = null;
			if (!String.IsNullOrEmpty (bp.FileName))
				project = IdeApp.Workspace.GetProjectContainingFile (bp.FileName);

			if (project != null) {
				// Check the startup project of the solution too, since the current project may be a library
				SolutionEntityItem startup = project.ParentSolution.StartupItem;
				entryConditionalExpression.Sensitive = DebuggingService.IsFeatureSupported (project, DebuggerFeatures.ConditionalBreakpoints) ||
				                                       DebuggingService.IsFeatureSupported (startup, DebuggerFeatures.ConditionalBreakpoints);

				bool canTrace = DebuggingService.IsFeatureSupported (project, DebuggerFeatures.Tracepoints) ||
				                DebuggingService.IsFeatureSupported (startup, DebuggerFeatures.Tracepoints);

				checkPrintExpression.Sensitive = canTrace;
				entryPrintExpression.Sensitive = canTrace;
			}
		}

		void SetInitialCatchpointData (Catchpoint cp)
		{
			entryExceptionType.Text = cp.ExceptionName;
		}

		void SetInitialData ()
		{
			if (be != null) {
				stopOnException.Sensitive = false;
				stopOnFunction.Sensitive = false;
				stopOnLocation.Sensitive = false;
				entryLocationFile.ReadOnly = true;
				entryLocationLine.ReadOnly = true;
				entryLocationColumn.ReadOnly = true;
				entryExceptionType.ReadOnly = true;

				ignoreHitType.SelectedItem = be.HitCountMode;
				ignoreHitCount.Value = be.HitCount;

				if (be.HitAction == HitAction.PrintExpression) {
					checkPrintExpression.Active = true;
					entryPrintExpression.Text = be.TraceExpression;
				}
			} else {
				if (IdeApp.Workbench.ActiveDocument != null) {
					entryLocationFile.Text = IdeApp.Workbench.ActiveDocument.FileName;
					entryLocationLine.Text = IdeApp.Workbench.ActiveDocument.Editor.Caret.Line.ToString ();
					entryLocationColumn.Text = IdeApp.Workbench.ActiveDocument.Editor.Caret.Column.ToString ();
				}

				ignoreHitType.SelectedItem = HitCountMode.None;
				conditionalHitType.SelectedItem = ConditionalHitWhen.ConditionIsTrue;
			}

			var fb = be as FunctionBreakpoint;
			if (fb != null)
				SetInitialFunctionBreakpointData (fb);

			var bp = be as Breakpoint;
			if (bp != null)
				SetInitialBreakpointData (bp);

			var cp = be as Catchpoint;
			if (cp != null)
				SetInitialCatchpointData (cp);
		}

		void SaveFunctionBreakpoint (FunctionBreakpoint fb)
		{
			fb.FunctionName = parsedFunction;
			fb.ParamTypes = parsedParamTypes;
		}

		void SaveBreakpoint (Breakpoint bp, bool isNew)
		{
			if (isNew) {
				bp.SetColumn (Int32.Parse (entryLocationColumn.Text));
				bp.SetLine (Int32.Parse (entryLocationLine.Text));
			}

			if (!String.IsNullOrEmpty (entryConditionalExpression.Text)) {
				bp.ConditionExpression = entryConditionalExpression.Text;
				bp.BreakIfConditionChanges = conditionalHitType.SelectedItem.Equals (ConditionalHitWhen.ExpressionChanges);
			} else
				bp.ConditionExpression = null;
		}

		void SaveCatchpoint (Catchpoint cp)
		{
			if (checkIncludeSubclass.Active) {
				// add stuff
			} else {
				// remove stuff
			}
		}

		void OnSave (object sender, EventArgs e)
		{
			bool isNew = false;
			if (be == null) {
				isNew = true;

				if (stopOnFunction.Active)
					be = new FunctionBreakpoint ("", "C#");
				else if (stopOnLocation.Active)
					be = new Breakpoint (entryLocationFile.Text, Int32.Parse (entryLocationLine.Text), Int32.Parse (entryLocationColumn.Text));
				else if (stopOnException.Active)
					be = new Catchpoint (entryExceptionType.Text);
				else
					return;
			}

			var fb = be as FunctionBreakpoint;
			if (fb != null)
				SaveFunctionBreakpoint (fb);

			var bp = be as Breakpoint;
			if (bp != null)
				SaveBreakpoint (bp, isNew);

			var cp = be as Catchpoint;
			if (cp != null)
				SaveCatchpoint (cp);

			be.HitCountMode = (HitCountMode)ignoreHitType.SelectedItem;
			be.HitCount = be.HitCountMode != HitCountMode.None ? (int)ignoreHitCount.Value : 0;

			if (checkPrintExpression.Active) {
				// FIXME: Make HitAction flags.
				be.HitAction = HitAction.PrintExpression;
				be.TraceExpression = entryPrintExpression.Text;
			}
			be.HitAction = HitAction.Break;

			be.CommitChanges ();
		}

		void OnUpdateControls (object sender, EventArgs e)
		{
			// Check which radio is selected.
			hboxFunction.Sensitive = stopOnFunction.Active;
			hboxLineColumn.Sensitive = stopOnLocation.Active;
			hboxLocation.Sensitive = stopOnLocation.Active;
			hboxException.Sensitive = stopOnException.Active;
			checkIncludeSubclass.Sensitive = stopOnException.Active;

			// Check conditional
			if (!String.IsNullOrEmpty (entryConditionalExpression.Text))
				conditionalHitType.Show ();
			else
				conditionalHitType.Hide ();

			// Check ignoring hit counts.
			if (ignoreHitType.SelectedItem.Equals (HitCountMode.None))
				ignoreHitCount.Hide ();
			else
				ignoreHitCount.Show ();

			// Check printing an expression.
			entryPrintExpression.Sensitive = checkPrintExpression.Active;
			checkResumeExecution.Sensitive = checkPrintExpression.Active;

			// And display warning icons
			buttonOk.Sensitive = CheckValidity ();
		}

		bool CheckValidity ()
		{
			if (be is FunctionBreakpoint) {
				string text = entryFunctionName.Text.Trim ();

				if (stopOnFunction.Active) {
					if (text.Length == 0) {
						warningFunction.Show ();
						warningFunction.TooltipText = GettextCatalog.GetString ("Function name not specified");
						return false;
					}

					if (!TryParseFunction (text, out parsedFunction, out parsedParamTypes)) {
						warningFunction.Show ();
						warningFunction.TooltipText = GettextCatalog.GetString ("Invalid function syntax");
						return false;
					}
				}
			} else if (be is Breakpoint) {
				if (!System.IO.File.Exists (entryLocationFile.Text)) {
					warningLocation.Show ();
					warningLocation.TooltipText = GettextCatalog.GetString ("File does not exist");
					return false;
				}

				int val;
				if (!Int32.TryParse (entryLocationLine.Text, out val)) {
					warningLocation.Show ();
					warningLocation.TooltipText = GettextCatalog.GetString ("Line is not a number");
					return false;
				}

				if (!Int32.TryParse (entryLocationColumn.Text, out val)) {
					warningLocation.Show ();
					warningLocation.TooltipText = GettextCatalog.GetString ("Column is not a number");
					return false;
				}

				if (stopOnLocation.Active) {
					if (checkPrintExpression.Active && entryPrintExpression.Text.Length == 0) {
						warningCondition.Show ();
						warningCondition.TooltipText = GettextCatalog.GetString ("Trace expression not specified");
						return false;
					}
				}
			} else if (be is Catchpoint) {
				if (!classes.Contains (entryExceptionType.Text)) {
					warningException.Show ();
					warningException.TooltipText = GettextCatalog.GetString ("Exception not identified");
					return false;
				}
			}

			warningFunction.Hide ();
			warningLocation.Hide ();
			warningException.Hide ();
			warningCondition.Hide ();

			return true;
		}

		static bool TryParseFunction (string signature, out string function, out string[] paramTypes)
		{
			int paramListStart = signature.IndexOf ('(');
			int paramListEnd = signature.IndexOf (')');

			if (paramListStart == -1 && paramListEnd == -1) {
				function = signature;
				paramTypes = null;
				return true;
			}

			if (paramListEnd != signature.Length - 1) {
				paramTypes = null;
				function = null;
				return false;
			}

			function = signature.Substring (0, paramListStart).Trim ();

			paramListStart++;

			if (!FunctionBreakpoint.TryParseParameters (signature, paramListStart, paramListEnd, out paramTypes)) {
				paramTypes = null;
				function = null;
				return false;
			}

			return true;
		}

		void LoadExceptionList ()
		{
			classes.Add ("System.Exception");
			if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
				var dom = TypeSystemService.GetCompilation (IdeApp.ProjectOperations.CurrentSelectedProject);
				foreach (var t in dom.FindType (typeof (Exception)).GetSubTypeDefinitions ())
					classes.Add (t.ReflectionName);
			} else {
				// no need to unload this assembly context, it's not cached.
				var unresolvedAssembly = TypeSystemService.LoadAssemblyContext (Runtime.SystemAssemblyService.CurrentRuntime, MonoDevelop.Core.Assemblies.TargetFramework.Default, typeof(Uri).Assembly.Location);
				var mscorlib = TypeSystemService.LoadAssemblyContext (Runtime.SystemAssemblyService.CurrentRuntime, MonoDevelop.Core.Assemblies.TargetFramework.Default, typeof(object).Assembly.Location);
				if (unresolvedAssembly != null && mscorlib != null) {
					var dom = new ICSharpCode.NRefactory.TypeSystem.Implementation.SimpleCompilation (unresolvedAssembly, mscorlib);
					foreach (var t in dom.FindType (typeof (Exception)).GetSubTypeDefinitions ())
						classes.Add (t.ReflectionName);
				}
			}
		}

		public BreakEvent GetBreakEvent ()
		{
			return be;
		}

		void SetLayout ()
		{
			var vbox = new Xwt.VBox ();
			vbox.MinHeight = 400;
			vbox.MinWidth = 450;

			vbox.PackStart (new Xwt.Label (GettextCatalog.GetString ("Pause program execution in the debugger")));

			// Radio group
			var vboxRadio = new Xwt.VBox {
				MarginLeft = 12
			};

			// Function group
			vboxRadio.PackStart (stopOnFunction);

			hboxFunction = new Xwt.HBox {
				MarginLeft = 12
			};
			hboxFunction.PackStart (new Xwt.Label (GettextCatalog.GetString ("Function:")));
			hboxFunction.PackStart (entryFunctionName, true);
			hboxFunction.PackEnd (warningFunction);

			vboxRadio.PackStart (hboxFunction);

			// Location group
			vboxRadio.PackStart (stopOnLocation);

			var vboxLocation = new Xwt.VBox {
				MarginLeft = 12
			};
			hboxLocation = new Xwt.HBox ();
			hboxLocation.PackStart (new Xwt.Label (GettextCatalog.GetString ("Location:")));
			hboxLocation.PackStart (entryLocationFile, true);
			hboxLocation.PackEnd (warningLocation);
			vboxLocation.PackStart (hboxLocation);

			hboxLineColumn = new Xwt.HBox ();
			hboxLineColumn.PackStart (new Xwt.Label (GettextCatalog.GetString ("Line:")));
			hboxLineColumn.PackStart (entryLocationLine);
			hboxLineColumn.PackStart (new Xwt.Label (GettextCatalog.GetString ("Column:")));
			hboxLineColumn.PackStart (entryLocationColumn);
			vboxLocation.PackStart (hboxLineColumn);

			vboxRadio.PackStart (vboxLocation);

			// Exception group
			vboxRadio.PackStart (stopOnException);

			var vboxException = new Xwt.VBox {
				MarginLeft = 12
			};
			hboxException = new Xwt.HBox ();
			hboxException.PackStart (new Xwt.Label (GettextCatalog.GetString ("Type:")));
			hboxException.PackStart (entryExceptionType, true);
			hboxException.PackEnd (warningException);

			vboxException.PackStart (hboxException);
			vboxException.PackStart (checkIncludeSubclass);
			vboxRadio.PackStart (vboxException);

			vbox.PackStart (vboxRadio);

			var hboxCondition = new Xwt.HBox ();
			hboxCondition.PackStart (new Xwt.Label (GettextCatalog.GetString ("Condition:")));
			hboxCondition.PackStart (entryConditionalExpression, true);
			hboxCondition.PackEnd (warningCondition);
			hboxCondition.PackEnd (conditionalHitType);

			vbox.PackStart (hboxCondition);

			var hboxHitCount = new Xwt.HBox ();
			hboxHitCount.PackStart (new Xwt.Label (GettextCatalog.GetString ("When hit break")));
			hboxHitCount.PackStart (ignoreHitType);
			hboxHitCount.PackStart (ignoreHitCount);

			vbox.PackStart (hboxHitCount);

			vbox.PackStart (checkPrintExpression);
			var vboxExpression = new Xwt.VBox {
				MarginLeft = 12
			};
			vboxExpression.PackStart (entryPrintExpression);
			vboxExpression.PackStart (checkResumeExecution);

			vbox.PackStart (vboxExpression);

			Buttons.Add (new Xwt.DialogButton (Xwt.Command.Cancel));
			Buttons.Add (buttonOk);

			Content = vbox;

			OnUpdateControls (null, null);
		}
	}
}