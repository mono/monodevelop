//
// RoslynEventNodeBuilder.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Linq;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.AssemblyBrowser
{
	class RoslynTypeNodeBuilder : RoslynMemberNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof (ITypeSymbol); }
		}

		internal AssemblyBrowserWidget Widget {
			get;
			private set;
		}

		public RoslynTypeNodeBuilder (AssemblyBrowserWidget widget)
		{
			Widget = widget;
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ITypeSymbol)dataObject).GetFullName ();
		}

		//Same as MonoDevelop.Ide.TypeSystem.Ambience.NameFormat except SymbolDisplayTypeQualificationStyle is NameOnly
		static readonly SymbolDisplayFormat NameFormat =
			new SymbolDisplayFormat (
				globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
				propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
				genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
				memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
				parameterOptions:
				SymbolDisplayParameterOptions.IncludeParamsRefOut |
				SymbolDisplayParameterOptions.IncludeExtensionThis |
				SymbolDisplayParameterOptions.IncludeType |
				SymbolDisplayParameterOptions.IncludeName,
				miscellaneousOptions:
				SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
				SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			ITypeSymbol classData = dataObject as ITypeSymbol;
			nodeInfo.Label = Ambience.EscapeText (classData.ToDisplayString (NameFormat));
			nodeInfo.Icon = Context.GetIcon (classData.GetStockIcon ());
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			ITypeSymbol classData = dataObject as ITypeSymbol;
			bool publicOnly = Widget.PublicApiOnly;
			bool publicProtectedOnly = false;
			publicOnly |= publicProtectedOnly;

			// Delegates have an Invoke method, which doesn't need to be shown.
			if (classData.TypeKind == TypeKind.Delegate)
				return;

			builder.AddChildren (classData.GetTypeMembers ()
								 .Where (innerClass => innerClass.DeclaredAccessibility == Accessibility.Public ||
													   (innerClass.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
													   !publicOnly));

			builder.AddChildren (classData.GetMembers ().OfType<IMethodSymbol> ().Where (m => m.MethodKind != MethodKind.PropertyGet && m.MethodKind != MethodKind.PropertySet)
								 .Where (method => method.DeclaredAccessibility == Accessibility.Public ||
												   (method.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
												   !publicOnly));

			builder.AddChildren (classData.GetMembers ().OfType<IPropertySymbol> ()
								 .Where (property => property.DeclaredAccessibility == Accessibility.Public ||
										 (property.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
										 !publicOnly));

			builder.AddChildren (classData.GetMembers ().OfType<IFieldSymbol> ()
								 .Where (field => field.DeclaredAccessibility == Accessibility.Public ||
										 (field.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
										 !publicOnly));

			builder.AddChildren (classData.GetMembers ().OfType<IEventSymbol> ()
								 .Where (e => e.DeclaredAccessibility == Accessibility.Public ||
										 (e.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
										 !publicOnly));
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			// Checking if a class has member is expensive since it requires loading the whole
			// info from the db, so we always return true here. After all 99% of classes will have members
			return true;
		}
	}
}
