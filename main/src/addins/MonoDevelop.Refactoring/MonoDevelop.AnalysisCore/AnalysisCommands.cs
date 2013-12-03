// 
// AnalysisCommands.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Components.Commands;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.AnalysisCore.Gui;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.QuickTasks;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.AnalysisCore.Fixes;
using MonoDevelop.Ide;
using MonoDevelop.CodeIssues;
using ICSharpCode.NRefactory.Refactoring;
using MonoDevelop.CodeActions;
using System.Threading;

namespace MonoDevelop.AnalysisCore
{
	public enum AnalysisCommands
	{
		FixOperations,
		ShowFixes,
		QuickFix
	}
	
	class ShowFixesHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			MonoDevelop.Ide.Gui.Document document;
			IList<FixableResult> results;
			info.Enabled = FixOperationsHandler.GetFixes (out document, out results)
			    && results.Any (r => FixOperationsHandler.GetActions (document, r).Any ());
		}
		
		protected override void Run ()
		{
			var doc = MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument;
			var view = doc.GetContent<MonoDevelop.SourceEditor.SourceEditorView> ();
			if (view == null) {
				LoggingService.LogWarning ("ShowFixesHandler could not find a SourceEditorView");
				return;
			}
			var widget = view.TextEditor;
			var pt = view.DocumentToScreenLocation (doc.Editor.Caret.Location);
			
			var ces = new CommandEntrySet ();
			ces.AddItem (AnalysisCommands.FixOperations);
			var menu = MonoDevelop.Ide.IdeApp.CommandService.CreateMenu (ces);
			
			menu.Popup (null, null, delegate (Menu mn, out int x, out int y, out bool push_in) {
				x = pt.X;
				y = pt.Y;
				push_in = true;
				//if the menu would be off the bottom of the screen, "drop" it upwards
				if (y + mn.Requisition.Height > widget.Screen.Height)
					y -= mn.Requisition.Height + (int)widget.LineHeight;
			}, 0, Global.CurrentEventTime);
		}
	}
	
	class FixOperationsHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			MonoDevelop.Ide.Gui.Document document;
			IList<FixableResult> results;
			if (!GetFixes (out document, out results))
				return;
			PopulateInfos (info, document, results);
		}
		
		protected override void Run (object dataItem)
		{
			if (dataItem is Result) {
				((Result)dataItem).ShowResultOptionsDialog ();
				return;
			}
			if (dataItem is System.Action)  {
				((System.Action)dataItem) ();
				return;
			}
			var action = dataItem as IAnalysisFixAction;
			if (action != null) {
				action.Fix (); 
				return;
			}
			var ca = dataItem as CodeAction;
			if (ca != null) {
				var doc = MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument;

				var context = doc.ParsedDocument.CreateRefactoringContext != null ? doc.ParsedDocument.CreateRefactoringContext (doc, default(CancellationToken)) : null;
				using (var script = context.CreateScript ()) {
					ca.Run (context, script);
				}
				return;
			}


		}
		
		public static bool GetFixes (out Document document, out IList<FixableResult> results)
		{
			results = null;
			document = MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument;
			if (document == null)
				return false;
			
			var ext = document.GetContent<ResultsEditorExtension> ();
			if (ext == null)
				return false;
			
			var list = ext.GetResultsAtOffset (document.Editor.Caret.Offset).OfType<FixableResult> ().ToList ();
			list.Sort (ResultCompareImportanceDesc);
			results = list;

			if (results.Count > 0)
				return true;

			var codeActionExtension = document.GetContent <CodeActionEditorExtension> ();
			if (codeActionExtension != null) {
				var fixes = codeActionExtension.GetCurrentFixes ();
				if (fixes != null)
					return fixes.Any (CodeActionWidget.IsAnalysisOrErrorFix);
			} 
			return false;
		}
		
		static int ResultCompareImportanceDesc (Result r1, Result r2)
		{
			int c = ((int)r1.Level).CompareTo ((int)r2.Level);
			if (c != 0)
				return c;
			return string.Compare (r1.Message, r2.Message, StringComparison.Ordinal);
		}
		
		public static void PopulateInfos (CommandArrayInfo infos, Document doc, IEnumerable<FixableResult> results)
		{
			//FIXME: ellipsize long messages
			int mnemonic = 1;

			var codeActionExtension = doc.GetContent <CodeActionEditorExtension> ();
			var fixes = codeActionExtension.GetCurrentFixes ();
			if (fixes != null) {
				foreach (var _fix in fixes.Where (CodeActionWidget.IsAnalysisOrErrorFix)) {
					var fix = _fix;
					if (fix is AnalysisContextActionProvider.AnalysisCodeAction)
						continue;
					var escapedLabel = fix.Title.Replace ("_", "__");
					var label = (mnemonic <= 10)
						? "_" + (mnemonic++ % 10).ToString () + " " + escapedLabel
							: "  " + escapedLabel;
					infos.Add (label, fix);
				}
			}

			foreach (var result in results) {
				bool firstAction = true;
				foreach (var action in GetActions (doc, result)) {
					if (firstAction) {
						//FIXME: make this header item insensitive but not greyed out
						infos.Add (new CommandInfo (result.Message.Replace ("_", "__"), false, false) {
							Icon = GetIcon (result.Level)
						}, null);
						firstAction = false;
					}
					var escapedLabel = action.Label.Replace ("_", "__");
					var label = (mnemonic <= 10)
						? "_" + (mnemonic++ % 10).ToString () + " " + escapedLabel
						: "  " + escapedLabel;
					infos.Add (label, action);
				}
				if (result.HasOptionsDialog) {
					var declSet = new CommandInfoSet ();
					declSet.Text = GettextCatalog.GetString ("_Options for \"{0}\"", result.OptionsTitle);

					bool hasBatchFix = false;
					foreach (var fix in result.Fixes.OfType<IAnalysisFixAction> ().Where (f => f.SupportsBatchFix)) {
						hasBatchFix = true;
						var title = string.Format (GettextCatalog.GetString ("Apply in file: {0}"), fix.Label);
						declSet.CommandInfos.Add (title, new System.Action(fix.BatchFix));
					}
					if (hasBatchFix)
						declSet.CommandInfos.AddSeparator ();

					var ir = result as InspectorResults;
					if (ir != null) {
						var inspector = ir.Inspector;

						if (inspector.CanSuppressWithAttribute) {
							declSet.CommandInfos.Add (GettextCatalog.GetString ("_Suppress with attribute"), new System.Action(delegate {
								inspector.SuppressWithAttribute (doc, ir.Region); 
							}));
						}

						if (inspector.CanDisableWithPragma) {
							declSet.CommandInfos.Add (GettextCatalog.GetString ("_Suppress with #pragma"), new System.Action(delegate {
								inspector.DisableWithPragma (doc, ir.Region); 
							}));
						}

						if (inspector.CanDisableOnce) {
							declSet.CommandInfos.Add (GettextCatalog.GetString ("_Disable once with comment"), new System.Action(delegate {
								inspector.DisableOnce (doc, ir.Region); 
							}));
						}

						if (inspector.CanDisableAndRestore) {
							declSet.CommandInfos.Add (GettextCatalog.GetString ("Disable _and restore with comments"), new System.Action(delegate {
								inspector.DisableAndRestore (doc, ir.Region); 
							}));
						}
					}

					declSet.CommandInfos.Add (GettextCatalog.GetString ("_Configure inspection"), result);

					infos.Add (declSet);
				}
			}
		}
		
		public static IEnumerable<IAnalysisFixAction> GetActions (Document doc, FixableResult result)
		{
			foreach (var fix in result.Fixes)
				foreach (var handler in AnalysisExtensions.GetFixHandlers (fix.FixType))
					foreach (var action in handler.GetFixes (doc, fix))
						yield return action;

		}
		
		static string GetIcon (Severity severity)
		{
			switch (severity) {
			case Severity.Error:
				return Gtk.Stock.DialogError;
			case Severity.Warning:
				return Gtk.Stock.DialogWarning;
			case Severity.Hint:
				return Gtk.Stock.Info;
			default:
				return null;
			}
		}
	}
}

