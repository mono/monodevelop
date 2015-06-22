//
// NR5CompatibiltyExtensions.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.TypeSystem
{
	public static class NR5CompatibiltyExtensions
	{
		readonly static Type ISymbolExtensionsTypeInfo;
		static MethodInfo isAccessibleWithin1Method, isAccessibleWithin2Method;

		static NR5CompatibiltyExtensions ()
		{
			ISymbolExtensionsTypeInfo = Type.GetType ("Microsoft.CodeAnalysis.Shared.Extensions.ISymbolExtensions, Microsoft.CodeAnalysis.Workspaces", true);

			isAccessibleWithin1Method = ISymbolExtensionsTypeInfo.GetMethod ("IsAccessibleWithin", BindingFlags.Static | BindingFlags.Public, null, new [] { typeof(ISymbol), typeof(IAssemblySymbol), typeof(ITypeSymbol) }, null);
			if (isAccessibleWithin1Method == null)
				LoggingService.LogFatalError ("NR5CompatibiltyExtensions: IsAccessibleWithin(1) method not found");
			isAccessibleWithin2Method = ISymbolExtensionsTypeInfo.GetMethod ("IsAccessibleWithin", BindingFlags.Static | BindingFlags.Public, null, new [] { typeof(ISymbol), typeof(INamedTypeSymbol), typeof(ITypeSymbol) }, null);
			if (isAccessibleWithin2Method == null)
				LoggingService.LogFatalError ("NR5CompatibiltyExtensions: IsAccessibleWithin(2) method not found");
		}

		/// <summary>
		/// Gets the full name of the metadata.
		/// In case symbol is not INamedTypeSymbol it returns raw MetadataName
		/// Example: Generic type returns T1, T2...
		/// </summary>
		/// <returns>The full metadata name.</returns>
		/// <param name="symbol">Symbol.</param>
		public static string GetFullMetadataName (this ITypeSymbol symbol)
		{
			//This is for comaptibility with NR5 reflection name in case of generic types like T1, T2...
			var namedTypeSymbol = symbol as INamedTypeSymbol;
			return namedTypeSymbol != null ? GetFullMetadataName (namedTypeSymbol) : symbol.MetadataName;
		}

		/// <summary>
		/// Gets the full MetadataName(ReflectionName in NR5).
		/// Example: Namespace1.Namespace2.Classs1+NestedClassWithTwoGenericTypes`2+NestedClassWithoutGenerics
		/// </summary>
		/// <returns>The full metadata name.</returns>
		/// <param name="symbol">Symbol.</param>
		public static string GetFullMetadataName (this INamedTypeSymbol symbol)
		{
			var fullName = new StringBuilder (symbol.MetadataName);
			var parentType = symbol.ContainingType;
			while (parentType != null) {
				fullName.Insert (0, '+');
				fullName.Insert (0, parentType.MetadataName);
				parentType = parentType.ContainingType;
			}
			var ns = symbol.ContainingNamespace;
			while (ns != null && !ns.IsGlobalNamespace) {
				fullName.Insert (0, '.');
				fullName.Insert (0, ns.MetadataName);
				ns = ns.ContainingNamespace;
			}
			return fullName.ToString ();
		}

		public static IEnumerable<INamedTypeSymbol> GetAllTypes (this INamespaceSymbol namespaceSymbol, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (namespaceSymbol == null)
				throw new ArgumentNullException (nameof (namespaceSymbol));
			var stack = new Stack<INamespaceOrTypeSymbol> ();
			stack.Push (namespaceSymbol);

			while (stack.Count > 0) {
				if (cancellationToken.IsCancellationRequested)
					yield break;
				var current = stack.Pop ();
				var currentNs = current as INamespaceSymbol;
				if (currentNs != null) {
					foreach (var member in currentNs.GetMembers ())
						stack.Push (member);
				} else {
					var namedType = (INamedTypeSymbol)current;
					foreach (var nestedType in namedType.GetTypeMembers ())
						stack.Push (nestedType);
					yield return namedType;
				}
			}
		}

		/// <summary>
		/// Determines if derived from baseType. Includes itself and all base classes, but does not include interfaces.
		/// </summary>
		/// <returns><c>true</c> if is derived from class the specified type baseType; otherwise, <c>false</c>.</returns>
		/// <param name="type">Type.</param>
		/// <param name="baseType">Base type.</param>
		public static bool IsDerivedFromClass (this INamedTypeSymbol type, INamedTypeSymbol baseType)
		{
			//NR5 is returning true also for same type
			for (; type != null; type = type.BaseType) {
				if (type == baseType) {
					return true;
				}
			}
			return false;
		}

		public static IEnumerable<INamedTypeSymbol> GetBaseTypes (this ITypeSymbol type)
		{
			var current = type.BaseType;
			while (current != null) {
				yield return current;
				current = current.BaseType;
			}
		}

		public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis (this ITypeSymbol type)
		{
			var current = type;
			while (current != null) {
				yield return current;
				current = current.BaseType;
			}
		}

		public static ITypeSymbol GetReturnType (this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			switch (symbol.Kind) {
			case SymbolKind.Field:
				var field = (IFieldSymbol)symbol;
				return field.Type;
			case SymbolKind.Method:
				var method = (IMethodSymbol)symbol;
				if (method.MethodKind == MethodKind.Constructor)
					return method.ContainingType;
				return method.ReturnType;
			case SymbolKind.Property:
				var property = (IPropertySymbol)symbol;
				return property.Type;
			case SymbolKind.Event:
				var evt = (IEventSymbol)symbol;
				return evt.Type;
			case SymbolKind.Parameter:
				var param = (IParameterSymbol)symbol;
				return param.Type;
			case SymbolKind.Local:
				var local = (ILocalSymbol)symbol;
				return local.Type;
			}
			return null;
		}


		/// <summary>
		/// Gets the full name of the namespace.
		/// </summary>
		public static string GetFullName (this INamespaceSymbol ns)
		{
			return ns.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat);
		}

		/// <summary>
		/// Gets the full name. The full name is no 1:1 representation of a type it's missing generics and it has a poor
		/// representation for inner types (just dot separated).
		/// DO NOT use this method unless you're know what you do. It's only implemented for legacy code.
		/// </summary>
		public static string GetFullName (this ITypeSymbol type)
		{
			return type.ToDisplayString (SymbolDisplayFormat.CSharpErrorMessageFormat);
		}

		public static IEnumerable<INamedTypeSymbol> GetAllTypesInMainAssembly (this Compilation compilation, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (compilation == null)
				throw new ArgumentNullException ("compilation");
			return compilation.Assembly.GlobalNamespace.GetAllTypes (cancellationToken);
		}

		public static IEnumerable<T> GetAccessibleMembersInThisAndBaseTypes<T>(this ITypeSymbol containingType, ISymbol within) where T : class, ISymbol
		{
			if (containingType == null)
				return Enumerable.Empty<T> ();

			var types = containingType.GetBaseTypesAndThis ();
			return types.SelectMany (x => x.GetMembers ().OfType<T> ().Where (m => m.IsAccessibleWithin (within)));
		}

		/// <summary>
		/// Gets all base classes.
		/// </summary>
		/// <returns>The all base classes.</returns>
		/// <param name="type">Type.</param>
		public static IEnumerable<INamedTypeSymbol> GetAllBaseClasses (this INamedTypeSymbol type, bool includeSuperType = false)
		{
			if (!includeSuperType)
				type = type.BaseType;
			while (type != null) {
				yield return type;
				type = type.BaseType;
			}
		}

		public static bool IsDefinedInMetadata (this ISymbol symbol)
		{
			return symbol.Locations.Any (loc => loc.IsInMetadata);
		}

		public static bool IsDefinedInSource (this ISymbol symbol)
		{
			return symbol.Locations.All (loc => loc.IsInSource);
		}

		//public static DeclarationModifiers GetSymbolModifiers(this ISymbol symbol)
		//{
		//	// ported from roslyn source - why they didn't use DeclarationModifiers.From (symbol) ?
		//	return DeclarationModifiers.None
		//		                           .WithIsStatic (symbol.IsStatic)
		//		                           .WithIsAbstract (symbol.IsAbstract)
		//		                           .WithIsUnsafe (symbol.IsUnsafe ())
		//		                           .WithIsVirtual (symbol.IsVirtual)
		//		                           .WithIsOverride (symbol.IsOverride)
		//		                           .WithIsSealed (symbol.IsSealed);
		//}

		public static IEnumerable<SyntaxReference> GetDeclarations (this ISymbol symbol)
		{
			return symbol != null
				? symbol.DeclaringSyntaxReferences.AsEnumerable ()
					: Enumerable.Empty<SyntaxReference> ();
		}

		public static bool IsSameAssemblyOrHasFriendAccessTo (this IAssemblySymbol assembly, IAssemblySymbol toAssembly)
		{
			return
				Equals (assembly, toAssembly) ||
				(assembly.IsInteractive && toAssembly.IsInteractive) ||
				toAssembly.GivesAccessTo (assembly);
		}

		/// <summary>
		/// Returns the component category.
		/// [System.ComponentModel.CategoryAttribute (CATEGORY)]
		/// </summary>
		/// <param name="symbol">Symbol.</param>
		public static string GetComponentCategory (this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			var browsableState = symbol.GetAttributes ().FirstOrDefault (attr => attr.AttributeClass.Name == "CategoryAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
			if (browsableState != null && browsableState.ConstructorArguments.Length == 1) {
				try {
					return (string)browsableState.ConstructorArguments [0].Value;
				} catch {
				}
			}
			return null;
		}


		/// <summary>
		/// Returns true if the type is public and was tagged with
		/// [System.ComponentModel.ToolboxItem (true)]
		/// </summary>
		/// <returns><c>true</c> if is designer browsable the specified symbol; otherwise, <c>false</c>.</returns>
		/// <param name="symbol">Symbol.</param>
		public static bool IsToolboxItem (this ITypeSymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			if (symbol.DeclaredAccessibility != Accessibility.Public)
				return false;
			var toolboxItemAttr = symbol.GetAttributes ().FirstOrDefault (attr => attr.AttributeClass.Name == "ToolboxItemAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
			if (toolboxItemAttr != null && toolboxItemAttr.ConstructorArguments.Length == 1) {
				try {
					return (bool)toolboxItemAttr.ConstructorArguments [0].Value;
				} catch {
				}
			}
			return false;
		}

		/// <summary>
		/// Returns true if the symbol wasn't tagged with
		/// [System.ComponentModel.BrowsableAttribute (false)]
		/// </summary>
		/// <returns><c>true</c> if is designer browsable the specified symbol; otherwise, <c>false</c>.</returns>
		/// <param name="symbol">Symbol.</param>
		public static bool IsDesignerBrowsable (this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			var browsableState = symbol.GetAttributes ().FirstOrDefault (attr => attr.AttributeClass.Name == "BrowsableAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
			if (browsableState != null && browsableState.ConstructorArguments.Length == 1) {
				try {
					return (bool)browsableState.ConstructorArguments [0].Value;
				} catch {
				}
			}
			return true;
		}

		public static INamedTypeSymbol GetEnclosingNamedType(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			return semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(position, cancellationToken);
		}

		public static TSymbol GetEnclosingSymbol<TSymbol>(this SemanticModel semanticModel, int position, CancellationToken cancellationToken)
			where TSymbol : ISymbol
		{
			for (var symbol = semanticModel.GetEnclosingSymbol(position, cancellationToken);
			     symbol != null;
			     symbol = symbol.ContainingSymbol)
			{
				if (symbol is TSymbol)
				{
					return (TSymbol)symbol;
				}
			}

			return default(TSymbol);
		}

		/// <summary>
		/// Checks if 'symbol' is accessible from within 'within'.
		/// </summary>
		public static bool IsAccessibleWithin(
			this ISymbol symbol,
			ISymbol within,
			ITypeSymbol throughTypeOpt = null)
		{
			if (within is IAssemblySymbol)
			{
				return symbol.IsAccessibleWithin((IAssemblySymbol)within, throughTypeOpt);
			}
			else if (within is INamedTypeSymbol)
			{
				return symbol.IsAccessibleWithin((INamedTypeSymbol)within, throughTypeOpt);
			}
			else
			{
				throw new ArgumentException();
			}
		}

		/// <summary>
		/// Checks if 'symbol' is accessible from within assembly 'within'.
		/// </summary>
		public static bool IsAccessibleWithin(
			this ISymbol symbol,
			IAssemblySymbol within,
			ITypeSymbol throughTypeOpt = null)
		{
			return (bool)isAccessibleWithin1Method.Invoke (null, new object [] { symbol, within, throughTypeOpt });
		}

		/// <summary>
		/// Checks if 'symbol' is accessible from within name type 'within', with an optional
		/// qualifier of type "throughTypeOpt".
		/// </summary>
		public static bool IsAccessibleWithin(
			this ISymbol symbol,
			INamedTypeSymbol within,
			ITypeSymbol throughTypeOpt = null)
		{
			return (bool)isAccessibleWithin2Method.Invoke (null, new object [] { symbol, within, throughTypeOpt });
		}
	}
}

