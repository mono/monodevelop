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
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.CSharp.Diagnostics.MonoTODODiagnostic
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	sealed class MonoTODODiagnosticAnalyzer : DiagnosticAnalyzer
	{
		static readonly ImmutableArray<OperationKind> operationKindsOfInterest = ImmutableArray.Create(
			OperationKind.EventReference,
			OperationKind.FieldReference,
			OperationKind.Invocation,
			OperationKind.MethodReference,
			OperationKind.PropertyReference,
			OperationKind.ObjectCreation
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

		const string MonoTODOAttributeName = "System.MonoTODOAttribute";
		const string MonoNotSupportedAttributeName = "MonoNotSupportedAttribute";
		const string MonoLimitationAttributeName = "MonoLimitationAttribute";

		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.RegisterCompilationStartAction (compilationContext => {
				var compilation = compilationContext.Compilation;
				var monoToDoAttribute = compilation.GetTypeByMetadataName (MonoTODOAttributeName);
				var monoNotSupportedAttribute = compilation.GetTypeByMetadataName (MonoNotSupportedAttributeName);
				var monoLimitationAttribute = compilation.GetTypeByMetadataName (MonoLimitationAttributeName);
				if (monoToDoAttribute == null && monoNotSupportedAttribute == null && monoLimitationAttribute == null)
					return;

				compilationContext.RegisterOperationAction(
					(nodeContext) => {
						IOperation operation = nodeContext.Operation;
						ISymbol symbol;
						if (operation is IMemberReferenceOperation memberReference) {
							symbol = memberReference.Member;
						} else if (operation is IInvocationOperation invocation) {
							symbol = invocation.TargetMethod;
						} else if (operation is IObjectCreationOperation creation) {
							symbol = creation.Constructor;
						} else {
							return;
						}

						Diagnostic diagnostic;
						if (TryFindMonoTODO(operation, symbol, monoToDoAttribute, monoNotSupportedAttribute, monoLimitationAttribute, out diagnostic))
							nodeContext.ReportDiagnostic (diagnostic);
					}, operationKindsOfInterest
					);
			});
		}

		bool TryFindMonoTODO (IOperation operation, ISymbol symbol, ISymbol monoToDoAttribute, ISymbol monoNotSupportedAttribute, ISymbol monoLimitationAttribute, out Diagnostic diagnostic)
		{
			diagnostic = default(Diagnostic);

			foreach (var attr in symbol.GetAttributes ()) {
				string val;
				if (attr.AttributeClass == monoToDoAttribute)
					val = "Mono TODO";
				else if (attr.AttributeClass == monoNotSupportedAttribute)
					val = "Mono NOT SUPPORTED";
				else if (attr.AttributeClass == monoLimitationAttribute)
					val = "Mono LIMITATION";
				else
					continue;

				string msg = null;
				if (attr.ConstructorArguments.Length > 0) {
					var arg = attr.ConstructorArguments [0];
					msg = arg.Value?.ToString ();
				}
				diagnostic = Diagnostic.Create (descriptor, operation.Syntax.GetLocation (), string.IsNullOrEmpty (msg) ? val : val + ": " + msg);
				return true;
			}

			return false;
		}
	}
}
