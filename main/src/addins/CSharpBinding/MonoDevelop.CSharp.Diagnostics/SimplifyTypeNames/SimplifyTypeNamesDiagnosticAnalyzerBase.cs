// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using System;
using RefactoringEssentials;

namespace MonoDevelop.CSharp.Diagnostics.SimplifyTypeNames
{
	internal abstract class SimplifyTypeNamesDiagnosticAnalyzerBase<TLanguageKindEnum> : DiagnosticAnalyzer where TLanguageKindEnum : struct
	{
		private static string s_localizableMessage = GettextCatalog.GetString ("Name can be simplified.");
		private static string s_localizableTitleSimplifyNames = GettextCatalog.GetString ("Simplify Names");

		private static readonly DiagnosticDescriptor s_descriptorSimplifyNames = new DiagnosticDescriptor(IDEDiagnosticIds.SimplifyNamesDiagnosticId,
			s_localizableTitleSimplifyNames,
			s_localizableMessage,
			DiagnosticAnalyzerCategories.RedundanciesInCode,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			customTags: DiagnosticCustomTags.Unnecessary);

		private static string s_localizableTitleSimplifyMemberAccess = GettextCatalog.GetString ("Simplify member access '{0}'");
		private static readonly DiagnosticDescriptor s_descriptorSimplifyMemberAccess = new DiagnosticDescriptor(IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId,
			s_localizableTitleSimplifyMemberAccess,
			s_localizableMessage,
			DiagnosticAnalyzerCategories.RedundanciesInCode,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			customTags: DiagnosticCustomTags.Unnecessary);

		private static string s_localizableTitleSimplifyThisOrMe = GettextCatalog.GetString ("Remove 'this'");
		private static readonly DiagnosticDescriptor s_descriptorSimplifyThisOrMe = new DiagnosticDescriptor(IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId,
			s_localizableTitleSimplifyThisOrMe,
			s_localizableMessage,
			DiagnosticAnalyzerCategories.RedundanciesInCode,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			customTags: DiagnosticCustomTags.Unnecessary);

		private OptionSet _lazyDefaultOptionSet;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		{
			get
			{
				return ImmutableArray.Create(s_descriptorSimplifyNames, s_descriptorSimplifyMemberAccess, s_descriptorSimplifyThisOrMe);
			}
		}

		protected abstract void AnalyzeNode(SyntaxNodeAnalysisContext context);

		protected abstract bool CanSimplifyTypeNameExpressionCore(SemanticModel model, SyntaxNode node, OptionSet optionSet, out TextSpan issueSpan, out string diagnosticId, CancellationToken cancellationToken);

		private OptionSet GetOptionSet(AnalyzerOptions analyzerOptions)
		{
			return TypeSystemService.Workspace.Options;
		}

		protected abstract string GetLanguageName();

		protected bool TrySimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, AnalyzerOptions analyzerOptions, out Diagnostic diagnostic, CancellationToken cancellationToken)
		{
			diagnostic = default(Diagnostic);

			var optionSet = GetOptionSet(analyzerOptions);
			string diagnosticId;

			TextSpan issueSpan;
			if (!CanSimplifyTypeNameExpressionCore(model, node, optionSet, out issueSpan, out diagnosticId, cancellationToken))
			{
				return false;
			}

			if (model.SyntaxTree.OverlapsHiddenPosition(issueSpan, cancellationToken))
			{
				return false;
			}

			DiagnosticDescriptor descriptor;
			switch (diagnosticId)
			{
			case IDEDiagnosticIds.SimplifyNamesDiagnosticId:
				descriptor = s_descriptorSimplifyNames;
				break;

			case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId:
				descriptor = s_descriptorSimplifyMemberAccess;
				break;

			case IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId:
				descriptor = s_descriptorSimplifyThisOrMe;
				break;

			default:
				throw new InvalidOperationException();
			}

			var tree = model.SyntaxTree;
			diagnostic = Diagnostic.Create(descriptor, tree.GetLocation(issueSpan));
			return true;
		}
	}
}
