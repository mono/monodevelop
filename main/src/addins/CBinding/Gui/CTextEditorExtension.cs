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
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;

using CBinding.Parser;
using Mono.TextEditor;

namespace CBinding
{
	public class CTextEditorExtension : CompletionTextEditorExtension, IPathedDocument
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
		
		protected Mono.TextEditor.TextEditorData textEditorData { get; set; }
		
		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			return base.KeyPress (key, keyChar, modifier);
		}
		
		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar, ref int triggerWordLength)
		{
			string lineText = Editor.GetLineText (completionContext.TriggerLine).TrimEnd();
			
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
			
			if (char.IsLetter (completionChar)) {
				// Aggressive completion
				ICompletionDataList list = GlobalComplete ();
				triggerWordLength = ResetTriggerOffset (completionContext);
				return list;
			}
			
			return null;
		}
		
		public override ICompletionDataList HandleCodeCompletion (
		    CodeCompletionContext completionContext, char completionChar)
		{
			int triggerWordLength = 0;
			return HandleCodeCompletion (completionContext, completionChar, ref triggerWordLength);
			
			string lineText = Editor.GetLineText (completionContext.TriggerLine).TrimEnd();
			
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
		    CodeCompletionContext completionContext)
		{
			int pos = completionContext.TriggerOffset;
			string lineText = Editor.GetLineText (Editor.Caret.Line).Trim();
			
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
			list.AutoSelect = false;
			
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
				foreach (CBinding.Parser.FileInformation fi in info.IncludedFiles[currentFileName]) {
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
				foreach (CBinding.Parser.FileInformation fi in info.IncludedFiles[currentFileName]) {
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
			list.AutoSelect = false;
			
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
				foreach (CBinding.Parser.FileInformation fi in info.IncludedFiles[currentFileName]) {
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
				foreach (CBinding.Parser.FileInformation fi in info.IncludedFiles[currentFileName]) {
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
			list.AutoSelect = false;
			
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
				foreach (CBinding.Parser.FileInformation fi in info.IncludedFiles[currentFileName]) {
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
		    CodeCompletionContext completionContext, char completionChar)
		{
			if (completionChar != '(')
				return null;
			
			CProject project = Document.Project as CProject;
			
			if (project == null)
				return null;
			
			ProjectInformation info = ProjectInformationManager.Instance.Get (project);
			string lineText = Editor.GetLineText (Editor.Caret.Line).TrimEnd ();
			if (lineText.EndsWith (completionChar.ToString (), StringComparison.Ordinal))
				lineText = lineText.Remove (lineText.Length-1).TrimEnd ();
			
			int nameStart = lineText.LastIndexOfAny (allowedChars);

			nameStart++;
			
			string functionName = lineText.Substring (nameStart).Trim ();
			
			if (string.IsNullOrEmpty (functionName))
				return null;
			
			return new ParameterDataProvider (Document, info, functionName);
		}
		
		[CommandHandler (MonoDevelop.DesignerSupport.Commands.SwitchBetweenRelatedFiles)]
		protected void Run ()
		{
			var cp = this.Document.Project as CProject;
			if (cp != null) {
				string match = cp.MatchingFile (this.Document.FileName);
				if (match != null)
					MonoDevelop.Ide.IdeApp.Workbench.OpenDocument (match, true);
			}
		}
		
		[CommandUpdateHandler (MonoDevelop.DesignerSupport.Commands.SwitchBetweenRelatedFiles)]
		protected void Update (CommandInfo info)
		{
			var cp = this.Document.Project as CProject;
			info.Visible = info.Visible = cp != null && cp.MatchingFile (this.Document.FileName) != null;
		}
		
		#region IPathedDocument implementation
		
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;
		
		public Gtk.Widget CreatePathWidget (int index)
		{
			PathEntry[] path = CurrentPath;
			if (null == path || 0 > index || path.Length <= index) {
				return null;
			}
			
			object tag = path[index].Tag;
			DropDownBoxListWindow.IListDataProvider provider = null;
			if (tag is ICompilationUnit) {
				provider = new CompilationUnitDataProvider (Document);
			} else {
				provider = new DataProvider (Document, tag, GetAmbience ());
			}
			
			DropDownBoxListWindow window = new DropDownBoxListWindow (provider);
			window.SelectItem (tag);
			return window;
		}

		public PathEntry[] CurrentPath {
			get;
			private set;
		}
		
		protected virtual void OnPathChanged (DocumentPathChangedEventArgs args)
		{
			if (PathChanged != null)
				PathChanged (this, args);
		}
		
		#endregion
		
		// Yoinked from C# binding
		void UpdatePath (object sender, Mono.TextEditor.DocumentLocationEventArgs e)
		{
			var unit = Document.CompilationUnit;
			if (unit == null)
				return;
				
			var loc = textEditorData.Caret.Location;
			IType type = unit.GetTypeAt (loc.Line, loc.Column);
			List<PathEntry> result = new List<PathEntry> ();
			Ambience amb = GetAmbience ();
			IMember member = null;
			INode node = (INode)unit;
			
			if (type != null && type.ClassType != ClassType.Delegate) {
				member = type.GetMemberAt (loc.Line, loc.Column);
			}
			
			if (null != member) {
				node = member;
			} else if (null != type) {
				node = type;
			}
			
			while (node != null) {
				PathEntry entry;
				if (node is ICompilationUnit) {
					if (!Document.ParsedDocument.UserRegions.Any ())
						break;
					FoldingRegion reg = Document.ParsedDocument.UserRegions.Where (r => r.Region.Contains (loc.Line, loc.Column)).LastOrDefault ();
					if (reg == null) {
						entry = new PathEntry (GettextCatalog.GetString ("No region"));
					} else {
						entry = new PathEntry (CompilationUnitDataProvider.Pixbuf, reg.Name);
					}
					entry.Position = EntryPosition.Right;
				} else {
					entry = new PathEntry (ImageService.GetPixbuf (((IMember)node).StockIcon, Gtk.IconSize.Menu), amb.GetString ((IMember)node, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates));
				}
				entry.Tag = node;
				result.Insert (0, entry);
				node = node.Parent;
			}
			
			PathEntry noSelection = null;
			if (type == null) {
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = new CustomNode (Document.CompilationUnit) };
			} else if (member == null && type.ClassType != ClassType.Delegate) 
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = new CustomNode (type) };
			if (noSelection != null) {
				result.Add (noSelection);
			}
			
			var prev = CurrentPath;
			CurrentPath = result.ToArray ();
			OnPathChanged (new DocumentPathChangedEventArgs (prev));
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			textEditorData = Document.Editor;
			UpdatePath (null, null);
			textEditorData.Caret.PositionChanged += UpdatePath;
			Document.DocumentParsed += delegate { UpdatePath (null, null); };
		}
		
		// Yoinked from C# binding
		class CustomNode : MonoDevelop.Projects.Dom.AbstractNode
		{
			public CustomNode (INode parent)
			{
				this.Parent = parent;
			}
		}
		
		/// <summary>
		/// Move the completion trigger offset to the beginning of the current token
		/// </summary>
		protected virtual int ResetTriggerOffset (CodeCompletionContext completionContext)
		{
			int i = completionContext.TriggerOffset;
			int accumulator = 0;
			
			for (;
			     1 < i && char.IsLetterOrDigit (Editor.GetCharAt (i));
			     --i, ++accumulator);
			completionContext.TriggerOffset = i-1;
			return accumulator+1;
		}// ResetTriggerOffset
		
		[CommandHandler (MonoDevelop.Refactoring.RefactoryCommands.GotoDeclaration)]
		public void GotoDeclaration ()
		{
			LanguageItem item = GetLanguageItemAt (Editor.Caret.Location);
			if (item != null)
				IdeApp.Workbench.OpenDocument ((FilePath)item.File, (int)item.Line, 1, true);
		}
		
		[CommandUpdateHandler (MonoDevelop.Refactoring.RefactoryCommands.GotoDeclaration)]
		public void CanGotoDeclaration (CommandInfo item)
		{
			item.Visible = (GetLanguageItemAt (Editor.Caret.Location) != null);
			item.Bypass = !item.Visible;
		}
		
		private LanguageItem GetLanguageItemAt (DocumentLocation location)
		{
			CProject project = Document.Project as CProject;
			string token = GetTokenAt (location);
			if (project != null && !string.IsNullOrEmpty (token)) {
				ProjectInformation info = ProjectInformationManager.Instance.Get (project);
				return info.AllItems ().FirstOrDefault (i => i.Name.Equals (token, StringComparison.Ordinal));
			}
			
			return null;
		}
		
		private string GetTokenAt (DocumentLocation location)
		{
			int lineOffset = location.Column-1;
			string line = Editor.GetLineText (location.Line);
			int first = line.LastIndexOfAny (allowedChars, lineOffset)+1,
			    last = line.IndexOfAny (allowedChars, lineOffset);
			if (last < 0) last = line.Length - 1;
			string token = string.Empty;
			    
			if (first >= 0 && first < last && last < line.Length) {
				token = line.Substring (first, last-first);
			}
			
			return token.Trim ();
		}
	}
}
