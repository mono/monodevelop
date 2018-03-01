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
using System.Linq;
using MonoDevelop.Refactoring;
using System.Collections.Generic;
using MonoDevelop.UnitTesting;
using MonoDevelop.Core;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Collections.Immutable;

namespace MonoDevelop.CSharp
{
	class UnitTestTextEditorExtension : AbstractUnitTestTextEditorExtension
	{
		static readonly IList<UnitTestLocation> emptyList = new UnitTestLocation[0];

		static bool HasMethodMarkerAttribute (SemanticModel model, IUnitTestMarkers[] markers)
		{
			var compilation = model.Compilation;
			foreach (var marker in markers)
				if (compilation.GetTypeByMetadataName (marker.TestMethodAttributeMarker) != null)
					return true;
			return false;
		}

		public override Task<IList<UnitTestLocation>> GatherUnitTests (IUnitTestMarkers[] unitTestMarkers, CancellationToken token)
		{
			var parsedDocument = DocumentContext.ParsedDocument;
			if (parsedDocument == null)
				return Task.FromResult (emptyList);
			
			var semanticModel = parsedDocument.GetAst<SemanticModel> ();
			if (semanticModel == null)
				return Task.FromResult (emptyList);

			if (!HasMethodMarkerAttribute (semanticModel, unitTestMarkers))
				return Task.FromResult (emptyList);

			var visitor = new NUnitVisitor (semanticModel, unitTestMarkers, token);
			try {
				visitor.Visit (semanticModel.SyntaxTree.GetRoot (token));
			} catch (OperationCanceledException) {
				throw;
			}catch (Exception ex) {
				LoggingService.LogError ("Exception while analyzing ast for unit tests.", ex);
				return Task.FromResult (emptyList);
			}
			return Task.FromResult (visitor.FoundTests);
		}

		class NUnitVisitor : CSharpSyntaxWalker
		{
			readonly SemanticModel semanticModel;
			readonly CancellationToken token;
			readonly IUnitTestMarkers [] unitTestMarkers;
			List<UnitTestLocation> foundTests = new List<UnitTestLocation> ();
			HashSet<ClassDeclarationSyntax> unitTestClasses = new HashSet<ClassDeclarationSyntax> ();
			public IList<UnitTestLocation> FoundTests {
				get {
					return foundTests;
				}
			}

			public NUnitVisitor (SemanticModel semanticModel, IUnitTestMarkers[] unitTestMarkers, CancellationToken token)
			{
				this.semanticModel = semanticModel;
				this.token = token;
				this.unitTestMarkers = unitTestMarkers;
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
				var sb = StringBuilderCache.Allocate ();
				ImmutableArray<TypedConstant> args;
				if (attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments [0].Kind == TypedConstantKind.Array)
					args = attr.ConstructorArguments [0].Values;
				else
					args = attr.ConstructorArguments;

				for (int i = 0; i < args.Length; i++)
				{
					if (i > 0)
						sb.Append (", ");

					AddArgument (args [i], sb);
				}
				return StringBuilderCache.ReturnAndFree (sb);
			}

			static void AddArgument(TypedConstant arg, StringBuilder sb)
			{
				if (arg.Kind == TypedConstantKind.Array) {
					sb.Append ("[");
					for (int i = 0; i < arg.Values.Length; i++)
					{
						if (i > 0)
							sb.Append (", ");
						
						AddArgument (arg.Values [i], sb);
					}
					sb.Append ("]");
				} else
					AppendConstant (sb, arg.Value);
			}

			public override void VisitMethodDeclaration (MethodDeclarationSyntax node)
			{
				var method = semanticModel.GetDeclaredSymbol (node);
				if (method == null)
					return;
				var parentClass = node.Parent as ClassDeclarationSyntax;
				if (parentClass == null)
					return;
				UnitTestLocation test = null;
				IUnitTestMarkers markers = null;
				foreach (var attr in method.GetAttributes ()) {
					var cname = attr.AttributeClass.GetFullName ();
					markers = unitTestMarkers.FirstOrDefault (m => (m.TestMethodAttributeMarker == cname || m.TestCaseMethodAttributeMarker == cname));
					if (markers != null) {
						if (test == null) {
							TagClass (parentClass, markers);
							test = new UnitTestLocation (node.Identifier.SpanStart);
							test.UnitTestIdentifier = GetFullName (parentClass) + "." + method.Name;
							foundTests.Add (test);
						}
						break;
					}
				}
				if (test != null) {
					foreach (var attr in method.GetAttributes ()) {
						if (attr.AttributeClass.GetFullName () == markers.TestCaseMethodAttributeMarker) {
							test.TestCases.Add ("(" + BuildArguments (attr) + ")");
						} else
							test.IsIgnored |= attr.AttributeClass.GetFullName () == markers.IgnoreTestMethodAttributeMarker;
					}
				}
			}

			void TagClass (ClassDeclarationSyntax c, IUnitTestMarkers markers)
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
						test.IsIgnored |= attr.AttributeClass.GetFullName () == markers.IgnoreTestClassAttributeMarker;
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

