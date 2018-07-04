// 
// CSharpIndentVirtualSpaceManager.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Formatting
{
	class IndentVirtualSpaceManager : IndentationTracker
	{
		readonly TextEditor editor;
		private ISmartIndentationService indentationService;
		int cacheSpaceCount = -1, oldTabCount = -1;
		string cachedIndentString;

		public override IndentationTrackerFeatures SupportedFeatures { 
            get {
                return IndentationTrackerFeatures.SmartBackspace | IndentationTrackerFeatures.CustomIndentationEngine;
            }
        }

		public IndentVirtualSpaceManager(TextEditor editor)
		{
			this.editor = editor;
			indentationService = CompositionManager.Instance.ExportProvider.GetExportedValue<ISmartIndentationService> ();
		}

		#region IndentationTracker implementation
		public override string GetIndentationString (int lineNumber)
		{
			if (lineNumber < 1) 
				return "";
			var snapshot = editor.TextView.TextBuffer.CurrentSnapshot;
			var caretLine = snapshot.GetLineFromLineNumber (lineNumber - 1);
			int? indentation = indentationService.GetDesiredIndentation (editor.TextView, caretLine);
			if (indentation.HasValue) {
				int tabCount = 0;
				int spaceCount = indentation.Value;
				if (!editor.Options.TabsToSpaces) {
					tabCount = spaceCount / editor.Options.TabSize;
					spaceCount = spaceCount % editor.Options.TabSize;
				}
				if (cacheSpaceCount != spaceCount || oldTabCount != tabCount) {
					string tabString = new string ('\t', tabCount);
					string spaceString = new string (' ', spaceCount);
					cacheSpaceCount = spaceCount;
					cacheSpaceCount = tabCount;
					return cachedIndentString = tabString + spaceString;
				}
				return cachedIndentString;
			}
			if (caretLine.Length > 0) {
				var sb = StringBuilderCache.Allocate ();
				try {
					for (int i = 0; i < caretLine.Length; i++) {
						char curChar = snapshot [i + caretLine.Start];
						if (Char.IsWhiteSpace (curChar))
							sb.Append (curChar);
						else
							break;
					}
					return sb.ToString ();
				} finally {
					StringBuilderCache.Free (sb);
				}
			}

			return "";
		}
		#endregion
	}
}
