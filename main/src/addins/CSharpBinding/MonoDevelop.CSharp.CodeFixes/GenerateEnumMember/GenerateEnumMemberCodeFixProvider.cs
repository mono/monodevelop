// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using MonoDevelop.CSharp.CodeFixes.GenerateConstructor;
using ICSharpCode.NRefactory6.CSharp.GenerateMember.GenerateEnumMember;

namespace MonoDevelop.CSharp.CodeFixes.GenerateEnumMember
{
	[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateEnumMember), Shared]
	[ExtensionOrder(After = PredefinedCodeFixProviderNames.GenerateConstructor)]
	internal class GenerateEnumMemberCodeFixProvider : AbstractGenerateMemberCodeFixProvider
	{
		private const string CS0117 = "CS0117"; // error CS0117: 'Color' does not contain a definition for 'Red'

		public override ImmutableArray<string> FixableDiagnosticIds
		{
			get { return ImmutableArray.Create(CS0117); }
		}
		static CSharpGenerateEnumMemberService service = new CSharpGenerateEnumMemberService();

		protected override Task<IEnumerable<CodeAction>> GetCodeActionsAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
		{
			return service.GenerateEnumMemberAsync(document, node, cancellationToken);
		}

		protected override bool IsCandidate(SyntaxNode node)
		{
			return node is IdentifierNameSyntax;
		}
	}
}
