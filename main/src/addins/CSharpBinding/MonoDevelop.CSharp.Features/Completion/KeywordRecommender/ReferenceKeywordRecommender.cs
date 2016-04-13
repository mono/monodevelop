// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.Completion.KeywordRecommenders
{
    internal class ReferenceKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public ReferenceKeywordRecommender()
            : base(SyntaxKind.ReferenceKeyword, isValidInPreprocessorContext: true)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                syntaxTree.IsInteractiveOrScript() &&
                syntaxTree.IsBeforeFirstToken(position, cancellationToken) &&
                context.IsPreProcessorKeywordContext;
        }
    }
}
