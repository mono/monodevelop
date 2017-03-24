// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
	[Obsolete]
	internal static class PortingExtensions
	{
		/// <summary>
		/// Gets the invoke method for a delegate type.
		/// </summary>
		/// <remarks>
		/// Returns null if the type is not a delegate type; or if the invoke method could not be found.
		/// </remarks>
		public static IMethodSymbol GetDelegateInvokeMethod (this ITypeSymbol type)
		{
			if (type == null)
				throw new ArgumentNullException (nameof (type));
			if (type.TypeKind == TypeKind.Delegate)
				return type.GetMembers ("Invoke").OfType<IMethodSymbol> ().FirstOrDefault (m => m.MethodKind == MethodKind.DelegateInvoke);
			return null;
		}

		public static async Task<CompilationUnitSyntax> GetCSharpSyntaxRootAsync (this Document document, CancellationToken cancellationToken = default (CancellationToken))
		{
			var root = await document.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
			return (CompilationUnitSyntax)root;
		}

		public static Task<SyntaxTree> GetCSharpSyntaxTreeAsync (this Document document, CancellationToken cancellationToken = default (CancellationToken))
		{
			return document.GetSyntaxTreeAsync (cancellationToken);
		}

		public static Task<SemanticModel> GetCSharpSemanticModelForNodeAsync (this Document document, SyntaxNode node, CancellationToken cancellationToken = default (CancellationToken))
		{
			return document.GetSemanticModelForNodeAsync (node, cancellationToken);
		}

		public static Task<SemanticModel> GetCSharpSemanticModelForSpanAsync (this Document document, TextSpan span, CancellationToken cancellationToken = default (CancellationToken))
		{
			return document.GetSemanticModelForSpanAsync (span, cancellationToken);
		}

		public static Task<Compilation> GetCSharpCompilationAsync (this Document document, CancellationToken cancellationToken = default (CancellationToken))
		{
			return document.Project.GetCompilationAsync (cancellationToken);
		}

		public static async Task<IEnumerable<T>> GetUnionResultsFromDocumentAndLinks<T> (
			this Document document,
			IEqualityComparer<T> comparer,
			Func<Document, CancellationToken, Task<IEnumerable<T>>> getItemsWorker,
			CancellationToken cancellationToken)
		{
			var linkedDocumentIds = document.GetLinkedDocumentIds ();
			var itemsForCurrentContext = await getItemsWorker (document, cancellationToken).ConfigureAwait (false) ?? SpecializedCollections.EmptyEnumerable<T> ();
			if (!linkedDocumentIds.Any ()) {
				return itemsForCurrentContext;
			}

			ISet<T> totalItems = itemsForCurrentContext.ToSet (comparer);
			foreach (var linkedDocumentId in linkedDocumentIds) {
				var linkedDocument = document.Project.Solution.GetDocument (linkedDocumentId);
				var items = await getItemsWorker (linkedDocument, cancellationToken).ConfigureAwait (false);
				if (items != null) {
					foreach (var item in items)
						totalItems.Add (item);
				}
			}

			return totalItems;
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
		/// Gets the EditorBrowsableState of an entity.
		/// </summary>
		/// <returns>
		/// The editor browsable state.
		/// </returns>
		/// <param name='symbol'>
		/// Entity.
		/// </param>
		public static System.ComponentModel.EditorBrowsableState GetEditorBrowsableState (this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			var browsableState = symbol.GetAttributes ().FirstOrDefault (attr => attr.AttributeClass.Name == "EditorBrowsableAttribute" && attr.AttributeClass.ContainingNamespace.MetadataName == "System.ComponentModel");
			if (browsableState != null && browsableState.ConstructorArguments.Length == 1) {
				try {
					return (System.ComponentModel.EditorBrowsableState)browsableState.ConstructorArguments [0].Value;
				} catch {
				}
			}
			return System.ComponentModel.EditorBrowsableState.Always;
		}

		/// <summary>
		/// Determines if an entity should be shown in the code completion window. This is the same as:
		/// <c>GetEditorBrowsableState (entity) != System.ComponentModel.EditorBrowsableState.Never</c>
		/// </summary>
		/// <returns>
		/// <c>true</c> if the entity should be shown; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='symbol'>
		/// The entity.
		/// </param>
		public static bool IsEditorBrowsable (this ISymbol symbol)
		{
			if (symbol == null)
				throw new ArgumentNullException ("symbol");
			return GetEditorBrowsableState (symbol) != System.ComponentModel.EditorBrowsableState.Never;
		}

		public static void TypeSwitch<TBaseType, TDerivedType1, TDerivedType2> (this TBaseType obj, Action<TDerivedType1> matchAction1, Action<TDerivedType2> matchAction2, Action<TBaseType> defaultAction = null)
			where TDerivedType1 : TBaseType
			where TDerivedType2 : TBaseType
		{
			if (obj is TDerivedType1) {
				matchAction1 ((TDerivedType1)obj);
			} else if (obj is TDerivedType2) {
				matchAction2 ((TDerivedType2)obj);
			} else {
				defaultAction?.Invoke (obj);
			}
		}

		public static void TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3> (this TBaseType obj, Action<TDerivedType1> matchAction1, Action<TDerivedType2> matchAction2, Action<TDerivedType3> matchAction3, Action<TBaseType> defaultAction = null)
			where TDerivedType1 : TBaseType
			where TDerivedType2 : TBaseType
			where TDerivedType3 : TBaseType
		{
			if (obj is TDerivedType1) {
				matchAction1 ((TDerivedType1)obj);
			} else if (obj is TDerivedType2) {
				matchAction2 ((TDerivedType2)obj);
			} else if (obj is TDerivedType3) {
				matchAction3 ((TDerivedType3)obj);
			} else {
				defaultAction?.Invoke (obj);
			}
		}

		public static void TypeSwitch<TBaseType, TDerivedType1, TDerivedType2, TDerivedType3, TDerivedType4> (this TBaseType obj, Action<TDerivedType1> matchAction1, Action<TDerivedType2> matchAction2, Action<TDerivedType3> matchAction3, Action<TDerivedType4> matchAction4, Action<TBaseType> defaultAction = null)
			where TDerivedType1 : TBaseType
			where TDerivedType2 : TBaseType
			where TDerivedType3 : TBaseType
			where TDerivedType4 : TBaseType
		{
			if (obj is TDerivedType1) {
				matchAction1 ((TDerivedType1)obj);
			} else if (obj is TDerivedType2) {
				matchAction2 ((TDerivedType2)obj);
			} else if (obj is TDerivedType3) {
				matchAction3 ((TDerivedType3)obj);
			} else if (obj is TDerivedType4) {
				matchAction4 ((TDerivedType4)obj);
			} else {
				defaultAction?.Invoke (obj);
			}
		}

		public static ExpressionSyntax SkipParens (this ExpressionSyntax expression)
		{
			if (expression == null)
				return null;
			while (expression != null && expression.IsKind (SyntaxKind.ParenthesizedExpression)) {
				expression = ((ParenthesizedExpressionSyntax)expression).Expression;
			}
			return expression;
		}
	}
}
