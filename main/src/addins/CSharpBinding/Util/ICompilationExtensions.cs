//
// ICompilationExtensions.cs
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
using Microsoft.CodeAnalysis;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class ICompilationExtensions
	{
		public static INamedTypeSymbol AttributeType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Attribute");
		}

		public static INamedTypeSymbol ExceptionType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Exception");
		}

		public static INamedTypeSymbol DesignerCategoryAttributeType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.ComponentModel.DesignerCategoryAttribute");
		}

		public static INamedTypeSymbol DesignerGeneratedAttributeType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute");
		}

		public static INamedTypeSymbol HideModuleNameAttribute(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("Microsoft.VisualBasic.HideModuleNameAttribute");
		}

		public static INamedTypeSymbol EventArgsType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.EventArgs");
		}

		public static INamedTypeSymbol NotImplementedExceptionType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.NotImplementedException");
		}

		public static INamedTypeSymbol EqualityComparerOfTType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Collections.Generic.EqualityComparer`1");
		}

		public static INamedTypeSymbol ActionType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Action");
		}

		public static INamedTypeSymbol ExpressionOfTType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
		}

		public static INamedTypeSymbol EditorBrowsableAttributeType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.ComponentModel.EditorBrowsableAttribute");
		}

		public static INamedTypeSymbol EditorBrowsableStateType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.ComponentModel.EditorBrowsableState");
		}

		public static INamedTypeSymbol TaskType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
		}

		public static INamedTypeSymbol TaskOfTType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
		}

		public static INamedTypeSymbol SerializableAttributeType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.SerializableAttribute");
		}

		public static INamedTypeSymbol CoClassType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.CoClassAttribute");
		}

		public static INamedTypeSymbol ComAliasNameAttributeType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Runtime.InteropServices.ComAliasNameAttribute");
		}

		public static INamedTypeSymbol SuppressMessageAttributeType(this Compilation compilation)
		{
			return compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.SuppressMessageAttribute");
		}
	}
}

