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
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.TypeSystem
{
	public static class NR5CompatibiltyExtensions
	{
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

		/// <summary>
		/// Checks if 'symbol' is accessible from within 'within'.
		/// </summary>
		public static bool IsAccessibleWithin (
			this ISymbol symbol,
			ISymbol within,
			ITypeSymbol throughTypeOpt = null)
		{
			if (within is IAssemblySymbol) {
				return symbol.IsAccessibleWithin ((IAssemblySymbol)within, throughTypeOpt);
			} else if (within is INamedTypeSymbol) {
				return symbol.IsAccessibleWithin ((INamedTypeSymbol)within, throughTypeOpt);
			} else {
				throw new ArgumentException ();
			}
		}

		/// <summary>
		/// Checks if 'symbol' is accessible from within assembly 'within'.
		/// </summary>
		public static bool IsAccessibleWithin (
			this ISymbol symbol,
			IAssemblySymbol within,
			ITypeSymbol throughTypeOpt = null)
		{
			bool failedThroughTypeCheck;
			return IsSymbolAccessibleCore (symbol, within, throughTypeOpt, out failedThroughTypeCheck);
		}

		/// <summary>
		/// Checks if 'symbol' is accessible from within name type 'within', with an optional
		/// qualifier of type "throughTypeOpt".
		/// </summary>
		public static bool IsAccessibleWithin (
			this ISymbol symbol,
			INamedTypeSymbol within,
			ITypeSymbol throughTypeOpt = null)
		{
			bool failedThroughTypeCheck;
			return IsSymbolAccessible (symbol, within, throughTypeOpt, out failedThroughTypeCheck);
		}

		/// <summary>
		/// Checks if 'symbol' is accessible from within assembly 'within', with an qualifier of
		/// type "throughTypeOpt". Sets "failedThroughTypeCheck" to true if it failed the "through
		/// type" check.
		/// </summary>
		private static bool IsSymbolAccessible (
			ISymbol symbol,
			INamedTypeSymbol within,
			ITypeSymbol throughTypeOpt,
			out bool failedThroughTypeCheck)
		{
			return IsSymbolAccessibleCore (symbol, within, throughTypeOpt, out failedThroughTypeCheck);
		}

		/// <summary>
		/// Checks if 'symbol' is accessible from within 'within', which must be a INamedTypeSymbol
		/// or an IAssemblySymbol.  If 'symbol' is accessed off of an expression then
		/// 'throughTypeOpt' is the type of that expression. This is needed to properly do protected
		/// access checks. Sets "failedThroughTypeCheck" to true if this protected check failed.
		/// </summary>
		//// NOTE(cyrusn): I expect this function to be called a lot.  As such, I do not do any memory
		//// allocations in the function itself (including not making any iterators).  This does mean
		//// that certain helper functions that we'd like to call are inlined in this method to
		//// prevent the overhead of returning collections or enumerators.  
		private static bool IsSymbolAccessibleCore (
			ISymbol symbol,
			ISymbol within,  // must be assembly or named type symbol
			ITypeSymbol throughTypeOpt,
			out bool failedThroughTypeCheck)
		{
			//			Contract.ThrowIfNull(symbol);
			//			Contract.ThrowIfNull(within);
			//			Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);

			failedThroughTypeCheck = false;
			// var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;

			switch (symbol.Kind) {
			case SymbolKind.Alias:
				return IsSymbolAccessibleCore (((IAliasSymbol)symbol).Target, within, throughTypeOpt, out failedThroughTypeCheck);

			case SymbolKind.ArrayType:
				return IsSymbolAccessibleCore (((IArrayTypeSymbol)symbol).ElementType, within, null, out failedThroughTypeCheck);

			case SymbolKind.PointerType:
				return IsSymbolAccessibleCore (((IPointerTypeSymbol)symbol).PointedAtType, within, null, out failedThroughTypeCheck);

			case SymbolKind.NamedType:
				return IsNamedTypeAccessible ((INamedTypeSymbol)symbol, within);

			case SymbolKind.ErrorType:
				return true;

			case SymbolKind.TypeParameter:
			case SymbolKind.Parameter:
			case SymbolKind.Local:
			case SymbolKind.Label:
			case SymbolKind.Namespace:
			case SymbolKind.DynamicType:
			case SymbolKind.Assembly:
			case SymbolKind.NetModule:
			case SymbolKind.RangeVariable:
				// These types of symbols are always accessible (if visible).
				return true;

			case SymbolKind.Method:
			case SymbolKind.Property:
			case SymbolKind.Field:
			case SymbolKind.Event:
				if (symbol.IsStatic) {
					// static members aren't accessed "through" an "instance" of any type.  So we
					// null out the "through" instance here.  This ensures that we'll understand
					// accessing protected statics properly.
					throughTypeOpt = null;
				}

				// If this is a synthesized operator of dynamic, it's always accessible.
				if (symbol?.Kind == SymbolKind.Method &&
					((IMethodSymbol)symbol).MethodKind == MethodKind.BuiltinOperator &&
					symbol.ContainingSymbol?.Kind == SymbolKind.DynamicType) {
					return true;
				}

				// If it's a synthesized operator on a pointer, use the pointer's PointedAtType.
				if (symbol?.Kind == SymbolKind.Method &&
					((IMethodSymbol)symbol).MethodKind == MethodKind.BuiltinOperator &&
					symbol.ContainingSymbol?.Kind == SymbolKind.PointerType) {
					return IsSymbolAccessibleCore (((IPointerTypeSymbol)symbol.ContainingSymbol).PointedAtType, within, null, out failedThroughTypeCheck);
				}

				return IsMemberAccessible (symbol.ContainingType, symbol.DeclaredAccessibility, within, throughTypeOpt, out failedThroughTypeCheck);

			default:
				throw new Exception ("unreachable");
			}
		}

		// Is the named type "type" accessible from within "within", which must be a named type or
		// an assembly.
		private static bool IsNamedTypeAccessible (INamedTypeSymbol type, ISymbol within)
		{
			//			Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);
			//			Contract.ThrowIfNull(type);

			if (type?.TypeKind == TypeKind.Error) {
				// Always assume that error types are accessible.
				return true;
			}

			bool unused;
			if (!type.IsDefinition) {
				// All type argument must be accessible.
				foreach (var typeArg in type.TypeArguments) {
					// type parameters are always accessible, so don't check those (so common it's
					// worth optimizing this).
					if (typeArg.Kind != SymbolKind.TypeParameter &&
							typeArg.TypeKind != TypeKind.Error &&
							!IsSymbolAccessibleCore (typeArg, within, null, out unused)) {
						return false;
					}
				}
			}

			var containingType = type.ContainingType;
			return containingType == null
				? IsNonNestedTypeAccessible (type.ContainingAssembly, type.DeclaredAccessibility, within)
					: IsMemberAccessible (type.ContainingType, type.DeclaredAccessibility, within, null, out unused);
		}

		// Is a top-level type with accessibility "declaredAccessibility" inside assembly "assembly"
		// accessible from "within", which must be a named type of an assembly.
		private static bool IsNonNestedTypeAccessible (
			IAssemblySymbol assembly,
			Accessibility declaredAccessibility,
			ISymbol within)
		{
			//			Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);
			//			Contract.ThrowIfNull(assembly);
			var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;

			switch (declaredAccessibility) {
			case Accessibility.NotApplicable:
			case Accessibility.Public:
				// Public symbols are always accessible from any context
				return true;

			case Accessibility.Private:
			case Accessibility.Protected:
			case Accessibility.ProtectedAndInternal:
				// Shouldn't happen except in error cases.
				return false;

			case Accessibility.Internal:
			case Accessibility.ProtectedOrInternal:
				// An internal type is accessible if we're in the same assembly or we have
				// friend access to the assembly it was defined in.
				return withinAssembly.IsSameAssemblyOrHasFriendAccessTo (assembly);

			default:
				throw new Exception ("unreachable");
			}
		}

		// Is a member with declared accessibility "declaredAccessiblity" accessible from within
		// "within", which must be a named type or an assembly.
		private static bool IsMemberAccessible (
			INamedTypeSymbol containingType,
			Accessibility declaredAccessibility,
			ISymbol within,
			ITypeSymbol throughTypeOpt,
			out bool failedThroughTypeCheck)
		{
			//			Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);
			//			Contract.ThrowIfNull(containingType);

			failedThroughTypeCheck = false;

			var originalContainingType = containingType.OriginalDefinition;
			var withinNamedType = within as INamedTypeSymbol;
			var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;

			// A nested symbol is only accessible to us if its container is accessible as well.
			if (!IsNamedTypeAccessible (containingType, within)) {
				return false;
			}

			switch (declaredAccessibility) {
			case Accessibility.NotApplicable:
				// TODO(cyrusn): Is this the right thing to do here?  Should the caller ever be
				// asking about the accessibility of a symbol that has "NotApplicable" as its
				// value?  For now, I'm preserving the behavior of the existing code.  But perhaps
				// we should fail here and require the caller to not do this?
				return true;

			case Accessibility.Public:
				// Public symbols are always accessible from any context
				return true;

			case Accessibility.Private:
				// All expressions in the current submission (top-level or nested in a method or
				// type) can access previous submission's private top-level members. Previous
				// submissions are treated like outer classes for the current submission - the
				// inner class can access private members of the outer class.
				if (withinAssembly.IsInteractive && containingType.IsScriptClass) {
					return true;
				}

				// private members never accessible from outside a type.
				return withinNamedType != null && IsPrivateSymbolAccessible (withinNamedType, originalContainingType);

			case Accessibility.Internal:
				// An internal type is accessible if we're in the same assembly or we have
				// friend access to the assembly it was defined in.
				return withinAssembly.IsSameAssemblyOrHasFriendAccessTo (containingType.ContainingAssembly);

			case Accessibility.ProtectedAndInternal:
				if (!withinAssembly.IsSameAssemblyOrHasFriendAccessTo (containingType.ContainingAssembly)) {
					// We require internal access.  If we don't have it, then this symbol is
					// definitely not accessible to us.
					return false;
				}

				// We had internal access.  Also have to make sure we have protected access.
				return IsProtectedSymbolAccessible (withinNamedType, withinAssembly, throughTypeOpt, originalContainingType, out failedThroughTypeCheck);

			case Accessibility.ProtectedOrInternal:
				if (withinAssembly.IsSameAssemblyOrHasFriendAccessTo (containingType.ContainingAssembly)) {
					// If we have internal access to this symbol, then that's sufficient.  no
					// need to do the complicated protected case.
					return true;
				}

				// We don't have internal access.  But if we have protected access then that's
				// sufficient.
				return IsProtectedSymbolAccessible (withinNamedType, withinAssembly, throughTypeOpt, originalContainingType, out failedThroughTypeCheck);

			case Accessibility.Protected:
				return IsProtectedSymbolAccessible (withinNamedType, withinAssembly, throughTypeOpt, originalContainingType, out failedThroughTypeCheck);

			default:
				throw new Exception ("unreachable");
			}
		}

		// Is a protected symbol inside "originalContainingType" accessible from within "within",
		// which much be a named type or an assembly.
		private static bool IsProtectedSymbolAccessible (
			INamedTypeSymbol withinType,
			IAssemblySymbol withinAssembly,
			ITypeSymbol throughTypeOpt,
			INamedTypeSymbol originalContainingType,
			out bool failedThroughTypeCheck)
		{
			failedThroughTypeCheck = false;

			// It is not an error to define protected member in a sealed Script class, 
			// it's just a warning. The member behaves like a private one - it is visible 
			// in all subsequent submissions.
			if (withinAssembly.IsInteractive && originalContainingType.IsScriptClass) {
				return true;
			}

			if (withinType == null) {
				// If we're not within a type, we can't access a protected symbol
				return false;
			}

			// A protected symbol is accessible if we're (optionally nested) inside the type that it
			// was defined in. 

			// NOTE(ericli): It is helpful to consider 'protected' as *increasing* the
			// accessibility domain of a private member, rather than *decreasing* that of a public
			// member. Members are naturally private; the protected, internal and public access
			// modifiers all increase the accessibility domain. Since private members are accessible
			// to nested types, so are protected members.

			// NOTE(cyrusn): We do this check up front as it is very fast and easy to do.
			if (IsNestedWithinOriginalContainingType (withinType, originalContainingType)) {
				return true;
			}

			// Protected is really confusing.  Check out 3.5.3 of the language spec "protected access
			// for instance members" to see how it works.  I actually got the code for this from
			// LangCompiler::CheckAccessCore
			{
				var current = withinType.OriginalDefinition;
				var originalThroughTypeOpt = throughTypeOpt == null ? null : throughTypeOpt.OriginalDefinition;
				while (current != null) {
					//	Contract.Requires(current.IsDefinition);

					if (current.Equals (originalContainingType)) {
						// NOTE(cyrusn): We're continually walking up the 'throughType's inheritance
						// chain.  We could compute it up front and cache it in a set.  However, i
						// don't want to allocate memory in this function.  Also, in practice
						// inheritance chains should be very short.  As such, it might actually be
						// slower to create and check inside the set versus just walking the
						// inheritance chain.
						if (originalThroughTypeOpt == null ||
							originalThroughTypeOpt.Equals (current)) {
							return true;
						} else {
							failedThroughTypeCheck = true;
						}
					}

					// NOTE(cyrusn): The container of an original type is always original.
					current = current.ContainingType;
				}
			}

			return false;
		}

		// Is a private symbol access
		private static bool IsPrivateSymbolAccessible (
			ISymbol within,
			INamedTypeSymbol originalContainingType)
		{
			//Contract.Requires(within is INamedTypeSymbol || within is IAssemblySymbol);

			var withinType = within as INamedTypeSymbol;
			if (withinType == null) {
				// If we're not within a type, we can't access a private symbol
				return false;
			}

			// A private symbol is accessible if we're (optionally nested) inside the type that it
			// was defined in.
			return IsNestedWithinOriginalContainingType (withinType, originalContainingType);
		}

		// Is the type "withinType" nested within the original type "originalContainingType".
		private static bool IsNestedWithinOriginalContainingType (
			INamedTypeSymbol withinType,
			INamedTypeSymbol originalContainingType)
		{
			//			Contract.ThrowIfNull(withinType);
			//			Contract.ThrowIfNull(originalContainingType);

			// Walk up my parent chain and see if I eventually hit the owner.  If so then I'm a
			// nested type of that owner and I'm allowed access to everything inside of it.
			var current = withinType.OriginalDefinition;
			while (current != null) {
				//Contract.Requires(current.IsDefinition);
				if (current.Equals (originalContainingType)) {
					return true;
				}

				// NOTE(cyrusn): The container of an 'original' type is always original. 
				current = current.ContainingType;
			}

			return false;
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
	}
}

