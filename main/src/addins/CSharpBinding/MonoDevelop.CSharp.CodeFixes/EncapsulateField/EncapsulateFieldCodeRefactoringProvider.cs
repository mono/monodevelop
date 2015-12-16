// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace ICSharpCode.NRefactory6.CSharp.CodeRefactorings.EncapsulateField
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "Encapsulate Field"), Shared]
	internal class EncapsulateFieldRefactoringProvider : AbstractEncapsulateFieldRefactoringProvider
	{
	}
}
