//
// ClassNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Text;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.TypeSystem;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class ClassNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof (ClassData); }
		}

		public override Type CommandHandlerType {
			get { return typeof (ClassNodeCommandHandler); }
		}

		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Class"; }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ClassData)dataObject).Class.GetFullName ();
		}

		//Same as MonoDevelop.Ide.TypeSystem.Ambience.NameFormat except SymbolDisplayTypeQualificationStyle is NameOnly
		static readonly SymbolDisplayFormat NameFormat =
			new SymbolDisplayFormat(
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
			ClassData classData = dataObject as ClassData;
			nodeInfo.Label = Ambience.EscapeText (classData.Class.ToDisplayString (NameFormat));
			nodeInfo.Icon = Context.GetIcon (classData.Class.GetStockIcon ());
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			ClassData classData = dataObject as ClassData;
			bool publicOnly = builder.Options ["PublicApiOnly"];
			bool publicProtectedOnly = builder.Options ["PublicProtectedApiOnly"];
			publicOnly |= publicProtectedOnly;

			// Delegates have an Invoke method, which doesn't need to be shown.
			if (classData.Class.TypeKind == TypeKind.Delegate)
				return;

			builder.AddChildren (classData.Class.GetTypeMembers ()
								 .Where (innerClass => innerClass.DeclaredAccessibility == Accessibility.Public ||
													   (innerClass.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
													   !publicOnly)
								 .Where (c => !c.IsImplicitClass)
								 .Select (innerClass => new ClassData (classData.Project, innerClass)));

			builder.AddChildren (classData.Class.GetMembers ().OfType<IMethodSymbol> ().Where (m => m.MethodKind != MethodKind.PropertyGet && m.MethodKind != MethodKind.PropertySet)
								 .Where (method => method.DeclaredAccessibility == Accessibility.Public ||
												   (method.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
												   !publicOnly)
								 .Where (m => !m.IsImplicitlyDeclared));

			builder.AddChildren (classData.Class.GetMembers ().OfType<IPropertySymbol> ()
								 .Where (property => property.DeclaredAccessibility == Accessibility.Public ||
										 (property.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
			                             !publicOnly)
								 .Where (m => !m.IsImplicitlyDeclared));

			builder.AddChildren (classData.Class.GetMembers ().OfType<IFieldSymbol> ()
								 .Where (field => field.DeclaredAccessibility == Accessibility.Public ||
										 (field.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
										 !publicOnly)
								 .Where (m => !m.IsImplicitlyDeclared));

			builder.AddChildren (classData.Class.GetMembers ().OfType<IEventSymbol> ()
								 .Where (e => e.DeclaredAccessibility == Accessibility.Public ||
										 (e.DeclaredAccessibility == Accessibility.Protected && publicProtectedOnly) ||
										 !publicOnly)
								 .Where (m => !m.IsImplicitlyDeclared));
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			// Checking if a class has member is expensive since it requires loading the whole
			// info from the db, so we always return true here. After all 99% of classes will have members
			return true;
		}
	}

	public class ClassNodeCommandHandler : NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			ClassData cls = CurrentNode.DataItem as ClassData;
			IdeApp.ProjectOperations.JumpToDeclaration (cls.Class, cls.Project);
		}
	}
}
