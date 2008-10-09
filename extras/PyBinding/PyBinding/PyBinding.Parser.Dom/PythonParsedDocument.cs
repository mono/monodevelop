// PythonParsedDocument.cs
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace PyBinding.Parser.Dom
{
	public class PythonParsedDocument : ParsedDocument
	{
		public PythonParsedDocument (string fileName) : base (fileName)
		{
		}

		public PythonModule Module {
			get;
			set;
		}

		public void Parse (XmlTextReader reader, string content)
		{
			this.CompilationUnit = new PythonCompilationUnit (this.FileName);
			var streamContent = new StringReader (content);
			ReadFromXml (reader, streamContent);
			ReadComments (content);
			(this.CompilationUnit as PythonCompilationUnit).Build ();
		}

		public void ReadComments (string content)
		{
			if (Module == null)
				return;

			string[] parts = content.Split ('\n');
			int blockStart = -1;
			StringBuilder text = new StringBuilder ();

			for (int i = 0; i < parts.Length; i++)
			{
				if (parts[i].Trim ().StartsWith ("#"))
				{
					text.AppendLine (parts[i]);
					if (blockStart < 0)
					{
						blockStart = i + 1;
					}
				}
				else
				{
					if (blockStart >= 0)
					{
						var region = new DomRegion (blockStart + 1, 0, i, parts[i - 1].Length);

						Module.Comments.Add (new PythonComment () {
							Region    = region,
							Text      = text.ToString (),
							MultiLine = i - 1 > blockStart,
						});

						text       = new StringBuilder ();
						blockStart = -1;
					}
				}
			}
		}

		/// <summary>
		/// This method will convert the xml output from the parsing
		/// python process and add it to the parsed document.
		/// </summary>
		/// <param name="xml">
		/// A <see cref="XmlTextReader"/>
		/// </param>
		public void ReadFromXml (XmlTextReader xml, StringReader content)
		{
			if (xml == null)
				throw new ArgumentNullException ("xml cannot be null");

			XmlDocument xmlDoc = new XmlDocument ();

			xmlDoc.Load (xml);
			XmlElement root = xmlDoc.DocumentElement;

			if (root.LocalName == "error") {
				ExtractError (root);
			}
			else if (root.LocalName == "module") {
				BuildFromXmlElement (root, content);
			}
			else {
				Console.WriteLine (root.LocalName);
				Debug.Assert (false, "Assert not reached");
			}

			(this.CompilationUnit as PythonCompilationUnit).Module = Module;
		}

		void ExtractError (XmlElement element)
		{
			Add (new Error (ErrorType.Error,
			                Int32.Parse (element.GetAttribute ("line")),
							Int32.Parse (element.GetAttribute ("column")),
			                element.InnerText));
		}

		/// <summary>
		/// Walks the xml element tree to build a result. This expects
		/// rootElement to be a &lt;module /&gt; element.
		/// <param name="rootElement">
		/// A <see cref="XmlElement"/>
		/// </param>
		/// </summary>
		/// <param name="rootElement">
		/// A <see cref="XmlElement"/>
		/// </param>
		void BuildFromXmlElement (XmlElement rootElement, StringReader content)
		{
			Debug.Assert (rootElement.LocalName == "module");
			string moduleName = String.Empty;

			if (!String.IsNullOrEmpty (FileName))
				moduleName = PythonHelper.ModuleFromFilename (FileName);

			Module = new PythonModule () {
				FullName = moduleName,
				Region   = GetDomRegion (rootElement),
			};

			foreach (XmlElement child in rootElement) {
				switch (child.LocalName) {
				case "import":
					BuildImport (child);
					break;
				case "class":
					BuildClass (child);
					break;
				case "attribute":
					BuildAttribute (child);
					break;
				case "function":
					BuildFunction (child);
					break;
				case "warning":
					BuildWarning (child);
					break;
				default:
					Debug.Assert (false, "Assert not reached");
					break;
				}
			}
		}

		DomRegion GetDomRegion (XmlElement element)
		{
			int lineNumber = Int32.Parse (element.GetAttribute ("line"));
			int endLine = lineNumber;
			if (element.HasAttribute ("endline"))
				endLine = Int32.Parse (element.GetAttribute ("endline"));
			return new DomRegion (lineNumber + 1, endLine + 1);
		}

		void BuildWarning (XmlElement element)
		{
			Add (new Error (ErrorType.Warning,
			                Int32.Parse (element.GetAttribute ("line")), 0,
			                element.InnerText));
		}

		void BuildImport (XmlElement element)
		{
			Module.Imports.Add (new PythonImport () {
				Region = GetDomRegion (element),
				Name   = element.GetAttribute ("name")
			});
		}

		void BuildAttribute (XmlElement element)
		{
			Module.Attributes.Add (new PythonAttribute () {
				Name   = element.GetAttribute ("name"),
				Region = GetDomRegion (element)
			});
		}

		void BuildAttribute (XmlElement element, PythonClass pyClass)
		{
			pyClass.Attributes.Add (new PythonAttribute () {
				Name   = element.GetAttribute ("name"),
				Region = GetDomRegion (element)
			});
		}

		void BuildFunction (XmlElement element)
		{
			PythonFunction pyFunc;

			Module.Functions.Add (pyFunc = new PythonFunction () {
				Name   = element.GetAttribute ("name"),
				Region = GetDomRegion (element),
			});

			foreach (XmlElement child in element)
			{
				if (child.LocalName == "doc")
				{
					pyFunc.Documentation = element.InnerText.Trim ();
				}
				else if (child.LocalName == "local")
				{
					BuildLocal (child, pyFunc);
				}
				else if (child.LocalName == "argument")
				{
					BuildArgument (child, pyFunc);
				}
			}
		}

		void BuildArgument (XmlElement element, PythonFunction pyFunc)
		{
			pyFunc.Arguments.Add (new PythonArgument () {
				Name       = element.GetAttribute("name"),
				Region     = pyFunc.Region,
			});
		}

		void BuildLocal (XmlElement element, PythonFunction pyFunc)
		{
			pyFunc.Locals.Add (new PythonLocal () {
				Name   = element.GetAttribute ("name"),
				Region = GetDomRegion (element)
			});
		}

		void BuildFunction (XmlElement element, PythonClass pyClass)
		{
			PythonFunction pyFunc;

			pyClass.Functions.Add (pyFunc = new PythonFunction () {
				Name   = element.GetAttribute ("name"),
				Region = GetDomRegion (element),
			});

			foreach (XmlElement child in element)
				if (child.LocalName == "doc")
					pyFunc.Documentation = element.InnerText.Trim ();
				else if (child.LocalName == "argument")
					BuildArgument (child, pyFunc);
		}

		void BuildClass (XmlElement element)
		{
			PythonClass pyClass;
			
			Module.Classes.Add (pyClass = new PythonClass () {
				Name     = element.GetAttribute ("name"),
				Region   = GetDomRegion (element)
			});

			// PythonClasses can have Attributes or Functions directly
			// inside them. They can also have doc elements which we
			// we can set.

			foreach (XmlElement child in element) {
				switch (child.LocalName) {
				case "doc":
					pyClass.Documentation = child.InnerText;
					break;
				case "attribute":
					BuildAttribute (child, pyClass);
					break;
				case "function":
					BuildFunction (child, pyClass);
					break;
				default:
					Debug.Assert (false, "Assert not reached");
					break;
				}
			}
		}

		public override IEnumerable<FoldingRegion> GenerateFolds ()
		{
			if (Module == null)
				yield break;

			foreach (FoldingRegion region in GenerateImportFolds ())
				yield return region;
			
			foreach (PythonComment pyComment in Module.Comments)
				if (pyComment.MultiLine)
					yield return new FoldingRegion (pyComment.Region);

			foreach (FoldingRegion region in GenerateClassFolds ())
				yield return region;

			foreach (PythonFunction pyFunc in Module.Functions)
				yield return new FoldingRegion (pyFunc.Region);
		}

		IEnumerable<FoldingRegion> GenerateImportFolds ()
		{
			if (Module == null)
				yield break;

			var en = Module.Imports.GetEnumerator ();

			if (!en.MoveNext ())
				yield break;

			var first = en.Current;
			var last  = first;

			while (en.MoveNext ())
			{
				if (first == null)
				{
					first = last;
				}
				else if (en.Current.Region.Start.Line - 1 != last.Region.End.Line)
				{
					yield return new FoldingRegion (new DomRegion (first.Region.Start, last.Region.End));
					first = null;
				}
				last = en.Current;
			}

			if (first != null && last != null)
				yield return new FoldingRegion (new DomRegion (first.Region.Start, last.Region.End));
		}

		IEnumerable<FoldingRegion> GenerateClassFolds ()
		{
			if (Module == null)
				yield break;

			foreach (PythonClass pyClass in Module.Classes)
			{
				yield return new FoldingRegion (pyClass.Region);

				foreach (FoldingRegion region in GenerateFunctionFolds (pyClass))
					yield return region;
			}
		}

		IEnumerable<FoldingRegion> GenerateFunctionFolds (PythonClass pyClass)
		{
			if (Module == null)
				yield break;

			foreach (PythonFunction pyFunc in pyClass.Functions)
			{
				yield return new FoldingRegion (pyFunc.Region);
				// TODO: Look for inner classes
			}
		}
	}
}