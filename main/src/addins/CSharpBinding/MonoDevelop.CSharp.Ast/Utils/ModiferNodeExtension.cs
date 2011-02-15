// 
// ModiferNodeExtension.cs
//  
// Author:
//       mkrueger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Ast.Utils
{
	public static class ModiferNodeExtension
	{
		public static bool IsConst (this Modifiers modifier)
		{
			return (modifier & Modifiers.Const) == Modifiers.Const;
		}

		public static bool IsPrivate (this Modifiers modifier)
		{
			return (modifier & Modifiers.Private) == Modifiers.Private;
		}

		public static bool IsInternal (this Modifiers modifier)
		{
			return (modifier & Modifiers.Internal) == Modifiers.Internal;
		}

		public static bool IsProtected (this Modifiers modifier)
		{
			return (modifier & Modifiers.Protected) == Modifiers.Protected;
		}

		public static bool IsPublic (this Modifiers modifier)
		{
			return (modifier & Modifiers.Public) == Modifiers.Public;
		}

		public static bool IsAbstract (this Modifiers modifier)
		{
			return (modifier & Modifiers.Abstract) == Modifiers.Abstract;
		}

		public static bool IsVirtual (this Modifiers modifier)
		{
			return (modifier & Modifiers.Virtual) == Modifiers.Virtual;
		}

		public static bool IsSealed (this Modifiers modifier)
		{
			return (modifier & Modifiers.Sealed) == Modifiers.Sealed;
		}

		public static bool IsStatic (this Modifiers modifier)
		{
			return (modifier & Modifiers.Static) == Modifiers.Static;
		}

		public static bool IsOverride (this Modifiers modifier)
		{
			return (modifier & Modifiers.Override) == Modifiers.Override;
		}

		public static bool IsReadonly (this Modifiers modifier)
		{
			return (modifier & Modifiers.Readonly) == Modifiers.Readonly;
		}

		public static bool IsNew (this Modifiers modifier)
		{
			return (modifier & Modifiers.New) == Modifiers.New;
		}

		public static bool IsPartial (this Modifiers modifier)
		{
			return (modifier & Modifiers.Partial) == Modifiers.Partial;
		}

		public static bool IsExtern (this Modifiers modifier)
		{
			return (modifier & Modifiers.Extern) == Modifiers.Extern;
		}

		public static bool IsVolatile (this Modifiers modifier)
		{
			return (modifier & Modifiers.Volatile) == Modifiers.Volatile;
		}

		public static bool IsUnsafe (this Modifiers modifier)
		{
			return (modifier & Modifiers.Unsafe) == Modifiers.Unsafe;
		}
	}
}
