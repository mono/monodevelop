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
using MonoDevelop.TypeSystem;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.CSharp.Completion;
using System.Linq;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using System.Threading;

namespace MonoDevelop.CSharp
{
	public class PathedDocumentTextEditorExtension : TextEditorExtension, IPathedDocument
	{
		public override void Dispose ()
		{
			Document.Editor.Caret.PositionChanged -= UpdatePath;
			base.Dispose ();
		}
		
		public override void Initialize ()
		{
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
			object tag;
			Ambience amb;
			List<IEntity> memberList = new List<IEntity> ();
			Document Document {
				get;
				set;
			}
			
			public DataProvider (Document doc, object tag, Ambience amb)
			{
				this.Document = doc;
				this.tag = tag;
				this.amb = amb;
				Reset ();
			}
			
			#region IListDataProvider implementation
			public void Reset ()
			{
				stringCache.Clear ();
				memberList.Clear ();
				if (tag is IParsedFile) {
					memberList.AddRange (this.Document.Compilation.GetAllTypeDefinitions ().Where (t => t.Region.FileName == this.Document.FileName && !t.IsSynthetic));
				} else if (tag is ITypeDefinition) {
					var type = (ITypeDefinition)tag;
					memberList.AddRange (type.NestedTypes.Where (t => t.Region.FileName == this.Document.FileName && !t.IsSynthetic));
					memberList.AddRange (type.Members.Where (t => t.Region.FileName == this.Document.FileName && !t.IsSynthetic));
				} else if (tag is IMember) {
					var member = (IMember)tag;
					if (member.DeclaringTypeDefinition != null) {
						memberList.AddRange (member.DeclaringTypeDefinition.NestedTypes.Where (t => t.Region.FileName == this.Document.FileName && !t.IsSynthetic));
						memberList.AddRange (member.DeclaringTypeDefinition.Members.Where (t => t.Region.FileName == this.Document.FileName && !t.IsSynthetic));
					}
				}
				memberList.Sort ((x, y) => String.Compare (x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
			
			Dictionary<IEntity, string> stringCache = new Dictionary<IEntity, string> ();
			string GetString (Ambience amb, IEntity rx)
			{
				string result;
				if (stringCache.TryGetValue(rx, out result))
					return result;
				if (tag is IParsedFile) {
					result = amb.GetString (rx, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName | OutputFlags.UseFullInnerTypeName | OutputFlags.ReformatDelegates);
				} else {
					result = amb.GetString (rx, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName | OutputFlags.ReformatDelegates);
				}
				
				stringCache[rx] = result;
				return result;
			}
			
			public string GetMarkup (int n)
			{
				var m = memberList[n];
				if (m.IsObsolete ())
					return "<s>" + GLib.Markup.EscapeText (GetString (amb, m)) + "</s>";
				return GLib.Markup.EscapeText (GetString (amb, m));
			}
			
			public Gdk.Pixbuf GetIcon (int n)
			{
				return ImageService.GetPixbuf (memberList[n].GetStockIcon (), Gtk.IconSize.Menu);
			}
			
			public object GetTag (int n)
			{
				return memberList[n];
			}
			
			public void ActivateItem (int n)
			{
				var member = memberList[n];
				MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor extEditor = Document.GetContent<MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor> ();
				if (extEditor != null)
					extEditor.SetCaretTo (Math.Max (1, member.Region.BeginLine), Math.Max (1, member.Region.BeginColumn));
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
			var window = new DropDownBoxListWindow (tag == null ? (DropDownBoxListWindow.IListDataProvider)new CompilationUnitDataProvider (Document) : new DataProvider (Document, tag, GetAmbience ()));
			window.SelectItem (path [index].Tag);
			return window;
		}
		
		public PathEntry[] CurrentPath {
			get;
			private set;
		}
		
		PathEntry GetRegionEntry (ParsedDocument unit, Mono.TextEditor.DocumentLocation loc)
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
		IUnresolvedTypeDefinition lastType;
		IUnresolvedMember lastMember = new DefaultUnresolvedField ();
		
		void UpdatePath (object sender, Mono.TextEditor.DocumentLocationEventArgs e)
		{
			var unit = Document.ParsedDocument;
			if (unit == null || unit.ParsedFile == null)
				return;
			
			var offset = Document.Editor.Caret.Offset;
			var loc = Document.Editor.Caret.Location;
			var ext = Document.GetContent<CSharpCompletionTextEditorExtension> ();
			
			var unresolvedType = ext.GetTypeAt (offset);
			var unresolvedMember = ext.GetMemberAt (offset);
			if (unresolvedType == lastType && lastMember == unresolvedMember)
				return;
			lastType = unresolvedType;
			lastMember = unresolvedMember;
			
			if (unresolvedType == null) {
				if (CurrentPath != null && CurrentPath.Length == 1 && CurrentPath [0].Tag is IParsedFile)
					return;
				var prevPath = CurrentPath;
				CurrentPath = new PathEntry[] { new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit.ParsedFile } };
				OnPathChanged (new DocumentPathChangedEventArgs (prevPath));	
				return;
			}
			
			//	ThreadPool.QueueUserWorkItem (delegate {
			var result = new List<PathEntry> ();
			var amb = GetAmbience ();
			var resolveCtx = unit.GetTypeResolveContext (document.Compilation, loc);
			ITypeDefinition typeDef;
			try {
				var resolved = unresolvedType.Resolve (resolveCtx);
				if (resolved == null) {
					ClearPath ();
					return;
				}
				typeDef = resolved.GetDefinition ();
			} catch (Exception) {
				ClearPath ();
				return;
			}
			if (typeDef != null) {
				var curType = typeDef;
				while (curType != null) {
					var flags = OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName | OutputFlags.ReformatDelegates | OutputFlags.IncludeMarkup;
					if (curType.DeclaringTypeDefinition == null)
						flags |= OutputFlags.UseFullInnerTypeName;
					var markup = amb.GetString ((IEntity)curType, flags);
					result.Insert (0, new PathEntry (ImageService.GetPixbuf (curType.GetStockIcon (), Gtk.IconSize.Menu), curType.IsObsolete () ? "<s>" + markup + "</s>" : markup) { Tag = (object)curType.DeclaringTypeDefinition ?? unit.ParsedFile });
					curType = curType.DeclaringTypeDefinition;
				}
			}
			IMember member = null;
			if (ext.typeSystemSegmentTree != null) {
				try {
					if (unresolvedMember != null)
						member = unresolvedMember.CreateResolved (resolveCtx);
				} catch (Exception) {
					ClearPath ();
					return;
				}
			} else {
				member = typeDef != null && typeDef.Kind != TypeKind.Delegate ? typeDef.Members.FirstOrDefault (m => !m.IsSynthetic && m.Region.FileName == document.FileName && m.Region.IsInside (loc)) : null;
			}
				
			if (member != null) {
				var markup = amb.GetString (member, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName | OutputFlags.ReformatDelegates | OutputFlags.IncludeMarkup);
				result.Add (new PathEntry (ImageService.GetPixbuf (member.GetStockIcon (), Gtk.IconSize.Menu), member.IsObsolete () ? "<s>" + markup + "</s>" : markup) { Tag = member });
			}
				
			var entry = GetRegionEntry (unit, loc);
			if (entry != null)
				result.Add (entry);
				
			PathEntry noSelection = null;
			if (typeDef == null) {
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit.ParsedFile };
			} else if (member == null && typeDef.Kind != TypeKind.Delegate) 
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = typeDef };
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
