// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.ExtractMethod
{
    public class UniqueNameGenerator
    {
        private readonly SemanticModel _semanticModel;

        public UniqueNameGenerator(SemanticModel semanticModel)
        {
            //Contract.ThrowIfNull(semanticModel);
            _semanticModel = semanticModel;
        }

        public string CreateUniqueMethodName(SyntaxNode contextNode, string baseName)
        {
            //Contract.ThrowIfNull(contextNode);
            //Contract.ThrowIfNull(baseName);

            return NameGenerator.GenerateUniqueName(baseName, string.Empty,
               n => _semanticModel.LookupSymbols(contextNode.SpanStart, /*container*/null, n).Length == 0);
        }
    }
}
