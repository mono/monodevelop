// 
//  DataProvider.cs
//  
//  Author:
//       Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com>
// 
//  Copyright (c) 2010 Levi Bard
// 
// This source code is licenced under The MIT License:
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
using System.Collections.Generic;

using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Components;

using Gtk;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.Editor;

namespace CBinding.Parser
{
	// Yoinked from C# binding
	public class DataProvider : DropDownBoxListWindow.IListDataProvider
	{
		object tag;
		Ambience amb;
		List<IUnresolvedEntity> memberList = new List<IUnresolvedEntity> ();

		TextEditor editor;
		
		EditContext EditContext {
			get;
			set;
		}
		
		public DataProvider (TextEditor editor, EditContext editContext, object tag, Ambience amb)
		{
			this.editor = editor;
			this.EditContext = editContext;
			this.tag = tag;
			this.amb = amb;
			Reset ();
		}
		
		#region IListDataProvider implementation
		public void Reset ()
		{
			memberList.Clear ();
			if (tag is IUnresolvedFile) {
				var types = new Stack<IUnresolvedTypeDefinition> (((IUnresolvedFile)tag).TopLevelTypeDefinitions);
				while (types.Count > 0) {
					var type = types.Pop ();
					memberList.Add (type);
					foreach (var innerType in type.NestedTypes)
						types.Push (innerType);
				}
			} else if (tag is IUnresolvedTypeDefinition) {
				memberList.AddRange (((IUnresolvedTypeDefinition)tag).Members);
			}
			memberList.Sort ((x, y) => String.Compare (GetString (amb, x), GetString (amb, y), StringComparison.OrdinalIgnoreCase));
		}
		
		string GetString (Ambience amb, IUnresolvedEntity x)
		{
			var ctx = new SimpleTypeResolveContext (EditContext.Compilation.MainAssembly);
			IEntity rx = null;
			if (x is IUnresolvedMember)
				rx = ((IUnresolvedMember)x).CreateResolved (ctx);
			
			if (tag is IUnresolvedFile)
				return amb.GetString (rx, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.UseFullInnerTypeName | OutputFlags.ReformatDelegates);
			return amb.GetString (rx, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates);
		}
		
		public string GetMarkup (int n)
		{
			var m = memberList[n];
//			if (m.IsObsolete ())
//				return "<s>" + GLib.Markup.EscapeText (GetString (amb, m)) + "</s>";
			return GLib.Markup.EscapeText (GetString (amb, m));
		}
		
		public Xwt.Drawing.Image GetIcon (int n)
		{
			return ImageService.GetIcon (memberList[n].GetStockIcon (), Gtk.IconSize.Menu);
		}
		
		public object GetTag (int n)
		{
			return memberList[n];
		}
		
		public void ActivateItem (int n)
		{
			var member = memberList[n];
			var extEditor = editor;
			if (extEditor != null) {
				extEditor.SetCaretLocation (Math.Max (1, member.Region.BeginLine), Math.Max (1, member.Region.BeginColumn), true);
			}
		}
		
		public int IconCount {
			get {
				return memberList.Count;
			}
		}
		#endregion
	}
}

