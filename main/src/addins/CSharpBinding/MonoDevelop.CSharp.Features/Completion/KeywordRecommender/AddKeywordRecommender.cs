// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Completion.KeywordRecommenders
{
    internal class AddKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public AddKeywordRecommender()
            : base(SyntaxKind.AddKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
			return context.TargetToken.IsAccessorDeclarationContext<EventDeclarationSyntax>(position, SyntaxKind.AddKeyword);
        }
    }
}
