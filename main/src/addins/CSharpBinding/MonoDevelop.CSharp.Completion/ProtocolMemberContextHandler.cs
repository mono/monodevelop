//
// ProtocolMemberContextHandler.cs
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
using ICSharpCode.NRefactory6.CSharp.Completion;
using System.Collections.Generic;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.Linq;
using Mono.Addins.Description;

namespace MonoDevelop.CSharp.Completion
{	
	interface IExtensionContextHandler
	{
		void Init (RoslynCodeCompletionFactory factory);
	}

	class ProtocolMemberContextHandler : OverrideContextHandler, IExtensionContextHandler
	{
		RoslynCodeCompletionFactory factory;

		void IExtensionContextHandler.Init (RoslynCodeCompletionFactory factory)
		{
			this.factory = factory;
		}

		protected override IEnumerable<ICompletionData> CreateCompletionData (CompletionEngine engine, SemanticModel semanticModel, int position, ITypeSymbol returnType, Accessibility seenAccessibility, SyntaxToken startToken, SyntaxToken tokenBeforeReturnType, bool afterKeyword, CancellationToken cancellationToken)
		{
			var result = new List<ICompletionData> ();
			ISet<ISymbol> overridableMembers;
			if (!TryDetermineOverridableMembers (semanticModel, tokenBeforeReturnType, seenAccessibility, out overridableMembers, cancellationToken)) {
				return result;
			}
			if (returnType != null) {
				overridableMembers = FilterOverrides (overridableMembers, returnType);
			}
			var curType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol> (startToken.SpanStart, cancellationToken);
			var declarationBegin = afterKeyword ? startToken.SpanStart : position;
			foreach (var m in overridableMembers) {
				var data = new ProtocolCompletionData (this, factory, declarationBegin, curType, m, afterKeyword);
				result.Add (data);
			}
			return result;
		}

		static bool TryDetermineOverridableMembers(SemanticModel semanticModel, SyntaxToken startToken, Accessibility seenAccessibility, out ISet<ISymbol> overridableMembers, CancellationToken cancellationToken)
		{
			var result = new HashSet<ISymbol>();
			var containingType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol>(startToken.SpanStart, cancellationToken);
			if (containingType != null && !containingType.IsScriptClass && !containingType.IsImplicitClass)
			{
				if (containingType.TypeKind == TypeKind.Class || containingType.TypeKind == TypeKind.Struct)
				{
					var baseTypes = containingType.GetBaseTypes().Reverse();
					foreach (var type in baseTypes)
					{
						cancellationToken.ThrowIfCancellationRequested();

						// Prefer overrides in derived classes
						RemoveOverriddenMembers(result, type, cancellationToken);

						// Retain overridable methods
						AddProtocolMembers(semanticModel, result, containingType, type, cancellationToken);
					}
					// Don't suggest already overridden members
					RemoveOverriddenMembers(result, containingType, cancellationToken);
				}
			}

			// Filter based on accessibility
			if (seenAccessibility != Accessibility.NotApplicable)
			{
				result.RemoveWhere(m => m.DeclaredAccessibility != seenAccessibility);
			}

			overridableMembers = result;
			return overridableMembers.Count > 0;
		}

		static void AddProtocolMembers(SemanticModel semanticModel, HashSet<ISymbol> result, INamedTypeSymbol containingType, INamedTypeSymbol type, CancellationToken cancellationToken)
		{
			string name;
			if (!HasProtocolAttribute (containingType, out name))
				return;

			var protocolType = semanticModel.Compilation.GetTypeByMetadataName (name);
			if (protocolType == null)
				return;
			
			foreach (var member in protocolType.GetMembers ().OfType<IMethodSymbol> ()) {
				if (member.ExplicitInterfaceImplementations.Length > 0 || member.IsAbstract || !member.IsVirtual)
					continue;
				if (member.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && IsFoundationNamespace (a.AttributeClass.ContainingNamespace.GetFullName ()))) {
					result.Add (member);
				}

			}
			foreach (var member in protocolType.GetMembers ().OfType<IPropertySymbol> ()) {
				if (member.ExplicitInterfaceImplementations.Length > 0 || member.IsAbstract || !member.IsVirtual)
					continue;
				if (member.GetMethod != null && member.GetMethod.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && IsFoundationNamespace (a.AttributeClass.ContainingNamespace.GetFullName ())) ||
					member.SetMethod != null && member.SetMethod.GetAttributes ().Any (a => a.AttributeClass.Name == "ExportAttribute" && IsFoundationNamespace (a.AttributeClass.ContainingNamespace.GetFullName ())))
					result.Add (member);
			}
		}

		internal static bool IsFoundationNamespace (string ns )
		{
			return (ns == "MonoTouch.Foundation" || ns == "Foundation");
		}

		internal static bool IsFoundationNamespace (INamespaceSymbol ns)
		{
			return IsFoundationNamespace (ns.GetFullName ());
		}

		internal static bool HasProtocolAttribute (INamedTypeSymbol type, out string name)
		{
			foreach (var attrs in type.GetAttributes ()) {
				if (attrs.AttributeClass.Name == "ProtocolAttribute" && IsFoundationNamespace (attrs.AttributeClass.ContainingNamespace.GetFullName ())) {
					foreach (var na in attrs.NamedArguments) {
						if (na.Key != "Name")
							continue;
						name = na.Value.Value as string;
						if (name != null)
							return true;
					}
				}
			}
			name = null;
			return false;
		}
	}
}