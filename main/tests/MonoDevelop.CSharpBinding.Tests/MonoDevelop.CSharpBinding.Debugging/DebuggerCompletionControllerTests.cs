//
// DebuggerCompletionControllerTests.cs
//
// Author:
//       jason <jaimison@microsoft.com>
//
// Copyright (c) 2019 
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
using System.Linq;
using System.Threading.Tasks;
using Mono.Debugging.Client;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Debugger;
using MonoDevelop.Debugger;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Documents;
using NUnit.Framework;
using Microsoft.VisualStudio.Platform;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.CSharpBinding.Debugging
{
	[TestFixture]
	public class DebuggerCompletionControllerTests : IdeTestBase
	{
		async Task<Ide.Gui.Document> GetDocument(string text)
		{
			var documentManager = await Runtime.GetService<DocumentManager> ();
			FilePath filePath = "foo.cs";
			var content = TestHelper.ToStream (text);
			var descriptor = new FileDescriptor (filePath, "text/x-csharp", content, null);
			var file = new FileDocumentController ();
			await file.Initialize (descriptor);
			return await documentManager.OpenDocument (file);
		}

		Microsoft.CodeAnalysis.Document GetAnalysisDocument(string text)
		{
			var workspace = new AdhocWorkspace ();

			string projectName = "TestProject";
			var projectId = ProjectId.CreateNewId ();
			var versionStamp = VersionStamp.Create ();
			var projectInfo = ProjectInfo.Create (projectId, versionStamp, projectName, projectName, LanguageNames.CSharp);
			var sourceText = SourceText.From (text);
			var project = workspace.AddProject (projectInfo);
			return workspace.AddDocument (project.Id, "Program.cs", sourceText);
		}

		[Test]
		public async Task ProvidesCompletions()
		{
			var contentType = MimeTypeCatalog.Instance.GetContentTypeForMimeType ("text/x-csharp");

			var text = @"
namespace console61
	{
		class MainClass
		{
			public static void Main (string [] args)
			{
				$Console.WriteLine(2);$
			}

			static void Method2 (int a)
			{
			}
		}
	}
";

			int startOfStatement = text.IndexOf ('$');
			if (startOfStatement >= 0)
				text = text.Substring (0, startOfStatement) + text.Substring (startOfStatement + 1);
			int endOfStatement = text.IndexOf ('$');
			if (endOfStatement >= 0)
				text = text.Substring (0, endOfStatement) + text.Substring (endOfStatement + 1);

			var buffer = PlatformCatalog.Instance.TextBufferFactoryService.CreateTextBuffer (text, contentType);
			var doc = GetAnalysisDocument (text);
			var controller = new DebuggerCompletionProvider (doc, buffer);

			var snapshot = buffer.CurrentSnapshot;
			var startLine = snapshot.GetLineFromPosition (startOfStatement);
			var startColumn = startOfStatement - startLine.Start.Position;
			var endLine = snapshot.GetLineFromPosition (endOfStatement);
			var endColumn = endOfStatement - endLine.Start.Position;

			var completionResult =
				await controller.GetExpressionCompletionDataAsync ("a", new StackFrame (0, new SourceLocation ("", "", startLine.LineNumber, startColumn, endLine.LineNumber, endColumn), "C#"), default);

			var items = completionResult.Items.Select(i => i.Name);
			Assert.That (items, Contains.Item ("args"));
			Assert.That (items, Contains.Item ("MainClass"));
			Assert.That (items, Contains.Item ("Method2"));
			Assert.AreEqual (1, completionResult.ExpressionLength);
		}
	}
}