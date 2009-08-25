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

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Projects.Gui.Completion;

using PyBinding;
using PyBinding.Parser;
using PyBinding.Parser.Dom;

namespace PyBinding.Gui
{
	public class PythonEditorCompletion : CompletionTextEditorExtension
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
		}

		public override bool ExtendsEditor (Document doc, IEditableTextBuffer editor)
		{
			if (doc == null || String.IsNullOrEmpty (doc.Name))
				return false;
			return Path.GetExtension (doc.Name) == ".py";
		}

		public override ICompletionDataList HandleCodeCompletion (ICodeCompletionContext completionContext, char completionChar)
		{
			switch (completionChar) {
			case ' ':
			case '.':
				PythonParsedDocument doc = Document.ParsedDocument as PythonParsedDocument;
				if (doc != null)
					return GenerateCompletionData (completionContext, doc, Editor);
				return null;
			default:
				return null;
			}
		}
		
		public IEnumerable<ICompletionData> SelfDotCompletionData (PythonClass klass)
		{
			foreach (var attr in klass.Attributes)
				yield return new CompletionData (attr.Name, s_ImgAttr, attr.Documentation);
			foreach (var func in klass.Functions)
				yield return new CompletionData (func.Name, s_ImgFunc, func.Documentation);
		}
		
		ICompletionDataList GenerateCompletionData (ICodeCompletionContext completionContext, PythonParsedDocument document, TextEditor editor)
		{
			if (document == null || document.Module == null)
				return null;
			
			var triggerWord = GetTriggerWord (editor, completionContext);
			
			// "self."
			if (triggerWord.Equals ("self.")) {
				var klass = GetClass (document.Module, completionContext.TriggerLine);
				if (klass == null)
					return null; // nothing to complete, self not in a class
				return new CompletionDataList (SelfDotCompletionData (klass));
			}
			
			// anything in the sqlite store
			if (!String.IsNullOrEmpty (triggerWord)) {
				// todo: try to complete on class/module/func/attr data
				// todo: limit to just the next word rather than full phrase
				
				return new CompletionDataList (
					from ParserItem item in m_site.Database.Find (triggerWord)
				    where !item.FullName.Substring (triggerWord.Length).Contains ('.')
					select CreateCompletionData (item, triggerWord))
					;
			}
			
			if (m_site != null) {
				return new CompletionDataList (
					from ParserItem item in m_site.Database.Find ("", ParserItemType.Module)
					where !item.FullName.Contains ('.')
					select CreateCompletionData (item, triggerWord))
					;
			}
			
			return null;
		}
		
		static ICompletionData CreateCompletionData (ParserItem item, string triggerWord)
		{
			var name = item.FullName.Substring (triggerWord.Length);
			return new CompletionData (name, IconForType (item), item.Documentation, name);
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
		
		static string GetTriggerWord (TextEditor editor, ICodeCompletionContext completionContext)
		{
			// Get the word we are trying to complete.  Please, please tell
			// me there is a better way to do this.
			
			var line = editor.GetLineText (completionContext.TriggerLine);
			var length = completionContext.TriggerLineOffset;
			if (length > line.Length)
				length = line.Length;
			var word = line.Substring (0, length);
			if (word.Contains (' '))
				word = word.Substring (word.LastIndexOf (' '), word.Length - word.LastIndexOf (' '));
			word = word.Trim ();
			
			return word;
		}
	}
}