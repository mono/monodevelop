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
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Diagnostics.MonoTODODiagnostic
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	sealed class MonoTODODiagnosticAnalyzer : DiagnosticAnalyzer
	{
		static readonly ImmutableArray<OperationKind> operationKindsOfInterest = ImmutableArray.Create (
			OperationKind.FieldReference,
			OperationKind.EventReference,
			OperationKind.MethodReference,
			OperationKind.PropertyReference,
			OperationKind.ObjectCreation,
			OperationKind.VariableDeclarator,
			OperationKind.SimpleAssignment,
			OperationKind.SizeOf
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

		static IEnumerable<IAssemblySymbol> GetSearchAssemblies(Compilation compilation)
		{
			yield return compilation.Assembly;
			foreach (var reference in compilation.References) {
				var symbol = compilation.GetAssemblyOrModuleSymbol (reference);
				if (symbol is IAssemblySymbol assemblySymbol)
					yield return assemblySymbol;
			}
		}

		const string MonoTODOAttributeName = "System.MonoTODOAttribute";
		public override void Initialize(AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.RegisterCompilationStartAction (compilationContext => {
				var compilation = compilationContext.Compilation;
				var monoTodoAttributeExists = GetSearchAssemblies (compilation)
				                                   .Any (assemblySymbol => assemblySymbol.GetTypeByMetadataName (MonoTODOAttributeName) != null);
				if (!monoTodoAttributeExists)
					return;

				compilationContext.RegisterOperationAction(
					(nodeContext) => {
						TryFindMonoTODO (nodeContext, nodeContext.CancellationToken);
					},
					operationKindsOfInterest);
			});
		}

		static readonly Dictionary<string, string> attributes = new Dictionary<string, string> {
			{ "MonoTODOAttribute", "Mono TODO" },
			{ "MonoNotSupportedAttribute", "Mono NOT SUPPORTED" },
			{ "MonoLimitationAttribute", "Mono LIMITATION" }
		};

		void TryFindMonoTODO (OperationAnalysisContext nodeContext, CancellationToken cancellationToken)
		{
			(ISymbol, IOperation) symbol, owningSymbol;
			switch (nodeContext.Operation) {
			case IMemberReferenceOperation member:
				symbol = (member.Member, member);
				owningSymbol = (member.Type, member);
				break;
			case IObjectCreationOperation creation:
				symbol = (creation.Constructor, creation);
				owningSymbol = (creation.Type, creation);
				break;
			case IAssignmentOperation assignment:
				symbol = (assignment.Type, assignment.Value);
				owningSymbol = (null, null);
				break;
			case IVariableDeclaratorOperation decl:
				symbol = (null, null);
				owningSymbol = (decl.Symbol.Type, decl);
				break;
			case ISizeOfOperation size:
				symbol = (null, null);
				owningSymbol = (size.TypeOperand, size);
				break;
			default:
				LoggingService.LogError ("Unexpected IOperation type {0} for kind {1}", nodeContext.Operation.GetType ().ToString (), nodeContext.Operation.Kind);
				return;
			}

			if (cancellationToken.IsCancellationRequested)
				return;

			if (ReportDiagnosticForSymbol (nodeContext, symbol))
				return;

			if (cancellationToken.IsCancellationRequested)
				return;
			
			ReportDiagnosticForSymbol (nodeContext, owningSymbol);
		}

		bool ReportDiagnosticForSymbol (OperationAnalysisContext nodeContext, (ISymbol symbol, IOperation op) group)
		{
			if (group.symbol == null)
				return false;
			
			foreach (var attr in group.symbol.GetAttributes ()) {
				if (attr.AttributeClass.ContainingNamespace.GetFullName () != "System")
					continue;
				string val;
				if (attributes.TryGetValue (attr.AttributeClass.Name, out val)) {
					string msg = null;
					if (attr.ConstructorArguments.Length > 0) {
						var arg = attr.ConstructorArguments [0];
						msg = arg.Value != null ? arg.Value.ToString () : null;
					}
					var diagnostic = Diagnostic.Create (descriptor, group.op.Syntax.GetLocation (), string.IsNullOrEmpty (msg) ? val : val + ": " + msg);
					nodeContext.ReportDiagnostic (diagnostic);
					return true;
				}
			}

			return false;
		}
	}
}
