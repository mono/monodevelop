// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.CSharp;

namespace ICSharpCode.NRefactory6.CSharp.ExtractMethod
{
	public class CSharpSyntaxTriviaService : AbstractSyntaxTriviaService
    {
        public CSharpSyntaxTriviaService()
			: base((int)SyntaxKind.EndOfLineTrivia)
        {
        }
    }
}
