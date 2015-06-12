// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Completion.KeywordRecommenders
{
    internal class AsyncKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public AsyncKeywordRecommender() :
            base(SyntaxKind.AsyncKeyword, isValidInPreprocessorContext: false)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
			if (context.IsAnyExpressionContext)
            {
                return true;
            }

            return !context.TargetToken.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword)
                && InMemberDeclarationContext(position, context, cancellationToken);
        }

        private static bool InMemberDeclarationContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
			return context.SyntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken)
                || context.IsMemberDeclarationContext(
                    validModifiers: SyntaxKindSet.AllMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassStructTypeDeclarations,
                    canBePartial: true,
                    cancellationToken: cancellationToken);
        }
    }
}
