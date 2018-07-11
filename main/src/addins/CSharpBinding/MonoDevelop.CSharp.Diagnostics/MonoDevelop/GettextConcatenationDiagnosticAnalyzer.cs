//
// GettextConcatenationDiagnosticAnalyzer.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace MonoDevelop.CSharp.Diagnostics
{
	[DiagnosticAnalyzer (LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class GettextConcatenationDiagnosticAnalyzer : LocalizationConcatenationDiagnosticAnalyzer
	{
		protected override string TypeName => "MonoDevelop.Core.GettextCatalog";
	}

	[DiagnosticAnalyzer (LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class MonoAddinsConcantenationDiagnosticAnalyzer : LocalizationConcatenationDiagnosticAnalyzer
	{
		protected override string TypeName => "Mono.Addins.Localization.IAddinLocalizer";
	}

	[DiagnosticAnalyzer (LanguageNames.CSharp, LanguageNames.VisualBasic)]
	sealed class TranslationCatalogConcatenationDiagnosticAnalyzer : LocalizationConcatenationDiagnosticAnalyzer
	{
		// Putting it here for now, don't want to add another assembly to MEF yet
		protected override string TypeName => "Xamarin.Components.Ide.TranslationCatalog";
	}

	abstract class LocalizationConcatenationDiagnosticAnalyzer : DiagnosticAnalyzer
	{
		static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor (
			IDEDiagnosticIds.GettextConcatenationDiagnosticId,
			"GetString calls should not use concatenation",
			"Only literal strings can be passed to GetString for the crawler to work",
			DiagnosticCategory.Style,
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true
		);

		protected abstract string TypeName { get; }

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (descriptor);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.None);

			context.RegisterCompilationStartAction (compilationContext => {
				// Limit search to compilations which reference the specific localizers.
				var compilation = compilationContext.Compilation;
				var type = compilation.GetTypeByMetadataName (TypeName);
				if (type == null)
					return;

				compilationContext.RegisterOperationAction (operationContext => {
					var invocation = (IInvocationOperation)operationContext.Operation;
					var targetMethod = invocation.TargetMethod;

					if (targetMethod == null || targetMethod.Name != "GetString")
						return;

					var containingType = targetMethod.ContainingType;
					if (containingType != type) {
						// Check if we're looking for an interface type.
						if (type.TypeKind != TypeKind.Interface)
							return;

						if (!containingType.AllInterfaces.Contains (type))
							return;
					}

					if (invocation.Arguments.Length < 1)
						return;

					var phrase = invocation.Arguments [0];
					if (phrase.Parameter.Type.SpecialType != SpecialType.System_String)
						return;

					if (phrase.Value.Kind == OperationKind.Literal)
						return;

					if (IsLiteralOperation (phrase.Value))
						return;

					operationContext.ReportDiagnostic (Diagnostic.Create (descriptor, phrase.Syntax.GetLocation ()));
				}, OperationKind.Invocation);
			});
		}

		static bool IsLiteralOperation (IOperation value)
		{
			if (value.Kind == OperationKind.Literal)
				return true;

			return value is IBinaryOperation binOp && IsLiteralOperation (binOp.LeftOperand) && IsLiteralOperation (binOp.RightOperand);
		}
	}
}
