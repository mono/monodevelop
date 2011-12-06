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

namespace MonoDevelop.CSharp
{
	public class PathedDocumentTextEditorExtension : TextEditorExtension, IPathedDocument
	{
		public override void Initialize ()
		{
			UpdatePath (null, null);
			Document.Editor.Caret.PositionChanged += UpdatePath;
			Document.DocumentParsed += (sender, e) => UpdatePath (null, null);
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
			List<IUnresolvedEntity> memberList = new List<IUnresolvedEntity> ();
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
					var types = new Stack<IUnresolvedTypeDefinition> (((IParsedFile)tag).TopLevelTypeDefinitions);
					while (types.Count > 0) {
						var type = types.Pop ();
						memberList.Add (type);
						foreach (var innerType in type.NestedTypes)
							types.Push (innerType);
					}
				} else if (tag is IUnresolvedTypeDefinition) {
					memberList.AddRange (((IUnresolvedTypeDefinition)tag).NestedTypes);
					memberList.AddRange (((IUnresolvedTypeDefinition)tag).Members);
				}
				memberList.Sort ((x, y) => String.Compare (x.Name, y.Name, StringComparison.OrdinalIgnoreCase));
			}
			
			Dictionary<IUnresolvedEntity, string> stringCache = new Dictionary<IUnresolvedEntity, string> ();
			string GetString (Ambience amb, IUnresolvedEntity x)
			{
				string result;
				if (stringCache.TryGetValue(x, out result))
					return result;
				var pf = Document.ParsedDocument.ParsedFile as CSharpParsedFile;
				var ctx = pf.GetTypeResolveContext (Document.Compilation, x.Region.Begin);
				IEntity rx = null;
				if (x is IUnresolvedMember)
					rx = ((IUnresolvedMember)x).CreateResolved (ctx);
				if (x is IUnresolvedTypeDefinition)
					rx = ((IUnresolvedTypeDefinition)x).Resolve (ctx).GetDefinition ();
				if (rx == null)
					return "unknown:" + x.GetType ();
				if (tag is IParsedFile) {
					result = amb.GetString (rx, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.UseFullInnerTypeName | OutputFlags.ReformatDelegates);
				} else {
					result = amb.GetString (rx, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates);
				}
				
				stringCache[x] = result;
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
			var reg = unit.UserRegions.Where (r => r.Region.IsInside (loc.Line, loc.Column)).LastOrDefault ();
			if (reg == null) {
				entry = new PathEntry (GettextCatalog.GetString ("No region"));
			} else {
				entry = new PathEntry (CompilationUnitDataProvider.Pixbuf,
						                       GLib.Markup.EscapeText (reg.Name));
			}
			entry.Position = EntryPosition.Right;
			return entry;
		}
		
		void UpdatePath (object sender, Mono.TextEditor.DocumentLocationEventArgs e)
		{
			var unit = Document.ParsedDocument;
			if (unit == null || unit.ParsedFile == null)
				return;
			var loc = Document.Editor.Caret.Location;
			var result = new List<PathEntry> ();
			var amb = GetAmbience ();
			var ctx = unit.ParsedFile.GetTypeResolveContext (document.Compilation, loc);
			var typeDef = unit.GetInnermostTypeDefinition (loc);
			IType type = null;
			if (typeDef != null) {
				type = typeDef.Resolve (ctx);
				var curType = type.GetDefinition ();
				while (curType != null) {
					var flags = OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates | OutputFlags.IncludeMarkup;
					if (curType.DeclaringTypeDefinition == null)
						flags |= OutputFlags.UseFullInnerTypeName;
					var markup = amb.GetString ((IEntity)curType, flags);
					result.Insert (0, new PathEntry (ImageService.GetPixbuf (type.GetStockIcon (), Gtk.IconSize.Menu), curType.IsObsolete () ? "<s>" + markup + "</s>" : markup) { Tag = (object)typeDef.DeclaringTypeDefinition ?? unit });
					curType = curType.DeclaringTypeDefinition;
				}
			}
			
			var unresolvedMember = type != null && type.Kind != TypeKind.Delegate ? unit.GetMember (loc.Line, loc.Column) : null;
			if (unresolvedMember != null) {
				var member = unresolvedMember.CreateResolved (ctx);
				var markup = amb.GetString (member, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates | OutputFlags.IncludeMarkup);
				result.Add (new PathEntry (ImageService.GetPixbuf (member.GetStockIcon (), Gtk.IconSize.Menu), member.IsObsolete () ? "<s>" + markup + "</s>" : markup) { Tag = unresolvedMember.DeclaringTypeDefinition });
			}
			
			var entry = GetRegionEntry (unit, loc);
			if (entry != null)
				result.Add (entry);
			
			PathEntry noSelection = null;
			if (type == null) {
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit };
			} else if (unresolvedMember == null && type.Kind != TypeKind.Delegate) 
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = typeDef };
			if (noSelection != null) 
				result.Add (noSelection);
			var prev = CurrentPath;
			if (prev != null && prev.Length == result.Count) {
				bool equals = true;
				for (int i = 0; i < prev.Length; i++) {
					if (prev[i].Markup != result[i].Markup) {
						equals = false;
						break;
					}
				}
				if (equals)
					return;
			}
			CurrentPath = result.ToArray ();
			OnPathChanged (new DocumentPathChangedEventArgs (prev));
		}
		#endregion
	}
}
