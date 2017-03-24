//
// MonoTODODiagnosticAnalyzer.cs
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

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Diagnostics.MonoTODODiagnostic
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	sealed class MonoTODODiagnosticAnalyzer : DiagnosticAnalyzer
	{
		static readonly ImmutableArray<SyntaxKind> syntaxKindsOfInterest = ImmutableArray.Create(
			SyntaxKind.IdentifierName,                // foo
			SyntaxKind.SimpleMemberAccessExpression,  // foo.bar
			SyntaxKind.PointerMemberAccessExpression, // foo->bar
			SyntaxKind.ConditionalAccessExpression    // foo?.bar
		);

		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
			IDEDiagnosticIds.MonoTODODiagnosticDiagnosticId,
			"Find APIs marked as TODO in Mono",
			"{0}",
			DiagnosticCategory.Style,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics {
			get {
				return ImmutableArray.Create(descriptor);
			}
		}

		public override void Initialize(AnalysisContext context)
		{
			context.RegisterSyntaxNodeAction(
				(nodeContext) => {
					Diagnostic diagnostic;
					if (TryFindMonoTODO(nodeContext.SemanticModel, nodeContext.Node, out diagnostic, nodeContext.CancellationToken))
						nodeContext.ReportDiagnostic (diagnostic);
				},
				syntaxKindsOfInterest);
		}

		static readonly Dictionary<string, string> attributes = new Dictionary<string, string> {
			{ "MonoTODOAttribute", "Mono TODO" },
			{ "MonoNotSupportedAttribute", "Mono NOT SUPPORTED" },
			{ "MonoLimitationAttribute", "Mono LIMITATION" }
		};

		bool TryFindMonoTODO (SemanticModel semanticModel, SyntaxNode node, out Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			var info = semanticModel.GetSymbolInfo (node);
			diagnostic = default(Diagnostic);
			if (info.Symbol == null)
				return false;

			foreach (var attr in info.Symbol.GetAttributes ()) {
				if (attr.AttributeClass.ContainingNamespace.GetFullName () != "System")
					continue;
				string val;
				if (attributes.TryGetValue (attr.AttributeClass.Name, out val)) {
					string msg = null;
					if (attr.ConstructorArguments.Length > 0) {
						var arg = attr.ConstructorArguments [0];
						msg = arg.Value != null ? arg.Value.ToString () : null;
					}
					var tree = semanticModel.SyntaxTree;
					diagnostic = Diagnostic.Create(descriptor, tree.GetLocation(node.Span), string.IsNullOrEmpty (msg) ? val : val + ": " + msg);
					return true;
				}
			}
			return false;
		}
	}
}