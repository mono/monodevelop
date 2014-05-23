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
using MonoDevelop.Refactoring;
using System.Collections.Generic;
using MonoDevelop.NUnit;
using MonoDevelop.Core;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp
{
	class UnitTestTextEditorExtension : AbstractUnitTestTextEditorExtension
	{
		static readonly IList<UnitTestLocation> emptyResult = new List<UnitTestLocation> ();

		public override async Task<IList<UnitTestLocation>> GatherUnitTests (CancellationToken token)
		{
			var analysisDocument = document.AnalysisDocument;
			if (analysisDocument == null)
				return emptyResult;
			var semanticModel = await analysisDocument.GetSemanticModelAsync (token);

			var visitor = new NUnitVisitor (semanticModel, token);
			try {
				visitor.Visit (semanticModel.SyntaxTree.GetRoot (token));
			} catch (OperationCanceledException) {
				throw;
			}catch (Exception ex) {
				LoggingService.LogError ("Exception while analyzing ast for unit tests.", ex);
				return null;
			}
			return visitor.FoundTests;
		}

		class NUnitVisitor : CSharpSyntaxWalker
		{
			readonly SemanticModel semanticModel;
			readonly CancellationToken token;
			List<UnitTestLocation> foundTests = new List<UnitTestLocation> ();
			HashSet<ClassDeclarationSyntax> unitTestClasses = new HashSet<ClassDeclarationSyntax> ();
			public IList<UnitTestLocation> FoundTests {
				get {
					return foundTests;
				}
			}

			public NUnitVisitor (SemanticModel semanticModel, CancellationToken token)
			{
				this.semanticModel = semanticModel;
				this.token = token;
			}

			static string GetFullName (ClassDeclarationSyntax typeDeclaration)
			{
				var parts = new List<string> ();
				while (true) {
					parts.Add (typeDeclaration.Identifier.ToString ());
					if (typeDeclaration.Parent is ClassDeclarationSyntax) {
						typeDeclaration = (ClassDeclarationSyntax)typeDeclaration.Parent;
					}
					else {
						break;
					}
				}
				;
				var ns = typeDeclaration.Parent as NamespaceDeclarationSyntax;
				if (ns != null)
					parts.Add (ns.Name.ToString ());
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

			static string BuildArguments (AttributeData attr)
			{
				var sb = new StringBuilder ();
				foreach (var arg in attr.ConstructorArguments) {
					if (sb.Length > 0)
						sb.Append (", ");
//					var cr = arg as ConversionResolveResult;
//					if (cr != arg.Value) {
//						AppendConstant (sb, cr.Input.ConstantValue);
//						continue;
//					}
					AppendConstant (sb, arg.Value);
				}
				return sb.ToString ();
			}

			public override void VisitMethodDeclaration (MethodDeclarationSyntax node)
			{
				var method = semanticModel.GetDeclaredSymbol (node);
				if (method == null)
					return;
				var parentClass = (ClassDeclarationSyntax)node.Parent;
				UnitTestLocation test = null;
				foreach (var attr in method.GetAttributes ()) {
					if (attr.AttributeClass.GetFullName () == "NUnit.Framework.TestAttribute") {
						if (test == null) {
							TagClass (parentClass);
							test = new UnitTestLocation (node.Identifier.SpanStart);
							test.UnitTestIdentifier = GetFullName (parentClass) + "." + method.Name;
							foundTests.Add (test);
						}
					}
				}
				if (test != null) {
					foreach (var attr in method.GetAttributes ()) {
						if (attr.AttributeClass.GetFullName () == "NUnit.Framework.TestCaseAttribute") {
							test.TestCases.Add ("(" + BuildArguments (attr) + ")");
						} else
							test.IsIgnored |= attr.AttributeClass.GetFullName () == "NUnit.Framework.IgnoreAttribute";
					}
				}
			}

			void TagClass (ClassDeclarationSyntax c)
			{
				if (unitTestClasses.Contains (c))
					return;
				unitTestClasses.Add (c);

				var type = semanticModel.GetDeclaredSymbol (c);
				var test = new UnitTestLocation (c.Identifier.SpanStart);
				test.IsFixture = true;
				test.UnitTestIdentifier = GetFullName (c);
				foundTests.Add (test);

				if (test != null) {
					foreach (var attr in type.GetAttributes ()) {
							test.IsIgnored |= attr.AttributeClass.GetFullName () == "NUnit.Framework.IgnoreAttribute";
					}
				}
			}

			public override void VisitBlock (BlockSyntax node)
			{
				token.ThrowIfCancellationRequested ();
			}
		}
	}
}

