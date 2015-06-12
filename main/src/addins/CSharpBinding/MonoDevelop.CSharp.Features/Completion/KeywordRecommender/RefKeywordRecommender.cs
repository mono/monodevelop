// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Completion.KeywordRecommenders
{
    internal class RefKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public RefKeywordRecommender()
            : base(SyntaxKind.RefKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                syntaxTree.IsParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                syntaxTree.IsAnonymousMethodParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                syntaxTree.IsPossibleLambdaParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                context.TargetToken.IsConstructorOrMethodParameterArgumentContext() ||
                context.TargetToken.IsXmlCrefParameterModifierContext();
        }
    }
}
