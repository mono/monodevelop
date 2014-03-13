//
// UnitTestTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.Collections.Generic;
using MonoDevelop.NUnit;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using System.Text;

namespace MonoDevelop.CSharp
{
	class UnitTestTextEditorExtension : AbstractUnitTestTextEditorExtension
	{
		public override IList<UnitTestLocation> GatherUnitTests ()
		{
			var resolver = document.GetSharedResolver ();
			if (resolver == null || resolver.Result == null)
				return null;
			var parsedDocument = document.ParsedDocument;
			if (parsedDocument == null)
				return null;
			var syntaxTree = parsedDocument.GetAst<SyntaxTree> ();
			if (syntaxTree == null)
				return null;

			var visitor = new NUnitVisitor (resolver.Result);
			try {
				visitor.VisitSyntaxTree (syntaxTree);
			} catch (Exception ex) {
				LoggingService.LogError ("Exception while analyzing ast for unit tests.", ex);
				return null;
			}
			return visitor.FoundTests;
		}

		class NUnitVisitor : DepthFirstAstVisitor
		{
			readonly CSharpAstResolver resolver;
			List<UnitTestLocation> foundTests = new List<UnitTestLocation> ();

			public IList<UnitTestLocation> FoundTests {
				get {
					return foundTests;
				}
			}

			public NUnitVisitor (CSharpAstResolver resolver)
			{
				this.resolver = resolver;
			}

			static string GetFullName (TypeDeclaration typeDeclaration)
			{
				var parts = new List<string> ();
				while (true) {
					parts.Add (typeDeclaration.Name);
					if (typeDeclaration.Parent is TypeDeclaration) {
						typeDeclaration = (TypeDeclaration)typeDeclaration.Parent;
					}
					else {
						break;
					}
				}
				;
				var ns = typeDeclaration.Parent as NamespaceDeclaration;
				if (ns != null)
					parts.Add (ns.FullName);
				parts.Reverse ();
				return string.Join (".", parts);
			}

			static void AppendConstant (StringBuilder sb, object constantValue)
			{
				if (constantValue is string)
					sb.Append ('"');
				if (constantValue is char)
					sb.Append ('\"');
				sb.Append (constantValue);
				if (constantValue is string)
					sb.Append ('"');
				if (constantValue is char)
					sb.Append ('\"');
			}

			static string BuildArguments (IAttribute attr)
			{
				var sb = new StringBuilder ();
				foreach (var arg in attr.PositionalArguments) {
					if (sb.Length > 0)
						sb.Append (", ");
					var cr = arg as ConversionResolveResult;
					if (cr != null) {
						AppendConstant (sb, cr.Input.ConstantValue);
						continue;
					}
					AppendConstant (sb, arg.ConstantValue);
				}
				return sb.ToString ();
			}

			public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
			{
				var result = resolver.Resolve (methodDeclaration) as MemberResolveResult;
				if (result == null)
					return;
				var method = result.Member as IMethod;
				if (method == null)
					return;

				UnitTestLocation test = null;
				foreach (var attr in method.Attributes) {
					if (attr.AttributeType.ReflectionName == "NUnit.Framework.TestAttribute") {
						if (test == null) {
							test = new UnitTestLocation (methodDeclaration.NameToken.StartLocation.Line);
							test.UnitTestIdentifier = GetFullName ((TypeDeclaration)methodDeclaration.Parent) + "." + methodDeclaration.Name;
							foundTests.Add (test);
						}
					}
				}
				if (test != null) {
					foreach (var attr in method.Attributes) {
						if (attr.AttributeType.ReflectionName == "NUnit.Framework.TestCaseAttribute") {
							test.TestCases.Add ("(" + BuildArguments (attr) + ")");
						} else
							test.IsIgnored |= attr.AttributeType.ReflectionName == "NUnit.Framework.IgnoreAttribute";
					}
				}
			}

			public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
			{
				if (typeDeclaration.HasModifier (Modifiers.Abstract))
					return;
				var result = resolver.Resolve (typeDeclaration);
				if (result == null || result.Type.GetDefinition () == null)
					return;
				UnitTestLocation unitTest = null;
				bool isIgnored = false;
				foreach (var attr in result.Type.GetDefinition ().GetAttributes ()) {
					if (attr.AttributeType.ReflectionName == "NUnit.Framework.TestFixtureAttribute") {
						unitTest = new UnitTestLocation (typeDeclaration.NameToken.StartLocation.Line);
						unitTest.IsFixture = true;
						unitTest.UnitTestIdentifier = GetFullName (typeDeclaration);
						foundTests.Add (unitTest);
					} else
						isIgnored |= attr.AttributeType.ReflectionName == "NUnit.Framework.IgnoreAttribute";
				}
				if (unitTest != null) {
					unitTest.IsIgnored = isIgnored;
					base.VisitTypeDeclaration (typeDeclaration);
				}
			}

			public override void VisitBlockStatement (BlockStatement blockStatement)
			{
			}
		}
	}
}

