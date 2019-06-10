//
// Copyright (c) Microsoft. All rights reserved.
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.CSharp
{
	sealed partial class CSharpPathedDocumentExtension
	{
		class DataProvider : DropDownBoxListWindow.IListDataProvider
		{
			readonly CSharpPathedDocumentExtension ext;
			object tag;
			List<SyntaxNode> memberList = new List<SyntaxNode> ();

			public DataProvider (CSharpPathedDocumentExtension ext, object tag)
			{
				this.ext = ext ?? throw new ArgumentNullException (nameof (ext));
				this.tag = tag;
				Reset ();
			}

			void AddTypeToMemberList (BaseTypeDeclarationSyntax btype)
			{
				if (btype is EnumDeclarationSyntax e) {
					foreach (var member in e.Members) {
						memberList.Add (member);
					}
					return;
				}
				var type = (TypeDeclarationSyntax)btype;
				foreach (var member in type.Members) {
					if (member is FieldDeclarationSyntax) {
						foreach (var variable in ((FieldDeclarationSyntax)member).Declaration.Variables)
							memberList.Add (variable);
					} else if (member is EventFieldDeclarationSyntax) {
						foreach (var variable in ((EventFieldDeclarationSyntax)member).Declaration.Variables)
							memberList.Add (variable);
					} else {
						memberList.Add (member);
					}
				}
			}

			public void Reset ()
			{
				memberList.Clear ();
				if (tag is SyntaxTree) {
					var unit = tag as SyntaxTree;
					memberList.AddRange (unit.GetRoot ().DescendantNodes ().Where (IsType));
				} else if (tag is AccessorDeclarationSyntax acc) {
					var parent = (MemberDeclarationSyntax)acc.Parent;
					memberList.AddRange (parent.ChildNodes ().OfType<AccessorDeclarationSyntax> ());
				} else if (tag is SyntaxNode entity) {
					var type = entity.AncestorsAndSelf ().OfType<BaseTypeDeclarationSyntax> ().FirstOrDefault ();
					if (type != null) {
						AddTypeToMemberList (type);
					}
				}

				memberList.Sort ((x, y) => {
					var result = string.Compare (GetName (x), GetName (y), StringComparison.OrdinalIgnoreCase);
					if (result == 0)
						result = GetTypeParameters (x).CompareTo (GetTypeParameters (y));
					if (result == 0)
						result = GetParameters (x).CompareTo (GetParameters (y));

					// partial methods without body should come last
					if (result == 0 && x is MethodDeclarationSyntax && y is MethodDeclarationSyntax) {
						var mx = x as MethodDeclarationSyntax;
						var my = y as MethodDeclarationSyntax;
						if (mx.Body == null && my.Body != null)
							return 1;
						if (mx.Body != null && my.Body == null)
							return -1;
					}
					return result;
				});
			}

			static int GetTypeParameters (SyntaxNode x)
			{
				return 0; //x.GetChildrenByRole (Roles.TypeParameter).Count ();
			}

			static int GetParameters (SyntaxNode x)
			{
				return 0; // x.GetChildrenByRole (Roles.Parameter).Count ();
			}

			string GetName (SyntaxNode node)
			{
				if (tag is SyntaxTree) {
					var type = node;
					if (type != null) {
						var sb = StringBuilderCache.Allocate ();
						sb.Append (ext.GetEntityMarkup (type));
						while (type.Parent is BaseTypeDeclarationSyntax) {
							sb.Insert (0, ext.GetEntityMarkup (type.Parent) + ".");
							type = type.Parent;
						}
						return StringBuilderCache.ReturnAndFree (sb);
					}
				}
				if (node is AccessorDeclarationSyntax accessor) {
					if (accessor.Kind () == SyntaxKind.GetAccessorDeclaration)
						return "get";
					if (accessor.Kind () == SyntaxKind.SetAccessorDeclaration)
						return "set";
					if (accessor.Kind () == SyntaxKind.AddAccessorDeclaration)
						return "add";
					if (accessor.Kind () == SyntaxKind.RemoveAccessorDeclaration)
						return "remove";
					return node.ToString ();
				}
				if (node is OperatorDeclarationSyntax)
					return "operator";
				if (node is PropertyDeclarationSyntax)
					return ((PropertyDeclarationSyntax)node).Identifier.ToString ();
				if (node is MethodDeclarationSyntax)
					return ((MethodDeclarationSyntax)node).Identifier.ToString ();
				if (node is ConstructorDeclarationSyntax)
					return ((ConstructorDeclarationSyntax)node).Identifier.ToString ();
				if (node is DestructorDeclarationSyntax)
					return ((DestructorDeclarationSyntax)node).Identifier.ToString ();
				if (node is BaseTypeDeclarationSyntax)
					return ((BaseTypeDeclarationSyntax)node).Identifier.ToString ();

				//				if (node is fixeds) {
				//					return ((FixedVariableInitializer)node).Name;
				//				}
				if (node is VariableDeclaratorSyntax)
					return ((VariableDeclaratorSyntax)node).Identifier.ToString ();
				return node.ToString ();
			}

			public string GetMarkup (int n)
			{
				if (tag is DotNetProject) {
					return GLib.Markup.EscapeText (ext.ownerProjects[n].Name);
				}

				var node = memberList[n];
				if (tag is SyntaxTree) {
					var type = node;
					if (type != null) {
						var sb = StringBuilderCache.Allocate ();
						sb.Append (ext.GetEntityMarkup (type));
						while (type.Parent is BaseTypeDeclarationSyntax) {
							sb.Insert (0, ext.GetEntityMarkup (type.Parent) + ".");
							type = type.Parent;
						}
						return StringBuilderCache.ReturnAndFree (sb);
					}
				}
				return ext.GetEntityMarkup (node);
			}

			public Xwt.Drawing.Image GetIcon (int n)
			{
				string icon;
				if (tag is DotNetProject) {
					icon = ext.ownerProjects[n].StockIcon;
				} else {
					var node = memberList[n];
					if (node is MemberDeclarationSyntax) {
						icon = ((MemberDeclarationSyntax)node).GetStockIcon ();
					} else if (node is VariableDeclaratorSyntax) {
						icon = node.Parent.Parent.GetStockIcon ();
					} else {
						icon = node.Parent.GetStockIcon ();
					}
				}
				return ImageService.GetIcon (icon, Gtk.IconSize.Menu);
			}

			public object GetTag (int n)
			{
				if (tag is DotNetProject) {
					return ext.ownerProjects[n];
				}
				return memberList[n];
			}

			public void ActivateItem (int n)
			{
				if (tag is DotNetProject) {
					var proj = ext.ownerProjects [n];
					foreach (var doc in ext.textContainer.GetRelatedDocuments ())
						if (IdeServices.TypeSystemService.GetMonoProject (doc.Project) is DotNetProject dnp && dnp == proj)
							ext.registration.Workspace.SetDocumentContext (doc.Id);
					ext.WorkspaceChanged (null, null);
					return;
				}

				var node = memberList[n];
				var editor = ext.textView;
				int offset;
				if (node is OperatorDeclarationSyntax) {
					offset = Math.Max (1, ((OperatorDeclarationSyntax)node).OperatorToken.SpanStart);
				} else if (node is MemberDeclarationSyntax && !(node is AccessorDeclarationSyntax)) {
					offset = Math.Max (1, ((MemberDeclarationSyntax)node).SpanStart);
				} else {
					offset = node.SpanStart;
				}

				//FIXME: use the snapshot that the nodes came from
				var point = new VirtualSnapshotPoint (editor.TextBuffer.CurrentSnapshot, offset);
				EditorOperations.SelectAndMoveCaret (point, point, TextSelectionMode.Stream, EnsureSpanVisibleOptions.AlwaysCenter);
			}

			public int IconCount {
				get {
					if (tag is DotNetProject) {
						return ext.ownerProjects.Count;
					}
					return memberList.Count;
				}
			}

			public IEditorOperations EditorOperations { get; internal set; }
		}
	}
}
