//
// SyntaxKindSet.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class SyntaxKindSet
	{
		public static readonly ISet<SyntaxKind> AllTypeModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
		{
			SyntaxKind.AbstractKeyword,
			SyntaxKind.InternalKeyword,
			SyntaxKind.NewKeyword,
			SyntaxKind.PublicKeyword,
			SyntaxKind.PrivateKeyword,
			SyntaxKind.ProtectedKeyword,
			SyntaxKind.SealedKeyword,
			SyntaxKind.StaticKeyword,
			SyntaxKind.UnsafeKeyword
		};

		public static readonly ISet<SyntaxKind> AllMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
		{
			SyntaxKind.AbstractKeyword,
			SyntaxKind.AsyncKeyword,
			SyntaxKind.ExternKeyword,
			SyntaxKind.InternalKeyword,
			SyntaxKind.NewKeyword,
			SyntaxKind.OverrideKeyword,
			SyntaxKind.PublicKeyword,
			SyntaxKind.PrivateKeyword,
			SyntaxKind.ProtectedKeyword,
			SyntaxKind.ReadOnlyKeyword,
			SyntaxKind.SealedKeyword,
			SyntaxKind.StaticKeyword,
			SyntaxKind.UnsafeKeyword,
			SyntaxKind.VirtualKeyword,
			SyntaxKind.VolatileKeyword,
		};

		public static readonly ISet<SyntaxKind> AllGlobalMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
		{
			SyntaxKind.ExternKeyword,
			SyntaxKind.InternalKeyword,
			SyntaxKind.NewKeyword,
			SyntaxKind.OverrideKeyword,
			SyntaxKind.PublicKeyword,
			SyntaxKind.PrivateKeyword,
			SyntaxKind.ReadOnlyKeyword,
			SyntaxKind.StaticKeyword,
			SyntaxKind.UnsafeKeyword,
			SyntaxKind.VolatileKeyword,
		};

		public static readonly ISet<SyntaxKind> AllTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
		{
			SyntaxKind.InterfaceDeclaration,
			SyntaxKind.ClassDeclaration,
			SyntaxKind.StructDeclaration,
			SyntaxKind.EnumDeclaration
		};

		public static readonly ISet<SyntaxKind> ClassInterfaceStructTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
		{
			SyntaxKind.InterfaceDeclaration,
			SyntaxKind.ClassDeclaration,
			SyntaxKind.StructDeclaration,
		};

		public static readonly ISet<SyntaxKind> ClassStructTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
		{
			SyntaxKind.ClassDeclaration,
			SyntaxKind.StructDeclaration,
		};

		public static readonly ISet<SyntaxKind> ClassOnlyTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
		{
			SyntaxKind.ClassDeclaration,
		};

		public static readonly ISet<SyntaxKind> StructOnlyTypeDeclarations = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
		{
			SyntaxKind.StructDeclaration,
		};
	}
}

