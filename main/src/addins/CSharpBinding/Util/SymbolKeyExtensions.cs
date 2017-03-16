// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace ICSharpCode.NRefactory6.CSharp
{
	class SymbolKey
	{
		readonly static Type typeInfo;

		readonly object instance;

		static SymbolKey ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.SymbolKey" + ReflectionNamespaces.WorkspacesAsmName, true);
			resolveMethod = typeInfo.GetMethod ("Resolve", BindingFlags.Instance | BindingFlags.Public);
			createMethod = typeInfo.GetMethod ("Create", BindingFlags.Static | BindingFlags.NonPublic);
		}

		SymbolKey (object instance)
		{
			this.instance = instance;
		}

		static MethodInfo createMethod;

		/// <summary>
		/// <para>
		/// This entry point should only be called from the actual Symbol classes. It should not be
		/// used internally inside this type.  Instead, any time we need to get the <see cref="SymbolKey"/> for a
		/// related symbol (i.e. the containing namespace of a namespace) we should call
		/// GetOrCreate.  The benefit of this is twofold.  First of all, it keeps the size of the
		/// <see cref="SymbolKey"/> small by allowing up to reuse parts we've already created.  For example, if we
		/// have the <see cref="SymbolKey"/> for <c>Foo(int, int)</c>, then we will reuse the <see cref="SymbolKey"/>s for both <c>int</c>s.
		/// Second, this allows us to deal with the recursive nature of MethodSymbols and
		/// TypeParameterSymbols.  Specifically, a MethodSymbol is defined by its signature.  However,
		/// it's signature may refer to type parameters of that method.  Unfortunately, the type
		/// parameters depend on their containing method.
		/// </para>
		/// <para>
		/// For example, if there is <c><![CDATA[Foo<T>(T t)]]></c>, then we must avoid the situation where we:
		/// <list type="number">
		/// <item>try to get the symbol ID for the type parameter <c>T</c>, which in turn</item>
		/// <item>tries to get the symbol ID for the method <c>T</c>, which in turn</item>
		/// <item>tries to get the symbol IDs for the parameter types, which in turn</item>
		/// <item>tries to get the symbol ID for the type parameter <c>T</c>, which leads back to 1 and infinitely loops.</item>
		/// </list>
		/// </para>
		/// <para>
		/// In order to break this circularity we do not create the SymbolIDs for a method's type
		/// parameters directly in the visitor.  Instead, we create the SymbolID for the method
		/// itself.  When the MethodSymbolId is created it will directly instantiate the SymbolIDs
		/// for the type parameters, and directly assign the type parameter's method ID to itself.
		/// It will also then directly store the mapping from the type parameter to its SymbolID in
		/// the visitor cache.  Then when we try to create the symbol IDs for the parameter types,
		/// any reference to the type parameters can be found in the cache.
		/// </para>
		/// <para>
		/// It is for this reason that it is essential that all calls to get related symbol IDs goes
		/// through GetOrCreate and not Create.
		/// </para>
		/// </summary>
		internal static SymbolKey Create(ISymbol symbol, CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				var instance = createMethod.Invoke (null, new object [] { symbol, cancellationToken });
				return new SymbolKey (instance);
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}

		static MethodInfo resolveMethod;

		public SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey = false, CancellationToken cancellationToken = default(CancellationToken))
		{
			try {
				return new SymbolKeyResolution (resolveMethod.Invoke (instance, new object[] { compilation, ignoreAssemblyKey, cancellationToken }));
			} catch (TargetInvocationException ex) {
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				return null;
			}
		}
	}

	class SymbolKeyResolution
	{
		readonly static Type typeInfo;
		readonly static PropertyInfo symbolProperty;
		readonly static PropertyInfo candidateSymbolsProperty;
		readonly static PropertyInfo candidateReasonProperty;

		readonly object instance;


		public ISymbol Symbol
		{
			get { return (ISymbol)symbolProperty.GetValue (instance); }
		}

		public ImmutableArray<ISymbol> CandidateSymbols
		{
			get { return (ImmutableArray<ISymbol>)candidateSymbolsProperty.GetValue (instance); }
		}

		public CandidateReason CandidateReason
		{
			get { return (CandidateReason)candidateReasonProperty.GetValue (instance); }
		}

		static SymbolKeyResolution ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.SymbolKeyResolution" + ReflectionNamespaces.WorkspacesAsmName, true);

			symbolProperty = typeInfo.GetProperty ("Symbol");
			candidateSymbolsProperty = typeInfo.GetProperty ("CandidateSymbols");
			candidateReasonProperty = typeInfo.GetProperty ("CandidateReason");

		}

		public SymbolKeyResolution (object instance)
		{
			this.instance = instance;
		}


	}

	static class SymbolKeyExtensions
	{
		public static SymbolKey GetSymbolKey(this ISymbol symbol)
		{
			return SymbolKey.Create(symbol, CancellationToken.None);
		}

		#if false
		internal static SymbolKey GetSymbolKey(this ISymbol symbol, Compilation compilation, CancellationToken cancellationToken)
		{
		return SymbolKey.Create(symbol, compilation, cancellationToken);
		}
		#endif


	}
}
