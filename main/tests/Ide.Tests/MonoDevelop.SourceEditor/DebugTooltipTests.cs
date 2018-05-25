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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Extension;
using System.Collections.Generic;

namespace MonoDevelop.SourceEditor
{
	[TestFixture]
	public class DebugTooltipTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharpWithReferences;
		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			yield return new CSharpCompletionTextEditorExtension ();
		}

		async Task<TextEditorExtensionTestCase> CreateDocument ()
		{
			var text = content;
			int endPos = text.IndexOf ('$');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			var testCase = await SetupTestCase (text, Math.Max (0, endPos));
			await testCase.Document.UpdateParseDocument ();

			return testCase;
		}

		const string content = @"using System;

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

		static string ResolveExpression (TextEditorExtensionTestCase testCase, int offset)
		{
			var doc = testCase.Document;
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
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("basicLocalVariable", ResolveExpression (testCase, GetBasicOffset ("basicLocalVariable")));
			}
		}

		[Test]
		public async Task TestConstructorInvocation ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("DebuggerTooltipTests.Abc", ResolveExpression (testCase, GetCtorOffset ("new Abc ()")));
			}
		}

		[Test]
		public async Task TestCastExpression ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("((Abc) instanceVariable).Text", ResolveExpression (testCase, GetPropertyOffset ("((Abc) instanceVariable).Text")));
				Assert.AreEqual ("((Abc) instanceVariable).Text.Length", ResolveExpression (testCase, GetPropertyOffset ("((Abc) instanceVariable).Text.Length")));
			}
		}

		[Test]
		public async Task TestPropertyOfMethodInvocation ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("instanceVariable.GetType ().Name", ResolveExpression (testCase, GetPropertyOffset ("instanceVariable.GetType ().Name")));
			}
		}

		[Test]
		public async Task TestFieldDeclarations ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("DebuggerTooltipTests.Abc.StaticField", ResolveExpression (testCase, GetBasicOffset ("StaticField")));
				Assert.AreEqual ("@double", ResolveExpression (testCase, GetBasicOffset ("@double")));
				Assert.AreEqual ("field", ResolveExpression (testCase, GetBasicOffset ("field")));
			}
		}

		[Test]
		public async Task TestPropertyDeclarations ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("DebuggerTooltipTests.Abc.StaticProperty", ResolveExpression (testCase, GetBasicOffset ("StaticProperty")));
				Assert.AreEqual ("Text", ResolveExpression (testCase, GetBasicOffset ("Text")));
			}
		}

		[Test]
		public async Task TestPropertySetter ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("value", ResolveExpression (testCase, GetBasicOffset ("value")));
			}
		}

		[Test]
		public async Task TestMethodParameters ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("this", ResolveExpression (testCase, GetBasicOffset ("this")));
				Assert.AreEqual ("this.Text", ResolveExpression (testCase, GetPropertyOffset ("this.Text")));
				Assert.AreEqual ("this.Text.Length", ResolveExpression (testCase, GetPropertyOffset ("this.Text.Length")));
				Assert.AreEqual ("abc", ResolveExpression (testCase, GetBasicOffset ("abc")));
				Assert.AreEqual ("abc.Text", ResolveExpression (testCase, GetPropertyOffset ("abc.Text")));
				Assert.AreEqual ("abc.Text.Length", ResolveExpression (testCase, GetPropertyOffset ("abc.Text.Length")));
			}
		}

		[Test]
		public async Task TestBaseExpressions ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("base.BaseProperty", ResolveExpression (testCase, GetPropertyOffset ("base.BaseProperty")));
			}
		}

		[Test]
		public async Task TestEscapedVariables ()
		{
			using (var testCase = await CreateDocument ()) {

				// Inside class Abc
				Assert.AreEqual ("@class", ResolveExpression (testCase, GetBasicOffset ("@class")));
				Assert.AreEqual ("@class.Length", ResolveExpression (testCase, GetPropertyOffset ("@class.Length")));
				Assert.AreEqual ("@double.Length", ResolveExpression (testCase, GetPropertyOffset ("@double.Length")));
				Assert.AreEqual ("this.@double.Length", ResolveExpression (testCase, GetPropertyOffset ("this.@double.Length")));
				Assert.AreEqual ("abc.@double.Length", ResolveExpression (testCase, GetPropertyOffset ("abc.@double.Length")));
			}
		}

		[Test]
		public async Task TestPropertyInitializers ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("propertyInitializer.Property", ResolveExpression (testCase, GetAssignmentOffset ("Property = string.Empty")));
			}
		}

		[Test]
		public async Task TestDefaultValueParameters ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual ("defaultValue", ResolveExpression (testCase, GetAssignmentOffset ("defaultValue = 5")));
			}
		}

		[Test]
		public async Task TestMethodInvocations ()
		{
			using (var testCase = await CreateDocument ()) {
				Assert.AreEqual (null, ResolveExpression (testCase, GetPropertyOffset ("instanceVariable.Method")));
				Assert.AreEqual (null, ResolveExpression (testCase, GetBasicOffset ("Method")));
			}
		}
	}
}
