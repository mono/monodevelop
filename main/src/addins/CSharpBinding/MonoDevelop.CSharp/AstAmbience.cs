// 
// AstAmbience.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;

namespace MonoDevelop.CSharp
{
	struct AstAmbience
	{
//		OptionSet options;
		
		public AstAmbience (OptionSet options)
		{
//			this.options = options;
		}
		
		static bool IsObsolete (MemberDeclarationSyntax entity)
		{
			if (entity == null)
				return false;
			// TODO!
//			foreach (var section in entity.Attributes) {
//				foreach (var attr in section.Attributes) {
//					var attrText = attr.Type.ToString ();
//					if (attrText == "Obsolete" || attrText == "ObsoleteAttribute" || attrText == "System.Obsolete" || attrText == "System.ObsoleteAttribute" )
//						return true;
//				}
//			}
			return false;
		}
		
		void AppendTypeParameter (StringBuilder sb, TypeParameterListSyntax parameters)
		{
			if (parameters == null || parameters.Parameters.Count == 0) 
				return;
			sb.Append ("&lt;");
			bool first = true;
			foreach (var param in parameters.Parameters) {
				if (!first) {
					sb.Append (", ");
				} else {
					first = false;
				}
				AppendEscaped (sb, param.ToString ());
			}
			sb.Append ("&gt;");
		}
		
		void AppendParameter (StringBuilder sb, ParameterListSyntax parameters)
		{
			// Missing roslyn formatting option ?
			//			if (options.GetOption (CSharpFormattingOptions.Spacing ???))
			//	sb.Append (" ");
			sb.Append ("(");
			var hasParameters = parameters != null && parameters.Parameters.Count > 0;

			// Missing roslyn formatting option ?
			//if (hasParameters && options.GetOption (SpaceWithinMethodDeclarationParentheses))
			//	sb.Append (" ");

			bool first = true;
			if (hasParameters) {
				foreach (var param in parameters.Parameters) {
					if (!first) {
						//if (options.SpaceBeforeMethodDeclarationParameterComma)
						//	sb.Append (" ");
						sb.Append (",");
						//if (options.SpaceAfterMethodDeclarationParameterComma)
						sb.Append (" ");
					} else {
						first = false;
					}
					foreach (var mod in param.Modifiers) {
						sb.Append (mod.ToString ());
						sb.Append (" ");
					}
					AppendEscaped (sb, StripTrivia(param.Type.ToString ()));
					sb.Append (" ");
					AppendEscaped (sb, param.Identifier.ToString ());
				}
			}

			// Missing roslyn formatting option ?
			//if (hasParameters && options.SpaceWithinMethodDeclarationParentheses)
			//	sb.Append (" ");
			sb.Append (")");
		}

		string StripTrivia (string str)
		{
			var result = StringBuilderCache.Allocate ();
			foreach (char ch in str) {
				if (char.IsWhiteSpace (ch))
					continue;
				result.Append (ch);
			}
			return StringBuilderCache.ReturnAndFree (result);
		}

		static void AppendEscaped (StringBuilder result, string text)
		{
			if (text == null)
				return;
			foreach (char ch in text) {
				switch (ch) {
				case '<':
					result.Append ("&lt;");
					break;
				case '>':
					result.Append ("&gt;");
					break;
				case '&':
					result.Append ("&amp;");
					break;
				case '\'':
					result.Append ("&apos;");
					break;
				case '"':
					result.Append ("&quot;");
					break;
				default:
					result.Append (ch);
					break;
				}
			}
		}
		
		public string GetEntityMarkup (SyntaxNode e)
		{
			var sb = StringBuilderCache.Allocate ();
			if (e is NamespaceDeclarationSyntax) {
				return ((NamespaceDeclarationSyntax)e).Name.ToString ();
			}
			if (e is TypeDeclarationSyntax) {
				var type = e as TypeDeclarationSyntax;
				sb.Append (type.Identifier.ToString ());
				AppendTypeParameter (sb, type.TypeParameterList);
			} else if (e is DelegateDeclarationSyntax) {
				var del = e as DelegateDeclarationSyntax;
				sb.Append (del.Identifier.ToString ());
				AppendTypeParameter (sb, del.TypeParameterList);
				AppendParameter (sb, del.ParameterList);
			} else if (e is AccessorDeclarationSyntax) {
				if (e.Kind () == SyntaxKind.GetAccessorDeclaration) {
					sb.Append ("get");
				} else if (e.Kind () == SyntaxKind.SetAccessorDeclaration) {
					sb.Append ("set");
				} else if (e.Kind () == SyntaxKind.AddAccessorDeclaration) {
					sb.Append ("add");
				} else if (e.Kind () == SyntaxKind.RemoveAccessorDeclaration) {
					sb.Append ("remove");
				}
			} else if (e is OperatorDeclarationSyntax) {
				var op = (OperatorDeclarationSyntax)e;
				sb.Append ("operator ");
				AppendEscaped (sb, op.OperatorToken.ToString ());
				AppendParameter (sb, op.ParameterList);
			} else if (e is ConversionOperatorDeclarationSyntax) {
				var op = (ConversionOperatorDeclarationSyntax)e;
				sb.Append (op.ImplicitOrExplicitKeyword.IsKind (SyntaxKind.ImplicitKeyword) ? "implicit " : "explicit ");
				sb.Append ("operator ");
				AppendEscaped (sb, op.Type.ToString ());
				AppendParameter (sb, op.ParameterList);
			} else if (e is MethodDeclarationSyntax) {
				var method = (MethodDeclarationSyntax)e;
				if (method.ExplicitInterfaceSpecifier != null)
					AppendEscaped (sb, method.ExplicitInterfaceSpecifier + ".");
				sb.Append (method.Identifier.ToString ());
				AppendTypeParameter (sb, method.TypeParameterList);
				AppendParameter (sb, method.ParameterList);
				if (method.Body != null && !method.Body.IsMissing) {
					string tag = null;
					if (method.Modifiers.Any (m => m.Kind () == SyntaxKind.AbstractKeyword))
						tag = "(abstract)";
					if (method.Modifiers.Any (m => m.Kind () == SyntaxKind.PartialKeyword))
						tag = "(partial)";
					if (tag != null) {
						sb.Append (" <small>");
						sb.Append (tag);
						sb.Append ("</small>");
					}
				}
			} else if (e is ConstructorDeclarationSyntax) {
				var constructor = e as ConstructorDeclarationSyntax;
				sb.Append (constructor.Identifier.ToString ());
				AppendParameter (sb, constructor.ParameterList);
			} else if (e is DestructorDeclarationSyntax) {
				var destructror = e as DestructorDeclarationSyntax;
				sb.Append ("~");
				sb.Append (destructror.Identifier.ToString ());
				//				if (options.SpaceBeforeMethodDeclarationParentheses)
				//	sb.Append (" ");
				sb.Append ("()");
			} else if (e is IndexerDeclarationSyntax) {
				var indexer = e as IndexerDeclarationSyntax;
				sb.Append ("this");
				//if (options.SpaceBeforeIndexerDeclarationBracket)
				//	sb.Append (" ");
				sb.Append ("[");
				//if (options.SpaceWithinIndexerDeclarationBracket)
				//	sb.Append (" ");
				
				bool first = true;
				foreach (var param in indexer.ParameterList.Parameters) {
					if (!first) {
						//if (options.SpaceBeforeIndexerDeclarationParameterComma)
						//	sb.Append (" ");
						sb.Append (",");
						//if (options.SpaceAfterIndexerDeclarationParameterComma)
						//	sb.Append (" ");
					} else {
						first = false;
					}
					sb.Append (param.ToString ());
				}
				//if (options.SpaceWithinIndexerDeclarationBracket)
				//	sb.Append (" ");
				sb.Append ("]");
			} else if (e is VariableDeclaratorSyntax) {
				var initializer = (VariableDeclaratorSyntax)e;
				sb.Append (initializer.Identifier.ToString ());
				if (IsObsolete (initializer.Parent as MemberDeclarationSyntax))
					return "<s>" + sb.ToString () + "</s>";
			} else if (e is FieldDeclarationSyntax) {
				var field = (FieldDeclarationSyntax)e;
				if (!field.Declaration.Variables.Any ())
					return "";
				sb.Append (field.Declaration.Variables.First ().Identifier.ToString ());
			} else if (e is EventFieldDeclarationSyntax) {
				var evt = (EventFieldDeclarationSyntax)e;
				if (!evt.Declaration.Variables.Any ())
					return "";
				sb.Append (evt.Declaration.Variables.First ().Identifier.ToString ());
			} else if (e is PropertyDeclarationSyntax) {
				var property = (PropertyDeclarationSyntax)e;
				if (property.ExplicitInterfaceSpecifier != null) {
					AppendEscaped (sb, property.ExplicitInterfaceSpecifier.ToString ());
					sb.Append (".");
				}
				sb.Append (property.Identifier.ToString ());
			} else if (e is EventDeclarationSyntax) {
				var customEvent = (EventDeclarationSyntax)e;
				if (customEvent.ExplicitInterfaceSpecifier != null) {
					AppendEscaped (sb, customEvent.ExplicitInterfaceSpecifier.ToString ());
					sb.Append (".");
				}
				sb.Append (customEvent.Identifier.ToString ());
			} else if (e is EnumDeclarationSyntax) {
				var enumDecl = (EnumDeclarationSyntax)e;
				sb.Append (enumDecl.Identifier.ToString ());
			} else if (e is EnumMemberDeclarationSyntax) {
				var enumMemberDecl = (EnumMemberDeclarationSyntax)e;
				sb.Append (enumMemberDecl.Identifier.ToString ());
			} /*else if (e is MemberDeclarationSyntax) {
				LoggingService.LogWarning ("can't display : " + e);
				//				var entity = (MemberDeclarationSyntax)e;
				// sb.Append (entity.Name);
			}*/

			if (IsObsolete (e as MemberDeclarationSyntax)) {
				sb.Append ("</s>");
				sb.Insert (0, "<s>");
			}
			return StringBuilderCache.ReturnAndFree (sb);
		}
	}
}
