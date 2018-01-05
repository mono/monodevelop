//
// RazorParserTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.AspNet.Razor;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.AspNet.Tests.Razor
{
	[TestFixture]
	public class RazorParserTests : TestBase
	{
		Solution solution;

		[TearDown]
		public override void TearDown ()
		{
			TypeSystemServiceTestExtensions.UnloadSolution (solution);
			base.TearDown ();
		}

		[Test]
		public async Task PreprocessedFileUsesPreprocessorRazorHost ()
		{
			var document = await Parse ("@{ }", isPreprocessed: true);
			var method = document.PageInfo.CSharpSyntaxTree
				.GetRoot ()
				.DescendantNodes ()
				.OfType <MethodDeclarationSyntax> ()
				.FirstOrDefault (m => m.Identifier.ValueText == "Generate");

			Assert.IsNotNull (method);
		}

		async Task<RazorCSharpParsedDocument> Parse (string text, bool isPreprocessed)
		{
			var project = Services.ProjectService.CreateDotNetProject ("C#", "AspNetApp");

			project.FileName = UnitTests.TestBase.GetTempFile (".csproj");
			string file = UnitTests.TestBase.GetTempFile (".cshtml");
			ProjectFile projectFile = project.AddFile (file);
			if (isPreprocessed)
				projectFile.Generator = "RazorTemplatePreprocessor";

			var sev = new TestViewContent ();
			sev.Owner = project;
			sev.ContentName = file;
			sev.Text = text;

			var tww = new TestWorkbenchWindow ();
			tww.ViewContent = sev;

			var doc = new TestDocument (tww);
			doc.Editor.FileName = sev.ContentName;
			doc.UpdateProject (project);

			solution = new MonoDevelop.Projects.Solution ();
			solution.DefaultSolutionFolder.AddItem (project);
			solution.AddConfiguration ("", true);
			await TypeSystemServiceTestExtensions.LoadSolution (solution);

			var parser = new RazorTestingParser {
				Doc = doc
			};
			var options = new ParseOptions {
				Project = project,
				FileName = file,
				Content = new StringTextSource (text)
			};
			return (RazorCSharpParsedDocument)parser.Parse (options, default(CancellationToken)).Result;
		}
	}
}

