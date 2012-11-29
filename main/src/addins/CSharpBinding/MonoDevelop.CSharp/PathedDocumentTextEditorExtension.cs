// 
// PathedDocumentTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Completion;
using System.Linq;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp;
using System.Text;

namespace MonoDevelop.CSharp
{
	public class PathedDocumentTextEditorExtension : TextEditorExtension, IPathedDocument
	{
		public override void Dispose ()
		{
			Document.Editor.Caret.PositionChanged -= UpdatePath;
			base.Dispose ();
		}

		bool isPathSet;
		
		public override void Initialize ()
		{
			CurrentPath = new PathEntry[] { new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = null } };
			isPathSet = false;
			UpdatePath (null, null);
			Document.Editor.Caret.PositionChanged += UpdatePath;
			var ext = Document.GetContent<CSharpCompletionTextEditorExtension> ();
			ext.TypeSegmentTreeUpdated += (o, s) => UpdatePath (null, null);
		}

		#region IPathedDocument implementation
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;

		protected virtual void OnPathChanged (DocumentPathChangedEventArgs e)
		{
			EventHandler<DocumentPathChangedEventArgs> handler = this.PathChanged;
			if (handler != null)
				handler (this, e);
		}
		
		class DataProvider : DropDownBoxListWindow.IListDataProvider
		{
			readonly PathedDocumentTextEditorExtension ext;
			object tag;
			List<AstNode> memberList = new List<AstNode> ();

			public DataProvider (PathedDocumentTextEditorExtension ext, object tag)
			{
				if (ext == null)
					throw new ArgumentNullException ("ext");
				this.ext = ext;
				this.tag = tag;
				Reset ();
			}
			
			#region IListDataProvider implementation
			public void Reset ()
			{
				memberList.Clear ();
				if (tag is SyntaxTree) {
					var unit = tag as SyntaxTree;
					memberList.AddRange (unit.GetTypes (true));
				} else if (tag is TypeDeclaration) {
					var type = (TypeDeclaration)tag;
					foreach (var member in type.Members) {
						if (member is FieldDeclaration) {
							foreach (var variable in ((FieldDeclaration)member).Variables)
								memberList.Add (variable);
						} else if (member is EventDeclaration) {
							foreach (var variable in ((EventDeclaration)member).Variables)
								memberList.Add (variable);
						} else {
							memberList.Add (member);
						}
					}
				} else if (tag is Accessor) {
					var acc = (Accessor)tag;
					var parent = (EntityDeclaration)acc.Parent;
					memberList.AddRange (parent.Children.OfType<Accessor> ());
				} else if (tag is EntityDeclaration) {
					var entity = (EntityDeclaration)tag;
					var type = (TypeDeclaration)entity.Parent;
					foreach (var member in type.Members) {
						if (member is FieldDeclaration) {
							foreach (var variable in ((FieldDeclaration)member).Variables)
								memberList.Add (variable);
						} else {
							memberList.Add (member);
						}
					}
				} 

				memberList.Sort ((x, y) => {
					var result = String.Compare (GetName (x), GetName(y), StringComparison.OrdinalIgnoreCase);
					if (result == 0)
						result = GetTypeParameters(x).CompareTo (GetTypeParameters(y));
					if (result == 0)
						result = GetParameters(x).CompareTo (GetParameters(y));

					// partial methods without body should come last
					if (result == 0 && x is MethodDeclaration && y is MethodDeclaration) {
						var mx = x as MethodDeclaration;
						var my = y as MethodDeclaration;
						if (mx.Body.IsNull && !my.Body.IsNull)
							return 1;
						if (!mx.Body.IsNull && my.Body.IsNull)
							return -1;
					}
					return result;
				});
			}

			static int GetTypeParameters (AstNode x)
			{
				return x.GetChildrenByRole (Roles.TypeParameter).Count ();
			}

			static int GetParameters (AstNode x)
			{
				return x.GetChildrenByRole (Roles.Parameter).Count ();
			}

			string GetName (AstNode node)
			{
				if (tag is SyntaxTree) {
					var type = node as TypeDeclaration;
					if (type != null) {
						var sb = new StringBuilder ();
						sb.Append (type.Name);
						while (type.Parent is TypeDeclaration) {
							type = type.Parent as TypeDeclaration;
							sb.Insert (0, type.Name + ".");
						}
						return sb.ToString ();
					}
				}
				
				if (node is Accessor) {
					if (node.Role == PropertyDeclaration.GetterRole)
						return "get";
					if (node.Role == PropertyDeclaration.SetterRole)
						return "set";
					if (node.Role == CustomEventDeclaration.AddAccessorRole) 
						return "add";
					if (node.Role == CustomEventDeclaration.RemoveAccessorRole)
						return "remove";
					return node.GetText ();
				}
				if (node is EntityDeclaration)
					return ((EntityDeclaration)node).Name;
				return ((VariableInitializer)node).Name;
			}

			public string GetMarkup (int n)
			{
				var node = memberList [n];
				if (tag is SyntaxTree) {
					var type = node;
					if (type != null) {
						var sb = new StringBuilder ();
						sb.Append (ext.GetEntityMarkup (type));
						while (type.Parent is TypeDeclaration) {
							sb.Insert (0, ext.GetEntityMarkup (type.Parent) + ".");
							type = type.Parent;
						}
						return sb.ToString ();
					}
				}
				return ext.GetEntityMarkup (node);
			}
			
			public Gdk.Pixbuf GetIcon (int n)
			{
				string icon;
				var node = memberList [n];
				if (node is EntityDeclaration) {
					icon = ((EntityDeclaration)node).GetStockIcon (false);
				} else {
					icon = ((EntityDeclaration)node.Parent).GetStockIcon (false);
				}
				return ImageService.GetPixbuf (icon, Gtk.IconSize.Menu);
			}
			
			public object GetTag (int n)
			{
				return memberList[n];
			}
			
			public void ActivateItem (int n)
			{
				var node = memberList [n];
				var extEditor = ext.Document.GetContent<IExtensibleTextEditor> ();
				if (extEditor != null) {
					int line, col;
					if (node is OperatorDeclaration) { 
						line = Math.Max (1, ((OperatorDeclaration)node).OperatorToken.StartLocation.Line);
						col = Math.Max (1, ((OperatorDeclaration)node).OperatorToken.StartLocation.Column);
					} else if (node is EntityDeclaration && !(node is Accessor)) {
						line = Math.Max (1, ((EntityDeclaration)node).NameToken.StartLocation.Line);
						col = Math.Max (1, ((EntityDeclaration)node).NameToken.StartLocation.Column);
					} else {
						line = node.StartLocation.Line;
						col = node.StartLocation.Column;
					}
					extEditor.SetCaretTo (line, col);
				}
			}
			
			public int IconCount {
				get {
					return memberList.Count;
				}
			}
			#endregion
		}
		
		class CompilationUnitDataProvider : DropDownBoxListWindow.IListDataProvider
		{
			Document Document {
				get;
				set;
			}
			
			public CompilationUnitDataProvider (Document document)
			{
				this.Document = document;
			}
			
			#region IListDataProvider implementation
			public void Reset ()
			{
			}
			
			public string GetMarkup (int n)
			{
				return GLib.Markup.EscapeText (Document.ParsedDocument.UserRegions.ElementAt (n).Name);
			}
			
			internal static Gdk.Pixbuf Pixbuf {
				get {
					return ImageService.GetPixbuf (Gtk.Stock.Add, Gtk.IconSize.Menu);
				}
			}
			
			public Gdk.Pixbuf GetIcon (int n)
			{
				return Pixbuf;
			}
			
			public object GetTag (int n)
			{
				return Document.ParsedDocument.UserRegions.ElementAt (n);
			}
			
			public void ActivateItem (int n)
			{
				var reg = Document.ParsedDocument.UserRegions.ElementAt (n);
				var extEditor = Document.GetContent<MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor> ();
				if (extEditor != null)
					extEditor.SetCaretTo (Math.Max (1, reg.Region.BeginLine), reg.Region.BeginColumn);
			}
			
			public int IconCount {
				get {
					if (Document.ParsedDocument == null)
						return 0;
					return Document.ParsedDocument.UserRegions.Count ();
				}
			}
			#endregion
		}
		
		public Gtk.Widget CreatePathWidget (int index)
		{
			PathEntry[] path = CurrentPath;
			if (path == null || index < 0 || index >= path.Length)
				return null;
			var tag = path [index].Tag;
			var window = new DropDownBoxListWindow (tag == null ? (DropDownBoxListWindow.IListDataProvider)new CompilationUnitDataProvider (Document) : new DataProvider (this, tag));
			window.SelectItem (path [index].Tag);
			return window;
		}
		
		PathEntry[] currentPath;
		public PathEntry[] CurrentPath {
			get {
				return currentPath;
			}
			private set {
				currentPath = value;
				isPathSet = true;
			}
		}
		
		static PathEntry GetRegionEntry (ParsedDocument unit, Mono.TextEditor.DocumentLocation loc)
		{
			PathEntry entry;
			if (!unit.UserRegions.Any ())
				return null;
			var reg = unit.UserRegions.Where (r => r.Region.IsInside (loc)).LastOrDefault ();
			if (reg == null) {
				entry = new PathEntry (GettextCatalog.GetString ("No region"));
			} else {
				entry = new PathEntry (CompilationUnitDataProvider.Pixbuf,
						                       GLib.Markup.EscapeText (reg.Name));
			}
			entry.Position = EntryPosition.Right;
			return entry;
		}
		
		void ClearPath ()
		{
			var prev = CurrentPath;
			CurrentPath = new PathEntry[0];
			OnPathChanged (new DocumentPathChangedEventArgs (prev));	
		}

		EntityDeclaration lastType;
		string lastTypeMarkup;
		EntityDeclaration lastMember;
		string lastMemberMarkup;
		AstAmbience amb;
		string GetEntityMarkup (AstNode node)
		{
			if (amb == null)
				return "";
			return amb.GetEntityMarkup (node);
		}

		void UpdatePath (object sender, Mono.TextEditor.DocumentLocationEventArgs e)
		{
			var parsedDocument = Document.ParsedDocument;
			if (parsedDocument == null || parsedDocument.ParsedFile == null)
				return;
			amb = new AstAmbience (document.GetFormattingOptions ());
			
			var unit = parsedDocument.GetAst<SyntaxTree> ();
			if (unit == null)
				return;

			var loc = Document.Editor.Caret.Location;

			var curType = (EntityDeclaration)unit.GetNodeAt (loc, n => n is TypeDeclaration || n is DelegateDeclaration);
			var curMember = unit.GetNodeAt<EntityDeclaration> (loc);
			if (curType == curMember)
				curMember = null;
			if (isPathSet && curType == lastType && lastMember == curMember)
				return;

			var curTypeMakeup = GetEntityMarkup (curType);
			var curMemberMarkup = GetEntityMarkup (curMember);
			if (isPathSet && curType != null && lastType != null && curType.StartLocation == lastType.StartLocation && curTypeMakeup == lastTypeMarkup &&
			    curMember != null && lastMember != null && curMember.StartLocation == lastMember.StartLocation && curMemberMarkup == lastMemberMarkup)
				return;

			lastType = curType;
			lastTypeMarkup = curTypeMakeup;

			lastMember = curMember;
			lastMemberMarkup = curMemberMarkup;

			if (curType == null) {
				if (CurrentPath != null && CurrentPath.Length == 1 && CurrentPath [0].Tag is IUnresolvedFile)
					return;
				var prevPath = CurrentPath;
				CurrentPath = new PathEntry[] { new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit } };
				OnPathChanged (new DocumentPathChangedEventArgs (prevPath));	
				return;
			}
			
			//	ThreadPool.QueueUserWorkItem (delegate {
			var result = new List<PathEntry> ();

			if (curType != null) {
				var type = curType;
				while (type != null) {
					var declaringType = type.Parent as TypeDeclaration;
					result.Insert (0, new PathEntry (ImageService.GetPixbuf (type.GetStockIcon (false), Gtk.IconSize.Menu), GetEntityMarkup (type)) { Tag = (AstNode)declaringType ?? unit });
					type = declaringType;
				}
			}
				
			if (curMember != null) {
				result.Add (new PathEntry (ImageService.GetPixbuf (curMember.GetStockIcon (true), Gtk.IconSize.Menu), curMemberMarkup) { Tag = curMember });
				if (curMember is Accessor) {
					var parent = curMember.Parent as EntityDeclaration;
					if (parent != null)
						result.Insert (result.Count - 1, new PathEntry (ImageService.GetPixbuf (parent.GetStockIcon (true), Gtk.IconSize.Menu), GetEntityMarkup (parent)) { Tag = parent });
				}
			}
				
			var entry = GetRegionEntry (parsedDocument, loc);
			if (entry != null)
				result.Add (entry);
				
			PathEntry noSelection = null;
			if (curType == null) {
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit };
			} else if (curMember == null && !(curType is DelegateDeclaration)) { 
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = curType };
			}

			if (noSelection != null) 
				result.Add (noSelection);

			var prev = CurrentPath;
			if (prev != null && prev.Length == result.Count) {
				bool equals = true;
				for (int i = 0; i < prev.Length; i++) {
					if (prev [i].Markup != result [i].Markup) {
						equals = false;
						break;
					}
				}
				if (equals)
					return;
			}
			//		Gtk.Application.Invoke (delegate {
			CurrentPath = result.ToArray ();
			OnPathChanged (new DocumentPathChangedEventArgs (prev));	
			//		});
			//	});
		}
		#endregion
	}


}
