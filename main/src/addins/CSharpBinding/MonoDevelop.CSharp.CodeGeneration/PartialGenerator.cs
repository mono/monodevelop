//
// PartialGenerator.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

using System.Linq;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Core;
using System.Threading;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MonoDevelop.CodeGeneration
{
	class PartialGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-method";
			}
		}

		public string Text {
			get {
				return GettextCatalog.GetString ("Partial methods");
			}
		}

		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select methods to be implemented.");
			}
		}

		public bool IsValid (CodeGenerationOptions options)
		{
			return new PartialMethods (options).IsValid ();
		}

		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, TreeView treeView)
		{
			var overrideMethods = new PartialMethods (options);
			overrideMethods.Initialize (treeView);
			return overrideMethods;
		}

		class PartialMethods : AbstractGenerateAction
		{
			public PartialMethods (CodeGenerationOptions options) : base (options)
			{
			}

			protected override IEnumerable<object> GetValidMembers ()
			{
				var type = Options.EnclosingType;
				if (type == null || Options.EnclosingMember != null)
					yield break;

				foreach (var method in Options.EnclosingType.GetMembers ().OfType<Microsoft.CodeAnalysis.IMethodSymbol> ()) {
					if (method.MethodKind != Microsoft.CodeAnalysis.MethodKind.Ordinary)
						continue;
					if (IsEmptyPartialMethod(method)) {
						yield return method;
					}
				}	
			}

			static bool IsEmptyPartialMethod(Microsoft.CodeAnalysis.ISymbol member, CancellationToken cancellationToken = default(CancellationToken))
			{
				var method = member as Microsoft.CodeAnalysis.IMethodSymbol;
				if (method == null || method.IsDefinedInMetadata ())
					return false;
				foreach (var r in method.DeclaringSyntaxReferences) {
					var node = r.GetSyntax (cancellationToken) as Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax;
					if (node == null)
						continue;
					if (node.Body != null || !node.Modifiers.Any(m => m.IsKind (Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
						return false;
				}

				return true;
			}


			protected override IEnumerable<string> GenerateCode (List<object> includedMembers)
			{
				foreach (Microsoft.CodeAnalysis.IMethodSymbol member in includedMembers)
					yield return CSharpCodeGenerator.CreatePartialMemberImplementation (Options.DocumentContext, Options.Editor, Options.EnclosingType, Options.EnclosingPart.GetLocation (), member, false, null).Code;
			}
		}
	}
}
