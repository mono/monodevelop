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
		private delegate CompletionDataProvider GetMembersForExtension (CTextEditorExtension self, string completionExtension, string completionText);
		
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
			return (Path.GetExtension (doc.Title).ToUpper () == ".C"   ||
			        Path.GetExtension (doc.Title).ToUpper () == ".CPP" ||
			        Path.GetExtension (doc.Title).ToUpper () == ".CXX" ||
			        Path.GetExtension (doc.Title).ToUpper () == ".H"   ||
			        Path.GetExtension (doc.Title).ToUpper () == ".HPP");
		}
		
		public override bool KeyPress (Gdk.Key key, Gdk.ModifierType modifier)
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
			
			return base.KeyPress (key, modifier);
		}
		
		public override ICompletionDataProvider HandleCodeCompletion (
		    ICodeCompletionContext completionContext, char completionChar)
		{
			string lineText = Editor.GetLineText (completionContext.TriggerLine + 1);
			
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
		
		public override ICompletionDataProvider CodeCompletionCommand (
		    ICodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			int line, column;
			Editor.GetLineColumnFromPosition (Editor.CursorPosition, out line, out column);
			string lineText = Editor.GetLineText (line);
			
			foreach (KeyValuePair<string, GetMembersForExtension> pair in completionExtensions) {
				if(lineText.Contains (pair.Key)) {
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
		/// <see cref="CompletionDataProvider"/>
		/// </returns>
		private static CompletionDataProvider GetItemMembers (CTextEditorExtension self, string completionExtension, string completionText) {
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
		/// <see cref="CompletionDataProvider"/>
		/// </returns>
		private static CompletionDataProvider GetInstanceMembers (CTextEditorExtension self, string completionExtension, string completionText) {
			return self.GetMembersOfInstance (completionText, ("->" == completionExtension));
		}
		
		private CompletionDataProvider GetMembersOfItem (string itemFullName)
		{
			CProject project = Document.Project as CProject;
			
			if (project == null)
				return null;
				
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			CompletionDataProvider provider = new CompletionDataProvider ();
			
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
				AddItemsWithParent (provider, info.AllItems (), container);
			} else {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					AddItemsWithParent (provider, fi.AllItems (), container);
				}
			}
			
			return provider;
		}
		
		/// <summary>
		/// Adds completion data for children to a provider
		/// </summary>
		/// <param name="provider">
		/// The provider to which completion data will be added
		/// <see cref="CompletionDataProvider"/>
		/// </param>
		/// <param name="items">
		/// A list of items to search
		/// <see cref="IEnumerable"/>
		/// </param>
		/// <param name="parent">
		/// The parent that will be matched
		/// </param>
		public static void AddItemsWithParent(CompletionDataProvider provider, IEnumerable<LanguageItem> items, LanguageItem parent) {
			foreach (LanguageItem li in items) {
				if (li.Parent != null && li.Parent.Equals (parent))
					provider.AddCompletionData (new CompletionData (li));
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
		/// <see cref="CompletionDataProvider"/>
		/// </returns>
		private CompletionDataProvider GetMembersOfInstance (string instanceName, bool isPointer)
		{
			CProject project = Document.Project as CProject;
			
			if (project == null)
				return null;
				
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			CompletionDataProvider provider = new CompletionDataProvider ();
			
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
				AddMembersWithParent (provider, info.InstanceMembers (), container);
			} else {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					AddMembersWithParent (provider, fi.InstanceMembers (), container);
				}
			}
			
			return provider;
		}
		
		/// <summary>
		/// Adds completion data for children to a provider
		/// </summary>
		/// <param name="provider">
		/// The provider to which completion data will be added
		/// <see cref="CompletionDataProvider"/>
		/// </param>
		/// <param name="items">
		/// A list of items to search
		/// <see cref="IEnumerable"/>
		/// </param>
		/// <param name="parentName">
		/// The name of the parent that will be matched
		/// <see cref="System.String"/>
		/// </param>
		public static void AddMembersWithParent(CompletionDataProvider provider, IEnumerable<LanguageItem> items, string parentName) {
				foreach (LanguageItem li in items) {
					if (li.Parent != null && li.Parent.Name.EndsWith (parentName))
						provider.AddCompletionData (new CompletionData (li));
				}
		}

		private ICompletionDataProvider GlobalComplete ()
		{
			CProject project = Document.Project as CProject;
			
			if (project == null)
				return null;
			
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			
			CompletionDataProvider provider = new CompletionDataProvider ();
			
			foreach (LanguageItem li in info.Containers ())
				if (li.Parent == null)
					provider.AddCompletionData (new CompletionData (li));
			
			foreach (Function f in info.Functions)
				if (f.Parent == null)
					provider.AddCompletionData (new CompletionData (f));

			foreach (Enumerator e in info.Enumerators)
				provider.AddCompletionData (new CompletionData (e));
			
			foreach (Macro m in info.Macros)
				provider.AddCompletionData (new CompletionData (m));
			
			string currentFileName = Document.FileName;
			
			if (info.IncludedFiles.ContainsKey (currentFileName)) {
				foreach (FileInformation fi in info.IncludedFiles[currentFileName]) {
					foreach (LanguageItem li in fi.Containers ())
						if (li.Parent == null)
							provider.AddCompletionData (new CompletionData (li));
					
					foreach (Function f in fi.Functions)
						if (f.Parent == null)
							provider.AddCompletionData (new CompletionData (f));

					foreach (Enumerator e in fi.Enumerators)
						provider.AddCompletionData (new CompletionData (e));
					
					foreach (Macro m in fi.Macros)
						provider.AddCompletionData (new CompletionData (m));
				}
			}
			
			return provider;
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
