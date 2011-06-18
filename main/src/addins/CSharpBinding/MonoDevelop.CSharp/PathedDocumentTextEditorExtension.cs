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
				memberList.Clear ();
				Console.WriteLine ("tag:" + tag);
				if (tag is IParsedFile) {
					var types = new Stack<ITypeDefinition> (((IParsedFile)tag).TopLevelTypeDefinitions);
					while (types.Count > 0) {
						var type = types.Pop ();
						memberList.Add (type);
						foreach (var innerType in type.NestedTypes)
							types.Push (innerType);
					}
				} else if (tag is ITypeDefinition) {
					memberList.AddRange (((ITypeDefinition)tag).Members);
				}
				memberList.Sort ((x, y) => String.Compare (GetString (amb, x), GetString (amb, y), StringComparison.OrdinalIgnoreCase));
			}
			
			string GetString (Ambience amb, IEntity x)
			{
				if (tag is IParsedFile)
					return amb.GetString (x, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.UseFullInnerTypeName | OutputFlags.ReformatDelegates);
				return amb.GetString (x, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates);
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
			if (unit == null)
				return;
			
			var loc = Document.Editor.Caret.Location;
			
			var result = new List<PathEntry> ();
			var amb = GetAmbience ();
			
			var type = unit.GetTypeDefinition (loc.Line, loc.Column);
			var curType = type;
			object lastTag = unit;
			while (curType != null) {
				var markup = amb.GetString (curType, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates | OutputFlags.IncludeMarkup);
				result.Insert (0, new PathEntry (ImageService.GetPixbuf (type.GetStockIcon (), Gtk.IconSize.Menu), curType.IsObsolete () ? "<s>" + markup + "</s>" : markup) { Tag = lastTag });
				lastTag = curType;
				curType = curType.DeclaringTypeDefinition;
			}
			
			var member = type != null && !type.IsDelegate () ? unit.GetMember (loc.Line, loc.Column) : null;
			if (member != null) {
				var markup = amb.GetString (member, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates | OutputFlags.IncludeMarkup);
				result.Add (new PathEntry (ImageService.GetPixbuf (member.GetStockIcon (), Gtk.IconSize.Menu), member.IsObsolete () ? "<s>" + markup + "</s>" : markup) { Tag = lastTag });
			}
			
			var entry = GetRegionEntry (unit, loc);
			if (entry != null)
				result.Add (entry);
			
			PathEntry noSelection = null;
			if (type == null) {
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = unit };
			} else if (member == null && !type.IsDelegate ()) 
				noSelection = new PathEntry (GettextCatalog.GetString ("No selection")) { Tag = type };
			if (noSelection != null) 
				result.Add (noSelection);
			var prev = CurrentPath;
			CurrentPath = result.ToArray ();
			OnPathChanged (new DocumentPathChangedEventArgs (prev));
		}
		#endregion
	}
}
