//
// Glyph.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
namespace ICSharpCode.NRefactory6.CSharp
{
	internal enum Glyph
	{
		Assembly,

		BasicFile,
		BasicProject,

		ClassPublic,
		ClassProtected,
		ClassPrivate,
		ClassInternal,

		CSharpFile,
		CSharpProject,

		ConstantPublic,
		ConstantProtected,
		ConstantPrivate,
		ConstantInternal,

		DelegatePublic,
		DelegateProtected,
		DelegatePrivate,
		DelegateInternal,

		EnumPublic,
		EnumProtected,
		EnumPrivate,
		EnumInternal,

		EnumMember,

		Error,

		EventPublic,
		EventProtected,
		EventPrivate,
		EventInternal,

		ExtensionMethodPublic,
		ExtensionMethodProtected,
		ExtensionMethodPrivate,
		ExtensionMethodInternal,

		FieldPublic,
		FieldProtected,
		FieldPrivate,
		FieldInternal,

		InterfacePublic,
		InterfaceProtected,
		InterfacePrivate,
		InterfaceInternal,

		Intrinsic,

		Keyword,

		Label,

		Local,

		Namespace,

		MethodPublic,
		MethodProtected,
		MethodPrivate,
		MethodInternal,

		ModulePublic,
		ModuleProtected,
		ModulePrivate,
		ModuleInternal,

		OpenFolder,

		Operator,

		Parameter,

		PropertyPublic,
		PropertyProtected,
		PropertyPrivate,
		PropertyInternal,

		RangeVariable,

		Reference,

		StructurePublic,
		StructureProtected,
		StructurePrivate,
		StructureInternal,

		TypeParameter,

		Snippet,

		CompletionWarning
	}
}

