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

using PyBinding.Parser.Dom;

namespace PyBinding.Gui
{
	public class PythonEditorCompletion : CompletionTextEditorExtension
	{
		public override void Initialize ()
		{
			base.Initialize ();
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
			case '=':
			case '.':
				PythonParsedDocument doc = Document.ParsedDocument as PythonParsedDocument;
				if (doc != null)
					return GenerateCompletionData (completionContext, doc, Editor);
				return null;
			default:
				return null;
			}
		}
		
		static ICompletionDataList GenerateCompletionData (ICodeCompletionContext completionContext,
		                                                   PythonParsedDocument document, TextEditor editor)
		{
			if (document == null || document.Module == null)
				return null;
			
			CompletionDataList results;
			string trimmedLine = editor.GetLineText (completionContext.TriggerLine + 1).Trim ();
			PythonClass cls = GetClass (document.Module, completionContext.TriggerLine);
			
			if (cls != null && trimmedLine.StartsWith ("def"))
				return GenerateClassFunctionsSpecial ();
			
			if (trimmedLine.StartsWith ("class"))
				return new CompletionDataList (GenerateClassData (document.Module));
			
			if (cls != null && trimmedLine.StartsWith ("self.")) {
				results = new CompletionDataList ();
				results.AddRange (
					from PythonAttribute pyAttr in cls.Attributes
					select (ICompletionData) new CompletionData (pyAttr.Name, s_ImgAttr));
				results.AddRange (
					from PythonFunction pyFunc in cls.Functions
					select (ICompletionData) new CompletionData (pyFunc.Name, s_ImgFunc, pyFunc.Documentation,
					                                             String.Format ("{0}(", pyFunc.Name)));
				return results;
			}
			
			results = new CompletionDataList ();
			
			if (cls != null) {
				results.AddRange (GenerateClassData (document.Module));
			}
			
			PythonFunction func = GetFunc (document.Module, completionContext.TriggerLine);
			if (func != null) {
				results.AddRange (
					from PythonLocal pyLocal in func.Locals
					select (ICompletionData) new CompletionData (pyLocal.Name, s_ImgAttr));
			}
				
			return results;
		}
		
		static PythonClass GetClass (PythonModule module, int line)
		{
			foreach (PythonClass pyClass in module.Classes)
				if (InRegion (pyClass.Region, line))
					return pyClass;

				// todo: classes in functions?
				return null;
		}
		
		static PythonFunction GetFunc (PythonModule module, int line)
		{
			foreach (PythonFunction pyFunc in module.Functions)
				if (InRegion (pyFunc.Region, line))
					return pyFunc;
			
			foreach (PythonClass pyClass in module.Classes)
				foreach (PythonFunction pyFunc in pyClass.Functions)
					if (InRegion (pyFunc.Region, line))
						return pyFunc;
			
			return null;
		}

		static bool InRegion (DomRegion region, int lineNumber)
		{
			return region.Start.Line <= lineNumber && region.End.Line >= lineNumber;
		}
		
		static IEnumerable<ICompletionData> GenerateClassData (PythonModule module)
		{
			return from PythonClass pyClass in module.Classes
			       select (ICompletionData) new CompletionData (pyClass.Name, s_ImgClass,
			                                                    pyClass.Documentation.Trim (),
			                                                    String.Format ("{0}(", pyClass.Name));
		}

		const string s_ImgAttr  = "md-property";
		const string s_ImgFunc  = "md-method";
		const string s_ImgClass = "md-class";

		static ICompletionDataList GenerateClassFunctionsSpecial ()
		{
			CompletionDataList list = new CompletionDataList ();
			list.Add ("__init__", s_ImgFunc, null, "__init__(self, ");
			list.Add ("__str__", s_ImgFunc, null, "__str__(self):");
			list.Add ("__repr__", s_ImgFunc, null, "__repr__(self):");
			list.Add ("__getattribute__", s_ImgFunc, null, "__getattribute__(self, attr):");
			list.Add ("__setattr__", s_ImgFunc, null, "__setattr__(self, attr, value):");
			list.Add ("__delattr__", s_ImgFunc, null, "__delattr__(self, attr):");
			return list;
		}
	}
}
