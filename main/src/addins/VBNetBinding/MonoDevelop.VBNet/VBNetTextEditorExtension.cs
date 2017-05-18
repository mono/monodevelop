//
// VBNetTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis;
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MonoDevelop.VBNet
{
	class VBNetTextEditorExtension : TextEditorExtension
	{
		MonoDevelopWorkspace workspace = new MonoDevelopWorkspace (null);
		Microsoft.CodeAnalysis.SyntaxTree parseTree;
		internal static MetadataReference [] DefaultMetadataReferences;

		DocumentId documentId;


		static VBNetTextEditorExtension ()
		{
			try {
				var mscorlib = MetadataReference.CreateFromFile (typeof (Console).Assembly.Location);
				var systemAssembly = MetadataReference.CreateFromFile (typeof (System.Text.RegularExpressions.Regex).Assembly.Location);
				//systemXmlLinq = MetadataReference.CreateFromFile (typeof(System.Xml.Linq.XElement).Assembly.Location);
				var systemCore = MetadataReference.CreateFromFile (typeof (Enumerable).Assembly.Location);
				DefaultMetadataReferences = new [] {
					mscorlib,
					systemAssembly,
					systemCore,
					//systemXmlLinq
				};
			} catch (Exception e) {
				Console.WriteLine (e);
			}

		}

		protected override void Initialize ()
		{
			base.Initialize ();

			parseTree = VisualBasicSyntaxTree.ParseText (Editor.Text);
			var sourceText = SourceText.From (Editor.Text);

			var projectId = ProjectId.CreateNewId ();
			documentId = DocumentId.CreateNewId (projectId);
			var projectInfo = ProjectInfo.Create (
				projectId,
				VersionStamp.Create (),
				"TestProject",
				"TestProject",
				LanguageNames.VisualBasic,
				null,
				null,
				new VisualBasicCompilationOptions (
					OutputKind.DynamicallyLinkedLibrary
				),
				new VisualBasicParseOptions (),
				new [] {
					DocumentInfo.Create(
						documentId,
						Editor.FileName,
						null,
						SourceCodeKind.Regular,
						TextLoader.From(TextAndVersion.Create(sourceText, VersionStamp.Create())),
						filePath: Editor.FileName
					)
				},
				null,
				DefaultMetadataReferences
			);
			var sInfo = SolutionInfo.Create (
				SolutionId.CreateNewId (),
				VersionStamp.Create (),
				null,
				new [] { projectInfo }
			);
			workspace.OpenSolutionInfo (sInfo);

			Editor.SyntaxHighlighting = new RoslynClassificationHighlighting (workspace, documentId, "source.vb");
			workspace.InformDocumentOpen (documentId, Editor);
		}

		public override void Dispose ()
		{
			base.Dispose ();
			workspace.CloseDocument (documentId);
		}
	}
}
