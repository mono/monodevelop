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

using PyBinding.Parser.Dom;

namespace PyBinding.Parser
{
	public class PythonParsedDocument : ParsedDocument
	{
		public PythonParsedDocument (string fileName) : base (fileName)
		{
		}

		public void Parse (XmlTextReader reader, string content)
		{
			var streamContent = new StringReader (content);
			ReadComments (content);
			ReadFromXml (reader, streamContent);
		}

		public void ReadComments (string content)
		{
			string[] parts = content.Split ('\n');
			int blockStart = -1;
			int offset = 0;
			StringBuilder text = new StringBuilder ();

			for (int i = 0; i < parts.Length; i++)
			{
				if (parts[i].Trim ().StartsWith ("#"))
				{
					text.AppendLine (parts[i]);
					if (blockStart < 0)
					{
						offset = parts[i].IndexOf ("#");
						blockStart = i + 1;
					}
				}
				else
				{
					if (blockStart >= 0)
					{
						Console.WriteLine ("Offset {0}", offset);
						var region = new DomRegion (blockStart + 1, 0, i, parts[i - 1].Length);

						Add (new Comment () {
							Region = region,
							Text = text.ToString (),
							OpenTag = "#",
							ClosingTag = String.Empty,
							CommentStartsLine = true,
							CommentType = i - 1 > blockStart ? CommentType.MultiLine : CommentType.SingleLine,
						});

						text       = new StringBuilder ();
						blockStart = -1;
						offset     = 0;
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
			this.CompilationUnit = new PythonCompilationUnit (this.FileName);

			xmlDoc.Load (xml);
			XmlElement root = xmlDoc.DocumentElement;

			if (root.LocalName == "error") {
				Console.WriteLine ("found an error");
			}
			else if (root.LocalName == "module") {
				BuildFromXmlElement (root, content);
			}
			else {
				Console.WriteLine (root.LocalName);
				Debug.Assert (false, "Assert not reached");
			}
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

			foreach (XmlElement child in rootElement) {
				switch (child.LocalName) {
				case "import":
					BuildImport (child);
					break;
				case "class":
					BuildClass (child, content);
					break;
				case "attribute":
					BuildAttribute (child);
					break;
				case "function":
					BuildFunction (child);
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

		DomLocation GetDomLocation (XmlElement element)
		{
			int lineNumber = Int32.Parse (element.GetAttribute ("line"));
			return new DomLocation (lineNumber + 1, 0);
		}

		void BuildImport (XmlElement element)
		{
			var compUnit = this.CompilationUnit as PythonCompilationUnit;
			Console.WriteLine ("Import({0})", element.GetAttribute ("name"));

			var region = GetDomRegion (element);
			var domUsing = new DomUsing (region, element.GetAttribute ("name"));
			compUnit.Add (domUsing);
		}

		void BuildAttribute (XmlElement element)
		{
			var compUnit = this.CompilationUnit as PythonCompilationUnit;
			Console.WriteLine ("Attribute({0})", element.GetAttribute ("name"));

			var location = GetDomLocation (element);
			var field = new DomField ();
			field.Location = location;
			field.Name = element.GetAttribute ("name");

			var mod = Modifiers.None;
			if (field.Name.StartsWith ("_"))
				mod |= Modifiers.Private;
			else
				mod |= Modifiers.Public;
			field.Modifiers = mod;

			compUnit.Add (field);
		}

		void BuildFunction (XmlElement element)
		{
			var compUnit = this.CompilationUnit as PythonCompilationUnit;
			Console.WriteLine ("Function({0})", element.GetAttribute ("name"));

			var name = element.GetAttribute ("name");
			var mod = Modifiers.None;
			if (name.StartsWith ("_"))
				mod |= Modifiers.Private;
			else
				mod |= Modifiers.Public;

			var start = GetDomLocation (element);
			var region = GetDomRegion (element);
			var func = new DomMethod (name, mod, MethodModifier.None,
			                          start, region);

			compUnit.Add (func);
		}

		void BuildClass (XmlElement element, StringReader content)
		{
			var compUnit = this.CompilationUnit as PythonCompilationUnit;
			Console.WriteLine ("Class({0})", element.GetAttribute ("name"));

			var name   = element.GetAttribute ("name");
			var start  = GetDomLocation (element);
			var region = GetDomRegion (element);
			
			var fullName = PythonHelper.ModuleFromFilename (FileName);
			var klass = new DomType (fullName) {
				Name            = name,
				Location        = start,
				BodyRegion      = region,
				Modifiers       = Modifiers.Public,
				CompilationUnit = compUnit
			};

			compUnit.Add (klass);
		}

		//
		// -----------------------------------------------------
		// Borrowed from ParsedDocument and overridden
		// -----------------------------------------------------
		//

		public override IEnumerable<FoldingRegion> GenerateFolds ()
		{
			foreach (FoldingRegion fold in AdditionalFolds)
				yield return fold;
			
			foreach (FoldingRegion fold in ConditionalRegions.ToFolds ())
				yield return fold;
			
			IEnumerable<FoldingRegion> commentFolds = Comments.ToPythonFolds ();
			if (CompilationUnit != null && CompilationUnit.Types != null && CompilationUnit.Types.Count > 0) {
				commentFolds = commentFolds.FlagIfInsideMembers (CompilationUnit.Types, delegate (FoldingRegion f) {
					f.Type = FoldType.CommentInsideMember;
				});
			}
			foreach (FoldingRegion fold in commentFolds)
				yield return fold;
			
			if (CompilationUnit == null)
				yield break;
			
			FoldingRegion usingFold = CompilationUnit.Usings.ToFold ();
			if (usingFold != null)
				yield return usingFold;
			
			foreach (FoldingRegion fold in CompilationUnit.Types.ToFolds ())
				yield return fold;

			PythonCompilationUnit pyUnit = CompilationUnit as PythonCompilationUnit;
			foreach (IMember m in pyUnit.Members)
			{
				var fold = new FoldingRegion (m.Name, m.BodyRegion, FoldType.Member);
				yield return fold;
			}
		}
	}

	public static class FoldingUtilities
	{
		public static IEnumerable<FoldingRegion> ToPythonFolds (this IList<Comment> comments)
		{
			for (int i = 0; i < comments.Count; i++) {
				Comment comment = comments[i];
				
				if (comment.CommentType == CommentType.MultiLine) {
					yield return new FoldingRegion ("...", comment.Region, FoldType.Comment);
					continue;
				}
				
				if (!comment.CommentStartsLine)
					continue;
				int j = i;
				int curLine = comment.Region.Start.Line - 1;
				DomLocation end = comment.Region.End;
				
				for (; j < comments.Count; j++) {
					Comment  curComment  = comments[j];
					if (curComment == null || !curComment.CommentStartsLine 
					    || curComment.CommentType != comment.CommentType 
					    || curLine + 1 != curComment.Region.Start.Line)
						break;
					end = curComment.Region.End;
					curLine = curComment.Region.Start.Line;
				}
				
				if (j - i > 1) {
					yield return new FoldingRegion (
						comment.IsDocumentation  ? "/// " : "// "  + comment.Text + "...",
						new DomRegion (comment.Region.Start.Line,
							comment.Region.Start.Column, end.Line, end.Column),
						FoldType.Comment);
					i = j - 1;
				}
			}
		}
	}
}