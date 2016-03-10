// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    internal class AsyncMethodHighlighter : AbstractAsyncHighlighter<MethodDeclarationSyntax>
    {
        protected override IEnumerable<TextSpan> GetHighlights(MethodDeclarationSyntax node, CancellationToken cancellationToken)
        {
            if (!node.Modifiers.Any(SyntaxKind.AsyncKeyword))
            {
                return SpecializedCollections.EmptyEnumerable<TextSpan>();
            }

            var spans = new List<TextSpan>();

            HighlightRelatedKeywords(node, spans);

            return spans;
        }
    }
}
