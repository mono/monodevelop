// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
	internal class CheckedStatementHighlighter : AbstractKeywordHighlighter<CheckedStatementSyntax>
    {
        protected override IEnumerable<TextSpan> GetHighlights(
            CheckedStatementSyntax checkedStatement, CancellationToken cancellationToken)
        {
            yield return checkedStatement.Keyword.Span;
        }
    }
}
