//
// DeclaredSymbolInfo.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;
using Gtk;
using MonoDevelop.Ide;
using MonoDevelop.CSharp;
using MonoDevelop.Components.MainToolbar;

namespace MonoDevelop.CSharp
{
	static class DeclaredSymbolInfoHelpers
	{
		public static bool TryGetDeclaredSymbolInfo(this SyntaxNode node, out DeclaredSymbolInfo declaredSymbolInfo)
		{
			switch (node.Kind())
			{
				case SyntaxKind.ClassDeclaration:
					       var classDecl = (ClassDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                        classDecl.Identifier.ValueText,
					                                        // GetContainerDisplayName(node.Parent),
					                                            GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.Class, classDecl.Identifier.Span);
				return true;
				case SyntaxKind.ConstructorDeclaration:
					       var ctorDecl = (ConstructorDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(
						node,
						ctorDecl.Identifier.ValueText,
						// GetContainerDisplayName(node.Parent),
						GetFullyQualifiedContainerName(node.Parent),
						DeclaredSymbolInfoKind.Constructor,
						ctorDecl.Identifier.Span,
						parameterCount: (ushort)(ctorDecl.ParameterList?.Parameters.Count ?? 0));
				return true;
				case SyntaxKind.DelegateDeclaration:
					       var delegateDecl = (DelegateDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                        delegateDecl.Identifier.ValueText,
					                                        // GetContainerDisplayName(node.Parent),
					                                        GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.Delegate, delegateDecl.Identifier.Span);
				return true;
				case SyntaxKind.EnumDeclaration:
					       var enumDecl = (EnumDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                        enumDecl.Identifier.ValueText,
					                                        // GetContainerDisplayName(node.Parent),
					                                        GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.Enum, enumDecl.Identifier.Span);
				return true;
				case SyntaxKind.EnumMemberDeclaration:
					       var enumMember = (EnumMemberDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                        enumMember.Identifier.ValueText,
					                                        // GetContainerDisplayName(node.Parent),
					                                        GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.EnumMember, enumMember.Identifier.Span);
				return true;
				case SyntaxKind.EventDeclaration:
					       var eventDecl = (EventDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                            ExpandExplicitInterfaceName(eventDecl.Identifier.ValueText, eventDecl.ExplicitInterfaceSpecifier),
					                                        // GetContainerDisplayName(node.Parent),
					                                        GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.Event, eventDecl.Identifier.Span);
				return true;
				case SyntaxKind.IndexerDeclaration:
					       var indexerDecl = (IndexerDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                        WellKnownMemberNames.Indexer,
					                                        // GetContainerDisplayName(node.Parent),
					                                        GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.Indexer, indexerDecl.ThisKeyword.Span);
				return true;
				case SyntaxKind.InterfaceDeclaration:
					       var interfaceDecl = (InterfaceDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                        interfaceDecl.Identifier.ValueText,
					                                        // GetContainerDisplayName(node.Parent),
					                                        GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.Interface, interfaceDecl.Identifier.Span);
				return true;
				case SyntaxKind.MethodDeclaration:
					       var method = (MethodDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                        ExpandExplicitInterfaceName(method.Identifier.ValueText, method.ExplicitInterfaceSpecifier),
					                                        // GetContainerDisplayName(node.Parent),
					                                        GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.Method,
					                                        method.Identifier.Span,
					                                        parameterCount: (ushort)(method.ParameterList?.Parameters.Count ?? 0),
					                                        typeParameterCount: (ushort)(method.TypeParameterList?.Parameters.Count ?? 0));
				return true;
				case SyntaxKind.PropertyDeclaration:
					       var property = (PropertyDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                        ExpandExplicitInterfaceName(property.Identifier.ValueText, property.ExplicitInterfaceSpecifier),
					                                        // GetContainerDisplayName(node.Parent),
					                                        GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.Property, property.Identifier.Span);
				return true;
				case SyntaxKind.StructDeclaration:
					       var structDecl = (StructDeclarationSyntax)node;
					declaredSymbolInfo = new DeclaredSymbolInfo(node,
					                                        structDecl.Identifier.ValueText,
					                                        // GetContainerDisplayName(node.Parent),
					                                        GetFullyQualifiedContainerName(node.Parent),
					                                        DeclaredSymbolInfoKind.Struct, structDecl.Identifier.Span);
				return true;
				case SyntaxKind.VariableDeclarator:
					       // could either be part of a field declaration or an event field declaration
					       var variableDeclarator = (VariableDeclaratorSyntax)node;
					var variableDeclaration = variableDeclarator.Parent as VariableDeclarationSyntax;
					var fieldDeclaration = variableDeclaration?.Parent as BaseFieldDeclarationSyntax;
					if (fieldDeclaration != null)
					{
						var kind = fieldDeclaration is EventFieldDeclarationSyntax
							? DeclaredSymbolInfoKind.Event
						                        : fieldDeclaration.Modifiers.Any(m => m.Kind() == SyntaxKind.ConstKeyword)
						                        ? DeclaredSymbolInfoKind.Constant
						                        : DeclaredSymbolInfoKind.Field;

						declaredSymbolInfo = new DeclaredSymbolInfo(node,
						                                        variableDeclarator.Identifier.ValueText,
						                                        // GetContainerDisplayName(fieldDeclaration.Parent),
						                                        GetFullyQualifiedContainerName(fieldDeclaration.Parent),
						                                        kind, variableDeclarator.Identifier.Span);
						return true;
					}

				break;
			}

			declaredSymbolInfo = default(DeclaredSymbolInfo);
			return false;
		}

		private static string GetContainerDisplayName(SyntaxNode node)
		{
			return GetContainer(node, immediate: true);
		}

		private static string GetFullyQualifiedContainerName(SyntaxNode node)
		{
			return GetContainer(node, immediate: false);
		}

		private static string GetContainer(SyntaxNode node, bool immediate)
		{
			var name = GetNodeName(node, includeTypeParameters: immediate);
			var names = new List<string> { name };

			// check for nested classes and always add that to the container name.
			var parent = node.Parent;
			while (parent is TypeDeclarationSyntax)
			{
				var currentParent = (TypeDeclarationSyntax)parent;
				names.Add(currentParent.Identifier.ValueText + (immediate ? ExpandTypeParameterList(currentParent.TypeParameterList) : ""));
				parent = currentParent.Parent;
			}

			// If they're just asking for the immediate parent, then we're done. Otherwise keep 
			// walking all the way to the root, adding the names.
			if (!immediate)
			{
				while (parent != null && parent.Kind() != SyntaxKind.CompilationUnit)
				{
					names.Add(GetNodeName(parent, includeTypeParameters: false));
					parent = parent.Parent;
				}
			}

			names.Reverse();
			return string.Join(".", names);
		}

		private static string GetNodeName(SyntaxNode node, bool includeTypeParameters)
		{
			string name;
			TypeParameterListSyntax typeParameterList;
			switch (node.Kind())
			{
				case SyntaxKind.ClassDeclaration:
					       var classDecl = (ClassDeclarationSyntax)node;
					name = classDecl.Identifier.ValueText;
					typeParameterList = classDecl.TypeParameterList;
				break;
				case SyntaxKind.CompilationUnit:
				return string.Empty;
				case SyntaxKind.DelegateDeclaration:
					       var delegateDecl = (DelegateDeclarationSyntax)node;
					name = delegateDecl.Identifier.ValueText;
					typeParameterList = delegateDecl.TypeParameterList;
				break;
				case SyntaxKind.EnumDeclaration:
				return ((EnumDeclarationSyntax)node).Identifier.ValueText;
				case SyntaxKind.IdentifierName:
				return ((IdentifierNameSyntax)node).Identifier.ValueText;
				case SyntaxKind.InterfaceDeclaration:
					       var interfaceDecl = (InterfaceDeclarationSyntax)node;
					name = interfaceDecl.Identifier.ValueText;
					typeParameterList = interfaceDecl.TypeParameterList;
				break;
				case SyntaxKind.MethodDeclaration:
					       var methodDecl = (MethodDeclarationSyntax)node;
					name = methodDecl.Identifier.ValueText;
					typeParameterList = methodDecl.TypeParameterList;
				break;
				case SyntaxKind.NamespaceDeclaration:
				return GetNodeName(((NamespaceDeclarationSyntax)node).Name, includeTypeParameters: false);
				case SyntaxKind.QualifiedName:
					       var qualified = (QualifiedNameSyntax)node;
				return GetNodeName(qualified.Left, includeTypeParameters: false) + "." + GetNodeName(qualified.Right, includeTypeParameters: false);
				case SyntaxKind.StructDeclaration:
					       var structDecl = (StructDeclarationSyntax)node;
					name = structDecl.Identifier.ValueText;
					typeParameterList = structDecl.TypeParameterList;
				break;
				default:
					Debug.Assert(false, "Unexpected node type " + node.Kind());
				return null;
			}

			return name + (includeTypeParameters ? ExpandTypeParameterList(typeParameterList) : "");
		}

		private static string ExpandTypeParameterList(TypeParameterListSyntax typeParameterList)
		{
			if (typeParameterList != null && typeParameterList.Parameters.Count > 0)
			{
				var builder = new StringBuilder();
				builder.Append('<');
				builder.Append(typeParameterList.Parameters[0].Identifier.ValueText);
				for (int i = 1; i < typeParameterList.Parameters.Count; i++)
				{
					builder.Append(',');
					builder.Append(typeParameterList.Parameters[i].Identifier.ValueText);
				}

				builder.Append('>');
				return builder.ToString();
			}
			else
			{
				return null;
			}
		}

		private static string ExpandExplicitInterfaceName(string identifier, ExplicitInterfaceSpecifierSyntax explicitInterfaceSpecifier)
		{
			if (explicitInterfaceSpecifier == null)
			{
				return identifier;
			}
			else
			{
				var builder = new StringBuilder();
				ExpandTypeName(explicitInterfaceSpecifier.Name, builder);
				builder.Append('.');
				builder.Append(identifier);
				return builder.ToString();
			}
		}

		private static void ExpandTypeName(TypeSyntax type, StringBuilder builder)
		{
			switch (type.Kind())
			{
				case SyntaxKind.AliasQualifiedName:
					       var alias = (AliasQualifiedNameSyntax)type;
					builder.Append(alias.Alias.Identifier.ValueText);
				break;
				case SyntaxKind.ArrayType:
					       var array = (ArrayTypeSyntax)type;
					ExpandTypeName(array.ElementType, builder);
					for (int i = 0; i < array.RankSpecifiers.Count; i++)
					{
						var rankSpecifier = array.RankSpecifiers[i];
						builder.Append(rankSpecifier.OpenBracketToken.Text);
						for (int j = 1; j < rankSpecifier.Sizes.Count; j++)
						{
							builder.Append(',');
						}

						builder.Append(rankSpecifier.CloseBracketToken.Text);
					}

				break;
				case SyntaxKind.GenericName:
					       var generic = (GenericNameSyntax)type;
					builder.Append(generic.Identifier.ValueText);
					if (generic.TypeArgumentList != null)
					{
						var arguments = generic.TypeArgumentList.Arguments;
						builder.Append(generic.TypeArgumentList.LessThanToken.Text);
						for (int i = 0; i < arguments.Count; i++)
						{
							if (i != 0)
							{
								builder.Append(',');
							}

							ExpandTypeName(arguments[i], builder);
						}

						builder.Append(generic.TypeArgumentList.GreaterThanToken.Text);
					}

				break;
				case SyntaxKind.IdentifierName:
					       var identifierName = (IdentifierNameSyntax)type;
					builder.Append(identifierName.Identifier.ValueText);
				break;
				case SyntaxKind.NullableType:
					       var nullable = (NullableTypeSyntax)type;
					ExpandTypeName(nullable.ElementType, builder);
					builder.Append(nullable.QuestionToken.Text);
				break;
				case SyntaxKind.OmittedTypeArgument:
					       // do nothing since it was omitted, but don't reach the default block
				break;
				case SyntaxKind.PointerType:
					       var pointer = (PointerTypeSyntax)type;
					ExpandTypeName(pointer.ElementType, builder);
					builder.Append(pointer.AsteriskToken.Text);
				break;
				case SyntaxKind.PredefinedType:
					       var predefined = (PredefinedTypeSyntax)type;
					builder.Append(predefined.Keyword.Text);
				break;
				case SyntaxKind.QualifiedName:
					       var qualified = (QualifiedNameSyntax)type;
					ExpandTypeName(qualified.Left, builder);
					builder.Append(qualified.DotToken.Text);
					ExpandTypeName(qualified.Right, builder);
				break;
				default:
					Debug.Assert(false, "Unexpected type syntax " + type.Kind());
				break;
			}
		}
	}

	enum DeclaredSymbolInfoKind : byte
	{
		Class,
		Constant,
		Constructor,
		Delegate,
		Enum,
		EnumMember,
		Event,
		Field,
		Indexer,
		Interface,
		Method,
		Module,
		Property,
		Struct
	}

	struct DeclaredSymbolInfo
	{
		internal DocumentId DocumentId;

		public string FilePath { get; }
		public string Name { get; }
//		public string ContainerDisplayName { get; }
		public string FullyQualifiedContainerName { get; }
		public DeclaredSymbolInfoKind Kind { get; }
		public TextSpan Span { get; }
		public ushort ParameterCount { get; }
		public ushort TypeParameterCount { get; }


		public DeclaredSymbolInfo(SyntaxNode node, string name, string fullyQualifiedContainerName, DeclaredSymbolInfoKind kind, TextSpan span, ushort parameterCount = 0, ushort typeParameterCount = 0)
			: this()
		{
			this.FilePath = node.SyntaxTree.FilePath;
			Name = string.Intern (name);
//			ContainerDisplayName = string.Intern (containerDisplayName);
			FullyQualifiedContainerName = fullyQualifiedContainerName;
			Kind = kind;
			Span = span;
			ParameterCount = parameterCount;
			TypeParameterCount = typeParameterCount;
		}

		public async Task<ISymbol> GetSymbolAsync(Document document, CancellationToken cancellationToken)
		{
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
			var node = root.FindNode(Span);
			var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
			var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken);
			return symbol;
		}
	}

	class DeclaredSymbolInfoResult : SearchResult
	{
		bool useFullName;

		DeclaredSymbolInfo type;

		public override SearchResultType SearchResultType { get { return SearchResultType.Type; } }

		public override string File {
			get { return type.FilePath; }
		}

		public override Xwt.Drawing.Image Icon {
			get {
				return ImageService.GetIcon (type.GetStockIconForSymbolInfo(), IconSize.Menu);
			}
		}

		public override int Offset {
			get { return type.Span.Start; }
		}

		public override int Length {
			get { return type.Span.Length; }
		}

		public override string PlainText {
			get {
				return type.Name;
			}
		}
		Document GetDocument (CancellationToken token)
		{
			var doc = type.DocumentId;
			if (doc == null) {
				var docId = TypeSystemService.GetDocuments (type.FilePath).FirstOrDefault ();
				if (docId == null)
					return null;
				return TypeSystemService.GetCodeAnalysisDocument (docId, token);
			}
			return TypeSystemService.GetCodeAnalysisDocument (type.DocumentId, token);
		}

		public override Task<TooltipInformation> GetTooltipInformation (CancellationToken token)
		{
			return Task.Run (async delegate {
				var doc = GetDocument (token);
				if (doc == null) {
					return null;
				}
				var symbol = await type.GetSymbolAsync (doc, token);
				return await Ambience.GetTooltip (token, symbol);
			});
		}

		public override string Description {
			get {
				string loc;
				//				if (type.TryGetSourceProject (out project)) {
				//					loc = GettextCatalog.GetString ("project {0}", project.Name);
				//				} else {
				loc = GettextCatalog.GetString ("file {0}", File);
				//				}

				switch (type.Kind) {
					case DeclaredSymbolInfoKind.Interface:
					return GettextCatalog.GetString ("interface ({0})", loc);
					case DeclaredSymbolInfoKind.Struct:
					return GettextCatalog.GetString ("struct ({0})", loc);
					case DeclaredSymbolInfoKind.Delegate:
					return GettextCatalog.GetString ("delegate ({0})", loc);
					case DeclaredSymbolInfoKind.Enum:
					return GettextCatalog.GetString ("enumeration ({0})", loc);
					case DeclaredSymbolInfoKind.Class:
					return GettextCatalog.GetString ("class ({0})", loc);

					case DeclaredSymbolInfoKind.Field:
					return GettextCatalog.GetString ("field ({0})", loc);
					case DeclaredSymbolInfoKind.Property:
					return GettextCatalog.GetString ("property ({0})", loc);
					case DeclaredSymbolInfoKind.Indexer:
					return GettextCatalog.GetString ("indexer ({0})", loc);
					case DeclaredSymbolInfoKind.Event:
					return GettextCatalog.GetString ("event ({0})", loc);
					case DeclaredSymbolInfoKind.Method:
					return GettextCatalog.GetString ("method ({0})", loc);
				}
				return GettextCatalog.GetString ("symbol ({0})", loc);
			}
		}

		public override string GetMarkupText ()
		{
			return HighlightMatch (useFullName ? type.FullyQualifiedContainerName : type.Name, match);
		}

		public DeclaredSymbolInfoResult (string match, string matchedString, int rank, DeclaredSymbolInfo type, bool useFullName)  : base (match, matchedString, rank)
		{
			this.useFullName = useFullName;
			this.type = type;
		}

		public override bool CanActivate {
			get {
				var doc = GetDocument (default (CancellationToken));
				return doc != null;
			}
		}

		public override async void Activate ()
		{
			var token = default (CancellationToken);
			var doc = GetDocument (token);
			if (doc != null) {
				var symbol = await type.GetSymbolAsync (doc, token);
				var project = TypeSystemService.GetMonoProject (doc.Id);
				IdeApp.ProjectOperations.JumpToDeclaration (symbol, project);
			}
		}
	}
}
