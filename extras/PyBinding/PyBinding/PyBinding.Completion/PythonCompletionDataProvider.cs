// PythonCompletionDataProvider.cs
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
using System.Xml;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui.Completion;

using PyBinding.Parser.Dom;

namespace PyBinding.Completion
{
	public class PythonCompletionDataProvider : ICompletionDataProvider
	{
		string                 m_DefaultCompletionString = String.Empty;
		ICodeCompletionContext m_CompletionContext       = null;
		TextEditor             m_Editor                  = null;

		public PythonCompletionDataProvider (ICodeCompletionContext completionContext,
		                                     Document               document,
		                                     TextEditor             editor)
			: this ()
		{
			Document = document;
			m_CompletionContext = completionContext;
			m_Editor = editor;
		}

		public PythonCompletionDataProvider ()
		{
		}

		Document Document {
			get;
			set;
		}

		PythonParsedDocument ParsedDocument {
			get {
				return Document.ParsedDocument as PythonParsedDocument;
			}
		}

		public string DefaultCompletionString {
			get { return this.m_DefaultCompletionString; }
		}

		public bool AutoCompleteUniqueMatch {
			get { return false; }
		}

		PythonClass OurClass {
			get;
			set;
		}

		bool InClass {
			get {
				if (ParsedDocument.Module == null)
					return false;

				foreach (PythonClass pyClass in ParsedDocument.Module.Classes)
				{
					if (InRegion (pyClass.Region, m_CompletionContext.TriggerLine))
					{
						OurClass = pyClass;
						return true;
					}
				}

				// todo: classes in functions?
				return false;
			}
		}

		PythonFunction OurFunc {
			get;
			set;
		}

		bool InFunc {
			get {
				if (ParsedDocument.Module == null)
					return false;

				foreach (PythonFunction pyFunc in ParsedDocument.Module.Functions)
				{
					if (InRegion (pyFunc.Region, m_CompletionContext.TriggerLine))
					{
						OurFunc = pyFunc;
						return true;
					}
				}
				foreach (PythonClass pyClass in ParsedDocument.Module.Classes)
				{
					foreach (PythonFunction pyFunc in pyClass.Functions)
					{
						if (InRegion (pyFunc.Region, m_CompletionContext.TriggerLine))
						{
							OurFunc = pyFunc;
							return true;
						}
					}
				}
				return false;
			}
		}

		bool InRegion (DomRegion region, int lineNumber)
		{
			return region.Start.Line <= lineNumber && region.End.Line >= lineNumber;
		}

		public ICompletionData[] GenerateCompletionData (ICompletionWidget widget, char charTyped)
		{
			string line = m_Editor.GetLineText (m_CompletionContext.TriggerLine + 1);
			bool inClass = InClass;
			bool inFunc = InFunc;
			string trimmed = line.Trim ();

			List<ICompletionData> results = new List<ICompletionData> ();

			if (inClass && trimmed.StartsWith ("def"))
			{
				return GenerateClassFunctionsSpecial ();
			}
			else if (trimmed.StartsWith ("class"))
			{
				var classes = GenerateClasses ();
				if (classes != null)
					results.AddRange (classes);
			}
			else if (inClass && trimmed.StartsWith ("self."))
			{
				var classAttrs = GenerateClassAttributes ();
				var classFuncs = GenerateClassFunctions ();

				if (classAttrs != null)
					results.AddRange (classAttrs);

				if (classFuncs != null)
					results.AddRange (classFuncs);
				
				return results.ToArray ();
			}
			else
			{
				var classes = GenerateClasses ();
				if (classes != null)
					results.AddRange (classes);

				if (inFunc)
				{
					var locals = GenerateLocals ();
					if (locals != null)
						results.AddRange (locals);
				}
			}

			return null;
		}

		public void Dispose ()
		{
		}

		const string s_ImgAttr  = "md-property";
		const string s_ImgFunc  = "md-method";
		const string s_ImgClass = "md-class";

		ICompletionData[] GenerateLocals ()
		{
			if (OurFunc == null)
				return null;

			List<ICompletionData> results = new List<ICompletionData> ();

			foreach (PythonLocal pyLocal in OurFunc.Locals)
			{
				results.Add (new PythonCompletionData () {
					Image = s_ImgAttr,
					Text = new string[] { pyLocal.Name },
					CompletionString = pyLocal.Name
				});
			}

			return results.ToArray ();
		}

		ICompletionData[] GenerateClasses ()
		{
			if (ParsedDocument.Module == null)
				return null;

			List<ICompletionData> results = new List<ICompletionData> ();

			foreach (PythonClass pyClass in ParsedDocument.Module.Classes)
			{
				results.Add (new PythonCompletionData () {
					Image = s_ImgClass,
					Text = new string[] { pyClass.Name },
					CompletionString = String.Format ("{0}(", pyClass.Name),
					Description = pyClass.Documentation.Trim ()
				});
			}

			return results.ToArray ();
		}

		ICompletionData[] GenerateClassFunctions ()
		{
			if (OurClass == null)
				return null;

			List<ICompletionData> results = new List<ICompletionData> ();

			foreach (PythonFunction pyFunc in OurClass.Functions)
			{
				results.Add (new PythonCompletionData () {
					Image = s_ImgFunc,
					Text = new string[] { pyFunc.Name },
					CompletionString = String.Format ("{0}(", pyFunc.Name),
					Description = pyFunc.Documentation
				});
			}

			return results.ToArray ();
		}

		ICompletionData[] GenerateClassAttributes ()
		{
			if (OurClass == null)
				return null;

			List<ICompletionData> results = new List<ICompletionData> ();

			foreach (PythonAttribute pyAttr in OurClass.Attributes)
			{
				results.Add (new PythonCompletionData () {
					Image = s_ImgAttr,
					Text = new string[] { pyAttr.Name },
					CompletionString = pyAttr.Name
				});
			}

			return results.ToArray ();
		}

		ICompletionData[] GenerateClassFunctionsSpecial ()
		{
			return new ICompletionData[] {
				new PythonCompletionData () {
					Image = s_ImgFunc,
					Text = new string[] { "__init__" },
					CompletionString = "__init__(self, "
				},
				new PythonCompletionData () {
					Image = s_ImgFunc,
					Text = new string[] { "__str__" },
					CompletionString = "__str__(self):"
				},
				new PythonCompletionData () {
					Image = s_ImgFunc,
					Text = new string[] { "__repr__" },
					CompletionString = "__repr__(self):"
				},
				new PythonCompletionData () {
					Image = s_ImgFunc,
					Text = new string[] { "__getattribute__" },
					CompletionString = "__getattribute__(self, attr):"
				},
				new PythonCompletionData () {
					Image = s_ImgFunc,
					Text = new string[] { "__setattr__" },
					CompletionString = "__setattr__(self, attr, value):"
				},
				new PythonCompletionData () {
					Image = s_ImgFunc,
					Text = new string[] { "__delattr__" },
					CompletionString = "__delattr__(self, attr):"
				},
			};
		}
	}

	public class PythonCompletionData : ICompletionData
	{
		public string Image {
			get;
			set;
		}

		public string[] Text {
			get;
			set;
		}

		public string Description {
			get;
			set;
		}

		public string CompletionString {
			get;
			set;
		}
	}
}