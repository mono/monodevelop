//
// ValaTextEditorExtension.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Gui.Completion;

using MonoDevelop.ValaBinding.Parser;

namespace MonoDevelop.ValaBinding
{
	public class ValaTextEditorExtension : CompletionTextEditorExtension
	{
		// Allowed chars to be next to an identifier
		private static char[] allowedChars = new char[] {
			'.', ':', ' ', '\t', '=', '*', '+', '-', '/', '%', ',', '&',
			'|', '^', '{', '}', '[', ']', '(', ')', '\n', '!', '?', '<', '>'
		};
		
		// Allowed Chars to be next to an identifier excluding ':' (to get the full name in '::' completion).
		private static char[] allowedCharsMinusColon = new char[] {
			'.', ' ', '\t', '=', '*', '+', '-', '/', '%', ',', '&', '|',
			'^', '{', '}', '[', ']', '(', ')', '\n', '!', '?', '<', '>'
		};
		
		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			return (Path.GetExtension (doc.Name).ToUpper () == ".VALA"   ||
			        Path.GetExtension (doc.Name).ToUpper () == ".VAPI" );
		}
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			int line, column;
			Editor.GetLineColumnFromPosition (Editor.CursorPosition, out line, out column);
			string lineText = Editor.GetLineText (line);
			
			// smart formatting strategy
			if (TextEditorProperties.IndentStyle == IndentStyle.Smart) {
				if (key == Gdk.Key.Return) {
					if (lineText.TrimEnd ().EndsWith ("{")) {
						Editor.InsertText (Editor.CursorPosition, 
						    "\n" + TextEditorProperties.IndentString + GetIndent (Editor, line));
						return false;
					}
				} else if (key == Gdk.Key.braceright && AllWhiteSpace (lineText) 
				    && lineText.StartsWith (TextEditorProperties.IndentString)) {
					if (lineText.Length > 0)
						lineText = lineText.Substring (TextEditorProperties.IndentString.Length);
					Editor.ReplaceLine (line, lineText + "}");
					return false;
				}
			}
			
			return base.KeyPress (key, keyChar, modifier);
		}
		
		public override ICompletionDataList HandleCodeCompletion (
		    ICodeCompletionContext completionContext, char completionChar)
		{
			string lineText = Editor.GetLineText (completionContext.TriggerLine + 1);
			
			if (lineText.EndsWith (".")) {
				// remove the trailing '.'
				lineText = lineText.Substring (0, lineText.Length - 1);
				
				int nameStart = lineText.LastIndexOfAny (allowedChars);

				nameStart++;
				
				string itemName = lineText.Substring (nameStart).Trim ();
				
				if (string.IsNullOrEmpty (itemName))
					return null;
				
				return GetMembersOfItem (itemName);
			}
			
			return null;
		}
		
		public override ICompletionDataList CodeCompletionCommand (
		    ICodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			int line, column;
			Editor.GetLineColumnFromPosition (Editor.CursorPosition, out line, out column);
			string lineText = Editor.GetLineText (line);
			
			if (!lineText.Contains ("."))
			    return GlobalComplete ();
			
			return HandleCodeCompletion (completionContext, Editor.GetText (pos - 1, pos)[0]);
		}
		
		private CompletionDataList GetMembersOfItem (string itemFullName)
		{
			ValaProject project = Document.Project as ValaProject;
			
			if (project == null)
				return null;
				
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			CompletionDataList list = new CompletionDataList ();
			
			LanguageItem container = null;
			string containerType = null;
			
			string currentFileName = Document.FileName;
			bool in_project = false;
			
			// Try containers
			foreach (LanguageItem li in info.Containers ()) {
				if ((itemFullName == li.FullName) || (itemFullName == li.Name)){
					container = li;
					in_project = true;
					break;
				}
			}
			
			// Try included containers
			if (!in_project && info.IncludedFiles.ContainsKey (currentFileName)) {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					foreach (LanguageItem li in fi.Containers ()) {
						if ((itemFullName == li.FullName) || (itemFullName == li.Name)) {
							container = li;
							break;
						}
					}
				}
			}
			
			// Try instances
			// Find the typename of the instance
			foreach (Member li in info.Members ) {
				if (itemFullName == li.Name) {
					containerType = li.InstanceType;
					in_project = true;
					break;
				}
			}
			
			// Search included files
			if (!in_project && info.IncludedFiles.ContainsKey (currentFileName)) {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					foreach (Member li in fi.Members) {
						if (itemFullName == li.Name) {
							containerType = li.InstanceType;
							break;
						}
					}
				}
			}
			
			if (null == container) {
				// Search locals
				foreach (Local li in info.Locals ) {
					if (itemFullName == li.Name && currentFileName == li.File) {
						containerType = li.InstanceType;
						in_project = true;
						break;
					}
				}
			}

			
			if ((container == null) && (containerType == null))
				return null;
			
			if(null != container) {
				if (in_project) {
					foreach (LanguageItem li in info.AllItems ()) {
						if (li.Parent != null && li.Parent.Equals (container)) {
							list.Add (new CompletionData (li));
						}
					}
				} else {
					foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
						foreach (LanguageItem li in fi.AllItems ()) {
							if (li.Parent != null && li.Parent.Equals (container))
								list.Add (new CompletionData (li));
						}
					}
				}
			} else {
				if (in_project) {
					AddMembersWithParent (list, info.InstanceMembers (), containerType);
				} else {
					foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
						AddMembersWithParent (list, fi.InstanceMembers (), containerType);
					}
				}
			}
			
			return list;
		}
		
		/// <summary>
		/// Adds completion data for children to a list
		/// </summary>
		/// <param name="list">
		/// The list to which completion data will be added
		/// <see cref="CompletionDataList"/>
		/// </param>
		/// <param name="items">
		/// A list of items to search
		/// <see cref="IEnumerable"/>
		/// </param>
		/// <param name="parentName">
		/// The name of the parent that will be matched
		/// <see cref="System.String"/>
		/// </param>
		public static void AddMembersWithParent(CompletionDataList list, IEnumerable<LanguageItem> items, string parentName) {
				foreach (LanguageItem li in items) {
					if (li.Parent != null && li.Parent.Name.EndsWith (parentName))
						list.Add (new CompletionData (li));
				}
		}

		
		private ICompletionDataList GlobalComplete ()
		{
			ValaProject project = Document.Project as ValaProject;
			
			if (project == null)
				return null;
			
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			
			CompletionDataList list = new CompletionDataList ();
			
			foreach (LanguageItem li in info.Containers ())
				if (li.Parent == null)
					list.Add (new CompletionData (li));
			
			foreach (Function f in info.Functions)
				if (f.Parent == null)
					list.Add (new CompletionData (f));

			foreach (Enumerator e in info.Enumerators)
				list.Add (new CompletionData (e));
			
			foreach (Macro m in info.Macros)
				list.Add (new CompletionData (m));
			
			string currentFileName = Document.FileName;
			
			if (info.IncludedFiles.ContainsKey (currentFileName)) {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					foreach (LanguageItem li in fi.Containers ())
						if (li.Parent == null)
							list.Add (new CompletionData (li));
					
					foreach (Function f in fi.Functions)
						if (f.Parent == null)
							list.Add (new CompletionData (f));

					foreach (Enumerator e in fi.Enumerators)
						list.Add (new CompletionData (e));
					
					foreach (Macro m in fi.Macros)
						list.Add (new CompletionData (m));
				}
			}
			
			return list;
		}
		
		public override  IParameterDataProvider HandleParameterCompletion (
		    ICodeCompletionContext completionContext, char completionChar)
		{
            //System.Console.WriteLine("ValaTextEditorExtension.HandleParameterCompletion({0})", completionChar);
			if (completionChar != '(')
				return null;
			
			ValaProject project = Document.Project as ValaProject;
			
			if (project == null)
				return null;
			
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			
			int line, column;
			Editor.GetLineColumnFromPosition (Editor.CursorPosition, out line, out column);
			int position = Editor.GetPositionFromLineColumn (line, 1);
			string lineText = Editor.GetText (position, Editor.CursorPosition - 1).TrimEnd ();
			
			int nameStart = lineText.LastIndexOfAny (allowedChars);

			nameStart++;
			
			string functionName = lineText.Substring (nameStart).Trim ();
			
			if (string.IsNullOrEmpty (functionName))
				return null;
			
			return new ParameterDataProvider (Document, info, functionName);
		}
		
		private bool AllWhiteSpace (string lineText)
		{
			foreach (char c in lineText)
				if (!char.IsWhiteSpace (c))
					return false;
			
			return true;
		}
		
		// Snatched from DefaultFormattingStrategy
		private string GetIndent (TextEditor d, int lineNumber)
		{
			string lineText = d.GetLineText (lineNumber);
			StringBuilder whitespaces = new StringBuilder ();
			
			foreach (char ch in lineText) {
				if (!char.IsWhiteSpace (ch))
					break;
				whitespaces.Append (ch);
			}
			
			return whitespaces.ToString ();
		}
	}
}
