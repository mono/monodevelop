// PythonEditorCompletion.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.CodeCompletion;

using PyBinding;
using PyBinding.Parser;
using PyBinding.Parser.Dom;
using MonoDevelop.Components;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide;

namespace PyBinding.Gui
{
	public class PythonEditorCompletion : CompletionTextEditorExtension, IPathedDocument
	{
		const string s_ImgModule = "md-package";
		const string s_ImgAttr   = "md-property";
		const string s_ImgFunc   = "md-method";
		const string s_ImgClass  = "md-class";
		
		PythonSite m_site = null;
		
		public override void Initialize ()
		{
			base.Initialize ();
			
			if (this.Document.HasProject) {
				var config = this.Document.Project.DefaultConfiguration as PythonConfiguration;
				if (config != null)
					m_site = new PythonSite (config.Runtime);
			}
			
			if (m_site == null)
				m_site = new PythonSite (PythonHelper.FindPreferedRuntime ());
				
			UpdatePath (null, null);
			Document.Editor.Caret.PositionChanged += UpdatePath;
			Document.DocumentParsed += delegate { UpdatePath (null, null); };
		}

		public override ICompletionDataList HandleCodeCompletion (CodeCompletionContext completionContext, char completionChar)
		{
			switch (completionChar) {
			case '(':
			case ' ':
			case '=':
			case '\t':
			case '\n':
			case '.':
				PythonParsedDocument doc = Document.ParsedDocument as PythonParsedDocument;
				if (doc != null)
					return GenerateCompletionData (completionContext, doc, Editor, completionChar);
				return null;
			default:
				return null;
			}
		}
		
		public IEnumerable<CompletionData> SelfDotCompletionData (PythonClass klass)
		{
			foreach (var attr in klass.Attributes)
				yield return new CompletionData (attr.Name, s_ImgAttr, attr.Documentation);
			foreach (var func in klass.Functions)
				yield return new CompletionData (func.Name, s_ImgFunc, func.Documentation);
		}
		
		ICompletionDataList GenerateCompletionData (CodeCompletionContext completionContext, PythonParsedDocument document, TextEditorData editor, char completionChar)
		{
			if (document == null)
				return null;
			
			var triggerWord = GetTriggerWord (editor, completionContext);

			// Its annoying when the data is poped up during an assignment such as:
			// abc = _
			if (completionChar == '=' && String.IsNullOrEmpty (triggerWord))
				return null;

			var triggerLine = editor.GetLineText (completionContext.TriggerLine);

			// if completionChar is ' ' and it is not a known completion type
			// that we can handle, return as early as possible
			if (completionChar == ' ') {
				if (!triggerWord.Contains ('.') &&
				    !triggerLine.StartsWith ("class") &&
				    !triggerLine.StartsWith ("def") &&
				    !triggerLine.StartsWith ("from") &&
				    !triggerLine.StartsWith ("import"))
					return null;
			}
			
			// "self."
			if (document.Module != null && triggerWord == "self" && completionChar == '.') {
				var klass = GetClass (document.Module, completionContext.TriggerLine);
				if (klass == null)
					return null; // nothing to complete, self not in a class
				return new CompletionDataList (SelfDotCompletionData (klass));
			}
			
			var inFrom = triggerLine.StartsWith ("from ");
			var inClass = triggerLine.StartsWith ("class ") || (triggerLine.StartsWith ("class") && completionChar == ' ');
			var inDef = triggerLine.StartsWith ("def ") || (triggerLine.StartsWith ("def") && completionChar == ' ');
			var parts = triggerLine.Split (' ');
			
			// "from blah "
			if (inFrom && parts.Length == 2 && parts [parts.Length-1].Trim ().Length > 0 && completionChar == ' ') {
				return new CompletionDataList (new CompletionData[] { new CompletionData ("import") });
			}
			// "from blah import "
			else if (inFrom && parts.Length > 2) {
				triggerWord = parts [1] + ".";
				return new CompletionDataList (
					from ParserItem item in m_site.Database.Find (triggerWord)
				    where !item.FullName.Substring (triggerWord.Length).Contains ('.')
					select CreateCompletionData (item, triggerWord))
					;
			}
			
			// if we are in a new class line and not to '(' yet
			// we cannot complete anything at this time, finish now
			if (inClass && parts.Length < 2)
				return null;
			
			// if we are in a new def line, the only time we can complete
			// is after an equal '='.  so ignore space trigger
			if (inDef && completionChar == ' ')
				return null;
			else if (inDef && completionChar == '=')
				triggerWord = "";
			
			if (inClass) {
				if (completionChar == '(')
					triggerWord = "";
				else
					triggerWord = triggerLine.Substring (triggerLine.LastIndexOf ('(') + 1);
			}
			
			// limit the depth of search to number of "." in trigger
			// "xml." has depth of 1 so anything matching ^xml. and no more . with match
			int depth = 0;
			foreach (var c in triggerWord)
				if (c == '.')
					depth++;
			
			// anything in the sqlite store
			if (!String.IsNullOrEmpty (triggerWord)) {
				// todo: try to complete on class/module/func/attr data
				
				return new CompletionDataList (
					from ParserItem item in m_site.Database.Find (triggerWord, ParserItemType.Any, depth)
					select CreateCompletionData (item, triggerWord))
					;
			}
			
			ParserItemType itemType = String.IsNullOrEmpty (triggerWord) ? ParserItemType.Module : ParserItemType.Any;
			
			return new CompletionDataList (
				from ParserItem item in m_site.Database.Find ("", itemType, depth)
				select CreateCompletionData (item, triggerWord))
				;
		}
		
		static CompletionData CreateCompletionData (ParserItem item, string triggerWord)
		{
			return CreateCompletionData (item, triggerWord, "");
		}
		
		static CompletionData CreateCompletionData (ParserItem item, string triggerWord, string suffix)
		{
			var name = item.FullName.Substring (triggerWord.Length);
			return new CompletionData (name, IconForType (item), item.Documentation, name + suffix);
		}
		
		static string IconForType (ParserItem item)
		{
			switch (item.ItemType) {
			case ParserItemType.Module:
				return s_ImgModule;
			case ParserItemType.Class:
				return s_ImgClass;
			case ParserItemType.Function:
				return s_ImgFunc;
			case ParserItemType.Attribute:
			case ParserItemType.Local:
				return s_ImgAttr;
			default:
				return String.Empty;
			}
		}
		
		static PythonClass GetClass (PythonModule module, int line)
		{
			foreach (PythonClass pyClass in module.Classes)
				if (InRegion (pyClass.Region, line))
					return pyClass;
				// todo: classes in functions?
				return null;
		}
		
//		static PythonFunction GetFunc (PythonModule module, int line)
//		{
//			foreach (PythonFunction pyFunc in module.Functions)
//				if (InRegion (pyFunc.Region, line))
//					return pyFunc;
//			
//			foreach (PythonClass pyClass in module.Classes)
//				foreach (PythonFunction pyFunc in pyClass.Functions)
//					if (InRegion (pyFunc.Region, line))
//						return pyFunc;
//			
//			return null;
//		}

		static bool InRegion (DomRegion region, int lineNumber)
		{
			return region.Start.Line <= lineNumber && region.End.Line >= lineNumber;
		}
		
		static string GetTriggerWord (TextEditorData editor, CodeCompletionContext completionContext)
		{
			// Get the line of text for our current line
			// and trim off everything after the cursor
			var line = editor.GetLineText (completionContext.TriggerLine);
			line = line.Substring (0, completionContext.TriggerLineOffset - 1);
			
			// Walk backwards looking for split chars and then trim the
			// beginning of the line off
			for (int i = line.Length - 1; i >= 0; i--) {
				switch (line [i]) {
				case ' ':
				case '(':
				case '\t':
				case '=':
					return line.Substring (i + 1, line.Length - 1 - i);
				default:
					break;
				}
			}
			
			return line;
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
		
		protected virtual void OnPathChanged (object sender, DocumentPathChangedEventArgs args)
		{
			if (PathChanged != null)
				PathChanged (sender, args);
		}
		#endregion
		
		// Yoinked from C# binding
		void UpdatePath (object sender, Mono.TextEditor.DocumentLocationEventArgs e)
		{
			var unit = Document.CompilationUnit;
			var textEditorData = Document.Editor;
			
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
					FoldingRegion reg = Document.ParsedDocument.UserRegions.LastOrDefault (r => r.Region.Contains (loc.Line, loc.Column));
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
			OnPathChanged (this, new DocumentPathChangedEventArgs (prev));
		}
		
		// Yoinked from C# binding
		class CustomNode : MonoDevelop.Projects.Dom.AbstractNode
		{
			public CustomNode (INode parent)
			{
				this.Parent = parent;
			}
		}
	}
}
