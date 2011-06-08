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
			var action = (IAnalysisFixAction)dataItem;
			action.Fix ();
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
			return results.Count > 0;
		}
		
		static int ResultCompareImportanceDesc (Result r1, Result r2)
		{
			int c = ((int)r1.Level).CompareTo ((int)r2.Level);
			if (c != 0)
				return c;
			c = ((int)r1.Importance).CompareTo ((int)r2.Importance);
			if (c != 0)
				return c;
			c = ((int)r1.Certainty).CompareTo ((int)r2.Certainty);
			if (c != 0)
				return c;
			return r1.Message.CompareTo (r2.Message);
		}
		
		public static void PopulateInfos (CommandArrayInfo infos, Document doc, IEnumerable<FixableResult> results)
		{
			//FIXME: ellipsize long messages
			int mnemonic = 1;
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
			}
		}
		
		public static IEnumerable<IAnalysisFixAction> GetActions (Document doc, FixableResult result)
		{
			foreach (var fix in result.Fixes)
				foreach (var handler in AnalysisExtensions.GetFixHandlers (fix.FixType))
					foreach (var action in handler.GetFixes (doc, fix))
						yield return action;
		}
		
		static string GetIcon (QuickTaskSeverity severity)
		{
			switch (severity) {
			case QuickTaskSeverity.Error:
				return Gtk.Stock.DialogError;
			case QuickTaskSeverity.Warning:
				return Gtk.Stock.DialogWarning;
			case QuickTaskSeverity.Hint:
				return Gtk.Stock.Info;
			default:
				return null;
			}
		}
	}
}

