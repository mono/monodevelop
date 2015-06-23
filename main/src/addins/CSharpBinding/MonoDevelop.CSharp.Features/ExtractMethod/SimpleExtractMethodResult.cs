// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.ExtractMethod
{
    public class SimpleExtractMethodResult : ExtractMethodResult
    {
        public SimpleExtractMethodResult(
            OperationStatus status,
            Document document,
            SyntaxToken invocationNameToken,
            SyntaxNode methodDefinition)
            : base(status.Flag, status.Reasons, document, invocationNameToken, methodDefinition)
        {
        }
    }
}
