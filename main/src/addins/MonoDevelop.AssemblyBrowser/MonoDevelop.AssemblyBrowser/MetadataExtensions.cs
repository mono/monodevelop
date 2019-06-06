//
// MetadataExtensions.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using ICSharpCode.Decompiler.TypeSystem;

namespace MonoDevelop.AssemblyBrowser
{
	static class MetadataExtensions
	{
		public static bool IsPrivate (this IEntity entity) =>
			entity.Accessibility == Accessibility.Private ||
			entity.Accessibility == Accessibility.Internal ||
			entity.Accessibility == Accessibility.ProtectedAndInternal;

		public static bool IsPublic (this IEntity entity) =>
			entity.Accessibility == Accessibility.Protected ||
			entity.Accessibility == Accessibility.ProtectedOrInternal ||
			entity.Accessibility == Accessibility.Public;

		public static bool IsPublic (this Microsoft.CodeAnalysis.ISymbol entity) =>
			entity.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Protected ||
			entity.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal ||
			entity.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public;

		public static string GetStockIcon (this Accessibility attributes)
		{
			switch (attributes) {
			case Accessibility.Private:
				return "private-";
			case Accessibility.Public:
				return "";
			case Accessibility.Protected:
				return "protected-";
			case Accessibility.Internal:
				return "internal-";
			case Accessibility.ProtectedOrInternal:
			case Accessibility.ProtectedAndInternal: // FIXME we have no icon here
				return "ProtectedOrInternal-";
			default:
				return "";
			}
		}
	}
}
