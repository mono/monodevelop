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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

using Gtk;

namespace CBinding.Parser
{
	// Yoinked from C# binding
	public class DataProvider : DropDownBoxListWindow.IListDataProvider
	{
		object tag;
		Ambience amb;
		List<IMember> memberList = new List<IMember> ();
		
		Document Document { get; set; }
		
		public DataProvider (Document doc, object tag, Ambience amb)
		{
			this.Document = doc;
			this.tag = ((INode)tag).Parent;
			this.amb = amb;
			Reset ();
		}// constructor
		
		#region IListDataProvider implementation
		public void Reset ()
		{
			memberList.Clear ();
			if (tag is ICompilationUnit) {
				Stack<IType> types = new Stack<IType> (((ICompilationUnit)tag).Types);
				while (types.Count > 0) {
					IType type = types.Pop ();
					memberList.Add (type);
					foreach (IType innerType in type.InnerTypes)
						types.Push (innerType);
				}
			} else  if (tag is IType) {
				memberList.AddRange (((IType)tag).Members);
			}
			memberList.Sort ((x, y) => String.Compare (GetString (amb, x), GetString (amb, y), StringComparison.OrdinalIgnoreCase));
		}// Reset
		
		string GetString (Ambience amb, IMember x)
		{
			if (tag is ICompilationUnit)
				return amb.GetString (x, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.UseFullInnerTypeName | OutputFlags.ReformatDelegates);
			return amb.GetString (x, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates);
		}// GetString
		
		public string GetMarkup (int n)
		{
			return GLib.Markup.EscapeText (GetString (amb, memberList[n]));
		}// GetText

		public Gdk.Pixbuf GetIcon (int n)
		{
			return ImageService.GetPixbuf (memberList[n].StockIcon, IconSize.Menu);
		}// GetIcon

		public object GetTag (int n)
		{
			return memberList[n];
		}// GetTag

		public void ActivateItem (int n)
		{
			var member = memberList[n];
			MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor extEditor = Document.GetContent<MonoDevelop.Ide.Gui.Content.IExtensibleTextEditor> ();
			if (extEditor != null)
				extEditor.SetCaretTo (Math.Max (1, member.Location.Line), member.Location.Column);
		}// ActivateItem

		public int IconCount {
			get {
				return memberList.Count;
			}
		}// IconCount
		#endregion
	}// DataProvider
}

