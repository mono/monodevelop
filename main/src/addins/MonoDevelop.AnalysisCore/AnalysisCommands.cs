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

namespace MonoDevelop.AnalysisCore
{
	public enum AnalysisCommands
	{
		FixOperations,
		ShowFixes,
		QuickFix
	}
	
	class FixOperationsHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			var menu = GetFixActionInfos ();
			if (menu != null) {
				menu.Text = GettextCatalog.GetString ("Fix");
				info.Add (menu);
			}
		}
		
		protected override void Run (object dataItem)
		{
			var action = (IAnalysisFixAction) dataItem;
			action.Fix ();
		}
		
		public static CommandInfoSet GetFixActionInfos ()
		{
			var doc = MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return null;
			
			var ext = doc.GetContent<ResultsEditorExtension> ();
			if (ext == null)
				return null;
			
			var results = ext.GetResultsAtOffset (doc.Editor.Caret.Offset).OfType<FixableResult> ();
			
			var infoSet = new CommandInfoSet ();
			
			//FIXME: ellipsize long messages
			foreach (var result in results) {
				foreach (var fix in result.Fixes) {
					var handlers = AnalysisExtensions.GetFixHandlers (fix.FixType);
					bool firstAction = true;
					foreach (var action in handlers.SelectMany (h => h.GetFixes (doc, fix))) {
						if (firstAction) {
							infoSet.CommandInfos.Add (new CommandInfo (result.Message, false, false), null);
							firstAction = false;
						}
						infoSet.CommandInfos.Add ("  " + action.Label, action);
					}
				}
			}
			if (infoSet.CommandInfos.Count == 0)
				return null;
			
			return infoSet;
		}
	}
}

