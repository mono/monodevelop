// 
// OverrideMethodsGenerator.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using MonoDevelop.Core;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.CSharp.Refactoring;

namespace MonoDevelop.CodeGeneration
{
	class OverrideMembersGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-method";
			}
		}

		public string Text {
			get {
				return GettextCatalog.GetString ("Override members");
			}
		}

		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members to be overridden.");
			}
		}

		public bool IsValid (CodeGenerationOptions options)
		{
			return new OverrideMethods (options).IsValid ();
		}

		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			OverrideMethods overrideMethods = new OverrideMethods (options);
			overrideMethods.Initialize (treeView);
			return overrideMethods;
		}

		class OverrideMethods : AbstractGenerateAction
		{
			public OverrideMethods (CodeGenerationOptions options) : base (options)
			{
			}

			protected override IEnumerable<object> GetValidMembers ()
			{
				var encType = Options.EnclosingType as INamedTypeSymbol;
				if (encType == null || Options.EnclosingMember != null)
					return Enumerable.Empty<object> ();


				var result = new HashSet<ISymbol> ();
				var cancellationToken = default (CancellationToken);
				var baseTypes = encType.GetBaseTypes ().Reverse ();
				foreach (var type in baseTypes) {
					RemoveOverriddenMembers (result, type, cancellationToken);

					AddOverridableMembers (result, encType, type, cancellationToken);
				}
				RemoveOverriddenMembers (result, encType, cancellationToken);
				return result;
			}

			static void AddOverridableMembers (HashSet<ISymbol> result, INamedTypeSymbol containingType, INamedTypeSymbol type, CancellationToken cancellationToken)
			{
				foreach (var member in type.GetMembers ()) {
					if (IsOverridable (member, containingType)) {
						result.Add (member);
					}
				}
			}

			protected static void RemoveOverriddenMembers (HashSet<ISymbol> result, INamedTypeSymbol containingType, CancellationToken cancellationToken)
			{
				foreach (var member in containingType.GetMembers ()) {
					var overriddenMember = member.OverriddenMember ();
					if (overriddenMember != null) {
						result.Remove (overriddenMember);
					}
				}
			}

			public static bool IsOverridable (ISymbol member, INamedTypeSymbol containingType)
			{
				if (member.IsAbstract || member.IsVirtual || member.IsOverride) {
					if (member.IsSealed) {
						return false;
					}

					if (!member.IsAccessibleWithin (containingType)) {
						return false;
					}

					switch (member.Kind) {
					case SymbolKind.Event:
						return true;
					case SymbolKind.Method:
						return ((IMethodSymbol)member).MethodKind == MethodKind.Ordinary;
					case SymbolKind.Property:
						return !((IPropertySymbol)member).IsWithEvents;
					}
				}
				return false;
			}

			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				var currentType = Options.EnclosingType as INamedTypeSymbol;

				foreach (ISymbol member in includedMembers) {
					yield return CSharpCodeGenerator.CreateOverridenMemberImplementation (Options.DocumentContext, Options.Editor, currentType, currentType.Locations.First (), member, false, null).Code;
				}
			}
		}
	}
}
