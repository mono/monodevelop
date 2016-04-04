//
// TypeExtensions.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Runtime.ExceptionServices;
using MonoDevelop.Ide.TypeSystem;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class TypeExtensions
	{
		readonly static MethodInfo generateTypeSyntaxMethod;
		readonly static MethodInfo findImplementingTypesAsync;

		static TypeExtensions()
		{
			var typeInfo = Type.GetType("Microsoft.CodeAnalysis.CSharp.Extensions.ITypeSymbolExtensions" + ReflectionNamespaces.CSWorkspacesAsmName, true);
			generateTypeSyntaxMethod = typeInfo.GetMethod("GenerateTypeSyntax", new[] { typeof(ITypeSymbol) });

			typeInfo = Type.GetType("Microsoft.CodeAnalysis.FindSymbols.DependentTypeFinder" + ReflectionNamespaces.WorkspacesAsmName, true);
			findImplementingTypesAsync = typeInfo.GetMethod("FindImplementingTypesAsync", new[] { typeof(INamedTypeSymbol), typeof(Solution), typeof(IImmutableSet<Project>), typeof(CancellationToken) });
			if (findImplementingTypesAsync == null)
				throw new Exception ("Can't find FindImplementingTypesAsync");
		}

		public static TypeSyntax GenerateTypeSyntax(this ITypeSymbol typeSymbol, SyntaxAnnotation simplifierAnnotation = null)
		{
			var typeSyntax = (TypeSyntax)generateTypeSyntaxMethod.Invoke(null, new object[] { typeSymbol });
			if (simplifierAnnotation != null)
				return typeSyntax.WithAdditionalAnnotations(simplifierAnnotation);
			return typeSyntax;
		}
		
		#region GetDelegateInvokeMethod
		/// <summary>
		/// Gets the invoke method for a delegate type.
		/// </summary>
		/// <remarks>
		/// Returns null if the type is not a delegate type; or if the invoke method could not be found.
		/// </remarks>
		public static IMethodSymbol GetDelegateInvokeMethod(this ITypeSymbol type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (type.TypeKind == TypeKind.Delegate)
				return type.GetMembers ("Invoke").OfType<IMethodSymbol>().FirstOrDefault(m => m.MethodKind == MethodKind.DelegateInvoke);
			return null;
		}
		#endregion

		public static Task<IEnumerable<INamedTypeSymbol>> FindImplementingTypesAsync (this INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return (Task<IEnumerable<INamedTypeSymbol>>)findImplementingTypesAsync.Invoke(null, new object[] { type, solution, projects, cancellationToken });
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		public static bool IsNullableType(this ITypeSymbol type)
		{
			var original = type.OriginalDefinition;
			return original.SpecialType == SpecialType.System_Nullable_T;
		}

		public static ITypeSymbol GetNullableUnderlyingType(this ITypeSymbol type)
		{
			if (!IsNullableType(type))
				return null;
			return ((INamedTypeSymbol)type).TypeArguments[0];
		}

		/// <summary>
		/// Gets all base classes and interfaces.
		/// </summary>
		/// <returns>All classes and interfaces.</returns>
		/// <param name="type">Type.</param>
		public static IEnumerable<INamedTypeSymbol> GetAllBaseClassesAndInterfaces (this INamedTypeSymbol type, bool includeSuperType = false)
		{
			if (!includeSuperType)
				type = type.BaseType;
			var curType = type;
			while (curType != null) {
				yield return curType;
				curType = curType.BaseType;
			}

			foreach (var inter in type.AllInterfaces) {
				yield return inter;
			}
		}

		/// <summary>
		/// Determines if derived from baseType. Includes itself, all base classes and all interfaces.
		/// </summary>
		/// <returns><c>true</c> if is derived from the specified type baseType; otherwise, <c>false</c>.</returns>
		/// <param name="type">Type.</param>
		/// <param name="baseType">Base type.</param>
		public static bool IsDerivedFromClassOrInterface(this INamedTypeSymbol type, INamedTypeSymbol baseType)
		{
			//NR5 is returning true also for same type
			for (; type != null; type = type.BaseType) {
				if (type == baseType) {
					return true;
				}
			}
			//And interfaces
			foreach (var inter in type.AllInterfaces) {
				if (inter == baseType) {
					return true;
				}
			}
			return false;
		}

	}
}

