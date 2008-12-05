//
// CTextEditorExtension.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
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
using MonoDevelop.Components.Commands;

using CBinding.Parser;

namespace CBinding
{
	public class CTextEditorExtension : CompletionTextEditorExtension
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
		
		/// <summary>
		/// A delegate for getting completion data 
		/// </summary>
		private delegate CompletionDataList GetMembersForExtension (CTextEditorExtension self, string completionExtension, string completionText);
		
		/// <summary>
		/// An associative array containing each completion-triggering extension 
		/// and its respective callback
		/// </summary>
		private static KeyValuePair<string, GetMembersForExtension>[] completionExtensions = new KeyValuePair<string, GetMembersForExtension>[] {
			new KeyValuePair<string, GetMembersForExtension>("::", GetItemMembers),
			new KeyValuePair<string, GetMembersForExtension>("->", GetInstanceMembers),
			new KeyValuePair<string, GetMembersForExtension>(".", GetInstanceMembers)
		};
		
		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			return (CProject.IsHeaderFile (doc.Name) || 
			        (0 <= Array.IndexOf(CProject.SourceExtensions, Path.GetExtension(doc.Name).ToUpper ())));
		}
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			int line, column;
			Editor.GetLineColumnFromPosition (Editor.CursorPosition, out line, out column);
			int lineBegins = Editor.GetPositionFromLineColumn(line, 0);
			int lineCursorIndex = (Editor.CursorPosition - lineBegins) - 1;
			string lineText = Editor.GetLineText (line);
			
			// Smart Indentation
			if (TextEditorProperties.IndentStyle == IndentStyle.Smart)
			{
				switch(key)
				{
					case Gdk.Key.Return:
						// Calculate additional indentation, if any.
						char finalChar = '\0';
						char nextChar = '\0';
						string indent = String.Empty;
						
						if(lineText.Length > 0)
						{
							if(lineCursorIndex > 0)
								finalChar = lineText[Math.Min(lineCursorIndex, lineText.Length) - 1];
							
							if(lineCursorIndex < lineText.Length)
								nextChar = lineText[lineCursorIndex];

							if(finalChar == '{')
								indent = TextEditorProperties.IndentString;
						}

						// If the next character is an closing brace, indent it appropriately.
						if(TextEditor.IsBrace(nextChar) && !TextEditor.IsOpenBrace(nextChar))
						{
							int openingLine = 0;
							if(Editor.GetClosingBraceForLine(line, out openingLine) >= 0)
							{
								Editor.InsertText(Editor.CursorPosition, Editor.NewLine + GetIndent(Editor, openingLine, 0));
								return false;
							}
						}

						// Default indentation method
						Editor.InsertText(Editor.CursorPosition, Editor.NewLine + indent + GetIndent(Editor, line, lineCursorIndex));
						
						return false;

					case Gdk.Key.braceright:
						// Only indent if the brace is preceeded by whitespace.
						if(AllWhiteSpace(lineText.Substring(0, lineCursorIndex)) == false)
							break;

						int openingLine = 0;
						if(Editor.GetClosingBraceForLine(line, out openingLine) >= 0)
						{
							Editor.ReplaceLine(line, GetIndent(Editor, openingLine, 0) + "}" + lineText.Substring(lineCursorIndex));
							return false;
						}

						break;
				}
			}
			
			return base.KeyPress (key, keyChar, modifier);
		}
		
		public override ICompletionDataList HandleCodeCompletion (
		    ICodeCompletionContext completionContext, char completionChar)
		{
			string lineText = Editor.GetLineText (completionContext.TriggerLine + 1).TrimEnd();
			
			// If the line ends with a matched extension, invoke its handler
			foreach (KeyValuePair<string, GetMembersForExtension> pair in completionExtensions) {
				if (lineText.EndsWith(pair.Key)) {
					lineText = lineText.Substring (0, lineText.Length - pair.Key.Length);
					
					int nameStart = lineText.LastIndexOfAny (allowedCharsMinusColon) + 1;
					string itemName = lineText.Substring (nameStart).Trim ();
					
					if (string.IsNullOrEmpty (itemName))
						return null;
					
					return pair.Value (this, pair.Key, itemName);
				}
			}
			
			return null;
		}
		
		public override ICompletionDataList CodeCompletionCommand (
		    ICodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			int line, column;
			Editor.GetLineColumnFromPosition (Editor.CursorPosition, out line, out column);
			string lineText = Editor.GetLineText (line).Trim();
			
			foreach (KeyValuePair<string, GetMembersForExtension> pair in completionExtensions) {
				if(lineText.EndsWith(pair.Key)) {
					return HandleCodeCompletion (completionContext, Editor.GetCharAt (pos));
				}
			}

			return GlobalComplete ();
		}
		
		/// <summary>
		/// Gets contained members for a namespace or class
		/// </summary>
		/// <param name="self">
		/// The current CTextEditorExtension
		/// <see cref="CTextEditorExtension"/>
		/// </param>
		/// <param name="completionExtension">
		/// The extension that triggered the completion 
		/// (e.g. "::")
		/// <see cref="System.String"/>
		/// </param>
		/// <param name="completionText">
		/// The identifier that triggered the completion
		/// (e.g. "Foo::")
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// Completion data for the namespace or class
		/// <see cref="CompletionDataList"/>
		/// </returns>
		private static CompletionDataList GetItemMembers (CTextEditorExtension self, string completionExtension, string completionText) {
			return self.GetMembersOfItem (completionText);
		}
		
		/// <summary>
		/// Gets contained members for an instance
		/// </summary>
		/// <param name="self">
		/// The current CTextEditorExtension
		/// <see cref="CTextEditorExtension"/>
		/// </param>
		/// <param name="completionExtension">
		/// The extension that triggered the completion 
		/// (e.g. "->")
		/// <see cref="System.String"/>
		/// </param>
		/// <param name="completionText">
		/// The identifier that triggered the completion
		/// (e.g. "blah->")
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// Completion data for the instance
		/// <see cref="CompletionDataList"/>
		/// </returns>
		private static CompletionDataList GetInstanceMembers (CTextEditorExtension self, string completionExtension, string completionText) {
			return self.GetMembersOfInstance (completionText, ("->" == completionExtension));
		}
		
		private CompletionDataList GetMembersOfItem (string itemFullName)
		{
			CProject project = Document.Project as CProject;
			
			if (project == null)
				return null;
				
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			CompletionDataList list = new CompletionDataList ();
			
			LanguageItem container = null;
			
			string currentFileName = Document.FileName;
			bool in_project = false;
				
			foreach (LanguageItem li in info.Containers ()) {
				if (itemFullName == li.FullName) {
					container = li;
					in_project = true;
				}
			}
			
			if (!in_project && info.IncludedFiles.ContainsKey (currentFileName)) {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					foreach (LanguageItem li in fi.Containers ()) {
						if (itemFullName == li.FullName)
							container = li;
					}
				}
			}
			
			if (container == null)
				return null;
			
			if (in_project) {
				AddItemsWithParent (list, info.AllItems (), container);
			} else {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					AddItemsWithParent (list, fi.AllItems (), container);
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
		/// <param name="parent">
		/// The parent that will be matched
		/// </param>
		public static void AddItemsWithParent(CompletionDataList list, IEnumerable<LanguageItem> items, LanguageItem parent) {
			foreach (LanguageItem li in items) {
				if (li.Parent != null && li.Parent.Equals (parent))
					list.Add (new CompletionData (li));
			}
		}

		
		/// <summary>
		/// Gets completion data for a given instance
		/// </summary>
		/// <param name="instanceName">
		/// The identifier of the instance
		/// <see cref="System.String"/>
		/// </param>
		/// <param name="isPointer">
		/// Whether the instance in question is a pointer
		/// <see cref="System.Boolean"/>
		/// </param>
		/// <returns>
		/// Completion data for the instance
		/// <see cref="CompletionDataList"/>
		/// </returns>
		private CompletionDataList GetMembersOfInstance (string instanceName, bool isPointer)
		{
			CProject project = Document.Project as CProject;
			
			if (project == null)
				return null;
				
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			CompletionDataList list = new CompletionDataList ();
			
			string container = null;
			
			string currentFileName = Document.FileName;
			bool in_project = false;
			
			// Find the typename of the instance
			foreach (Member li in info.Members ) {
				if (instanceName == li.Name && li.IsPointer == isPointer) {
					container = li.InstanceType;
					in_project = true;
					break;
				}
			}
			
			// Search included files
			if (!in_project && info.IncludedFiles.ContainsKey (currentFileName)) {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					foreach (Member li in fi.Members) {
						if (instanceName == li.Name && li.IsPointer == isPointer) {
							container = li.InstanceType;
							break;
						}
					}
				}
			}
			
			if (null == container) {
				// Search locals
				foreach (Local li in info.Locals ) {
					if (instanceName == li.Name && li.IsPointer == isPointer && currentFileName == li.File) {
						container = li.InstanceType;
						in_project = true;
						break;
					}
				}
			}
			
			// Not found
			if (container == null)
				return null;
			
			// Get the LanguageItem corresponding to the typename 
			// and populate completion data accordingly
			if (in_project) {
				AddMembersWithParent (list, info.InstanceMembers (), container);
			} else {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					AddMembersWithParent (list, fi.InstanceMembers (), container);
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
			CProject project = Document.Project as CProject;
			
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
			if (completionChar != '(')
				return null;
			
			CProject project = Document.Project as CProject;
			
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
			// We will almost definately need a faster method than this
			foreach (char c in lineText)
				if (!char.IsWhiteSpace (c))
					return false;
			
			return true;
		}
		
		// Snatched from DefaultFormattingStrategy
		private string GetIndent (TextEditor d, int lineNumber, int terminateIndex)
		{
			string lineText = d.GetLineText (lineNumber);
			if(terminateIndex > 0)
				lineText = lineText.Substring(0, terminateIndex);
			
			StringBuilder whitespaces = new StringBuilder ();
			
			foreach (char ch in lineText) {
				if (!char.IsWhiteSpace (ch))
					break;
				whitespaces.Append (ch);
			}
			
			return whitespaces.ToString ();
		}
		
		/// <summary>
		/// Swaps the source/header for the active view
		/// </summary>
		[CommandHandler (CBinding.CProjectCommands.SwapSourceHeader)]
		public void SwapSourceHeader ()
		{
			CProject cp = Document.Project as CProject;
			
			if (null != cp) { 
				string match = cp.MatchingFile (Document.FileName);
				
				if (null != match)
					IdeApp.Workbench.OpenDocument(match);
			}
		}
		
		/// <summary>
		/// Determine whether the SwapSourceHeader command should be enabled
		/// </summary>
		/// <param name="info">
		/// The command
		/// <see cref="CommandInfo"/>
		/// </param>
		[CommandUpdateHandler (CBinding.CProjectCommands.SwapSourceHeader)]
		protected void OnSwapSourceHeader (CommandInfo info)
		{
			CProject cp = Document.Project as CProject;
			info.Visible = false;
			
			if (null != cp) {
				string filename = Document.FileName;
				
				if (CProject.IsHeaderFile (filename) || cp.IsCompileable (filename)) {
					info.Visible = true;
					info.Enabled = (null != cp.MatchingFile (Document.FileName));
				}
			}
		}
	}
}
