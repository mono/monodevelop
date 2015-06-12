// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp.ExtractMethod
{
	public static class ExtractMethodService
    {
		readonly static CSharpExtractMethodService service = new CSharpExtractMethodService ();

        public static Task<ExtractMethodResult> ExtractMethodAsync(Document document, TextSpan textSpan, OptionSet options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return service.ExtractMethodAsync(document, textSpan, options, cancellationToken);
        }
    }
}
