//
// DebugTooltipTests.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using NUnit.Framework;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Debugger;
using MonoDevelop.CSharp.Completion;

namespace MonoDevelop.SourceEditor
{
	[TestFixture]
	public class DebugTooltipTests : MonoDevelop.Ide.IdeTestBase
	{
		Document document;
		string content;
		MonoDevelop.Projects.Solution solution;

		async Task<Document> CreateDocument (string input)
		{
			var text = input;
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			if (solution != null) {
				TypeSystemService.Unload (solution);
				solution.Dispose ();
			}
			
			var project = Services.ProjectService.CreateDotNetProject ("C#");
			project.Name = "test";
			project.References.Add (MonoDevelop.Projects.ProjectReference.CreateAssemblyReference ("mscorlib"));
			project.References.Add (MonoDevelop.Projects.ProjectReference.CreateAssemblyReference ("System, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"));
			project.References.Add (MonoDevelop.Projects.ProjectReference.CreateAssemblyReference ("System.Core"));

			project.FileName = "test.csproj";
			project.Files.Add (new ProjectFile ("/a.cs", BuildAction.Compile)); 

			solution = new MonoDevelop.Projects.Solution ();
			solution.AddConfiguration ("", true); 
			solution.DefaultSolutionFolder.AddItem (project);
			using (var monitor = new ProgressMonitor ())
				await TypeSystemService.Load (solution, monitor);

			var tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			tww.ViewContent = content;
			content.ContentName = "/a.cs";
			content.Data.MimeType = "text/x-csharp";
			content.Project = project;


			content.Text = text;
			content.CursorPosition = Math.Max (0, endPos);
			var doc = new Document (tww);
			doc.SetProject (project);

			var compExt = new CSharpCompletionTextEditorExtension ();
			compExt.Initialize (doc.Editor, doc);
			content.Contents.Add (compExt);

			await doc.UpdateParseDocument ();

			return doc;
		}

		public async Task Init ()
		{
			if (document != null)
				return;
			
			content = @"using System;

namespace DebuggerTooltipTests
{
	class Base
	{
		public string BaseProperty { get; set; }

		protected Base ()
		{
		}
	}

	class Abc : Base
	{
		// TEST: make sure we can resolve member variable declarations
		static int StaticProperty { get; set; }
		static int StaticField = 5;

		string @double;
		string field;

		public Abc ()
		{
		}

		public string Text {
			get { return string.Empty; }
			set {
				// TEST: make sure that we can resolve the variable being set
				Console.WriteLine (value);

				// TEST: make sure that we don't resolve method invocations
				Method (null);
			}
		}

		public string Property {
			get; set;
		}

		// TEST: make sure that we can resolve parameters...
		public bool Method (Abc abc)
		{
			// TEST: make sure we can resolve everything here...
			return this.Text.Length == abc.Text.Length;
		}

		public bool BaseTest ()
		{
			// TEST: make sure we can resolve these...
			return base.BaseProperty == 5;
		}

		public void AtVariables (Abc abc)
		{
			// TEST: make sure that we can resolve @variables
			var @class = ""resolve me"";
			var result = @class.Length;
			result = @double.Length;
			result = this.@double.Length;
			result = abc.@double.Length;
		}

		// TEST: make sure that we can resolve parameters with default values
		public void DefaultValues (int defaultValue = 5)
		{
			if (defaultValue == 5)
				Console.WriteLine (""it's the default value!!"");
		}
	}

	class MainClass
	{
		public static void Main (string[] args)
		{
			// TEST: make sure that a simple local can be resolved
			int basicLocalVariable = 5;

			// TEST: make sure that a .ctor can be resolved
			var instanceVariable = new Abc ();

			// TEST: make sure that we don't resolve method invocations
			instanceVariable.Method (null);

			// TEST: make sure that the cast, Text, and Text.Length can be resolved
			var castingLocalVariable = ((Abc) instanceVariable).Text.Length;

			// TEST: make sure that 'Name' can be resolved
			var invokingVariable = instanceVariable.GetType ().Name;

			// TEST: make sure that property initializers can be resolved
			var propertyInitializer = new Abc () {
				Property = string.Empty
			};
		}
	}
}
";

			document = await CreateDocument (content);
		}

		public override void TearDown()
		{
			if (solution != null) {
				TypeSystemService.Unload (solution);
				solution.Dispose ();
			}
			base.TearDown ();
		}

		static string ResolveExpression (Document doc, string content, int offset)
		{
			var editor = doc.Editor;
			var loc = editor.OffsetToLocation (offset);
			var resolver = doc.GetContent<IDebuggerExpressionResolver> ();

			return resolver.ResolveExpressionAsync (editor, doc, offset, default(System.Threading.CancellationToken)).Result.Text;
		}

		int GetBasicOffset (string expr)
		{
			int startOffset = content.IndexOf (expr, StringComparison.Ordinal);

			return startOffset + (expr.Length / 2);
		}

		int GetAssignmentOffset (string expr)
		{
			int startOffset = content.IndexOf (expr, StringComparison.Ordinal);
			int length = expr.IndexOf ('=');

			while (expr[length - 1] == ' ')
				length--;

			return startOffset + (length / 2);
		}

		int GetCtorOffset (string expr)
		{
			int startOffset = content.IndexOf (expr, StringComparison.Ordinal);
			int length = expr.IndexOf ('(');

			while (expr[length - 1] == ' ')
				length--;

			int dot = expr.LastIndexOf ('.', length, length - 4);
			if (dot != -1)
				return startOffset + dot + ((length - dot) / 2);

			return startOffset + 4 + ((length - 4) / 2);
		}

		int GetPropertyOffset (string expr)
		{
			int startOffset = content.IndexOf (expr, StringComparison.Ordinal);
			int dot = expr.LastIndexOf ('.');

			if (dot != -1)
				return startOffset + dot + ((expr.Length - dot) / 2);

			return startOffset + (expr.Length / 2);
		}

		[Test]
		public async Task TestBasicLocalVariable ()
		{
			await Init ();
			Assert.AreEqual ("basicLocalVariable", ResolveExpression (document, content, GetBasicOffset ("basicLocalVariable")));
		}

		[Test]
		public async Task TestConstructorInvocation ()
		{
			await Init ();
			Assert.AreEqual ("DebuggerTooltipTests.Abc", ResolveExpression (document, content, GetCtorOffset ("new Abc ()")));
		}

		[Test]
		public async Task TestCastExpression ()
		{
			await Init ();
			Assert.AreEqual ("((Abc) instanceVariable).Text", ResolveExpression (document, content, GetPropertyOffset ("((Abc) instanceVariable).Text")));
			Assert.AreEqual ("((Abc) instanceVariable).Text.Length", ResolveExpression (document, content, GetPropertyOffset ("((Abc) instanceVariable).Text.Length")));
		}

		[Test]
		public async Task TestPropertyOfMethodInvocation ()
		{
			await Init ();
			Assert.AreEqual ("instanceVariable.GetType ().Name", ResolveExpression (document, content, GetPropertyOffset ("instanceVariable.GetType ().Name")));
		}

		[Test]
		public async Task TestFieldDeclarations ()
		{
			await Init ();
			Assert.AreEqual ("DebuggerTooltipTests.Abc.StaticField", ResolveExpression (document, content, GetBasicOffset ("StaticField")));
			Assert.AreEqual ("@double", ResolveExpression (document, content, GetBasicOffset ("@double")));
			Assert.AreEqual ("field", ResolveExpression (document, content, GetBasicOffset ("field")));
		}

		[Test]
		public async Task TestPropertyDeclarations ()
		{
			await Init ();
			Assert.AreEqual ("DebuggerTooltipTests.Abc.StaticProperty", ResolveExpression (document, content, GetBasicOffset ("StaticProperty")));
			Assert.AreEqual ("Text", ResolveExpression (document, content, GetBasicOffset ("Text")));
		}

		[Test]
		public async Task TestPropertySetter ()
		{
			await Init ();
			Assert.AreEqual ("value", ResolveExpression (document, content, GetBasicOffset ("value")));
		}

		[Test]
		public async Task TestMethodParameters ()
		{
			await Init ();
			Assert.AreEqual ("this", ResolveExpression (document, content, GetBasicOffset ("this")));
			Assert.AreEqual ("this.Text", ResolveExpression (document, content, GetPropertyOffset ("this.Text")));
			Assert.AreEqual ("this.Text.Length", ResolveExpression (document, content, GetPropertyOffset ("this.Text.Length")));
			Assert.AreEqual ("abc", ResolveExpression (document, content, GetBasicOffset ("abc")));
			Assert.AreEqual ("abc.Text", ResolveExpression (document, content, GetPropertyOffset ("abc.Text")));
			Assert.AreEqual ("abc.Text.Length", ResolveExpression (document, content, GetPropertyOffset ("abc.Text.Length")));
		}

		[Test]
		public async Task TestBaseExpressions ()
		{
			await Init ();
			Assert.AreEqual ("base.BaseProperty", ResolveExpression (document, content, GetPropertyOffset ("base.BaseProperty")));
		}

		[Test]
		public async Task TestEscapedVariables ()
		{
			await Init ();

			// Inside class Abc
			Assert.AreEqual ("@class", ResolveExpression (document, content, GetBasicOffset ("@class")));
			Assert.AreEqual ("@class.Length", ResolveExpression (document, content, GetPropertyOffset ("@class.Length")));
			Assert.AreEqual ("@double.Length", ResolveExpression (document, content, GetPropertyOffset ("@double.Length")));
			Assert.AreEqual ("this.@double.Length", ResolveExpression (document, content, GetPropertyOffset ("this.@double.Length")));
			Assert.AreEqual ("abc.@double.Length", ResolveExpression (document, content, GetPropertyOffset ("abc.@double.Length")));
		}

		[Test]
		public async Task TestPropertyInitializers ()
		{
			await Init ();
			Assert.AreEqual ("propertyInitializer.Property", ResolveExpression (document, content, GetAssignmentOffset ("Property = string.Empty")));
		}

		[Test]
		public async Task TestDefaultValueParameters ()
		{
			await Init ();
			Assert.AreEqual ("defaultValue", ResolveExpression (document, content, GetAssignmentOffset ("defaultValue = 5")));
		}

		[Test]
		public async Task TestMethodInvocations ()
		{
			await Init ();
			Assert.AreEqual (null, ResolveExpression (document, content, GetPropertyOffset ("instanceVariable.Method")));
			Assert.AreEqual (null, ResolveExpression (document, content, GetBasicOffset ("Method")));
		}
	}
}
