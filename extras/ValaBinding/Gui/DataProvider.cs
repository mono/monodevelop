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
using ICSharpCode.NRefactory.PatternMatching;
using Xwt.Drawing;

namespace MonoDevelop.ValaBinding
{
    // Yoinked from C# binding
    public class DataProvider : DropDownBoxListWindow.IListDataProvider
    {
        object tag;
        Ambience amb;
        List<IUnresolvedEntity> memberList = new List<IUnresolvedEntity>();

        Document Document { get; set; }

        public DataProvider(Document doc, object tag, Ambience amb)
        {
            this.Document = doc;
            this.tag = tag;
            this.amb = amb;
            Reset();
        }

        #region IListDataProvider implementation
        public void Reset()
        {
            memberList.Clear();
            if (tag is IUnresolvedFile)
            {
                Stack<IUnresolvedTypeDefinition> types = new Stack<IUnresolvedTypeDefinition>(((IUnresolvedFile)tag).TopLevelTypeDefinitions);
                while (types.Count > 0)
                {
                    IUnresolvedTypeDefinition type = types.Pop();
                    memberList.Add(type);
                    foreach (IUnresolvedTypeDefinition innerType in type.NestedTypes)
                        types.Push(innerType);
                }
            }
            else if (tag is IUnresolvedTypeDefinition)
            {
                memberList.AddRange(((IUnresolvedTypeDefinition)tag).Members);
            }
            memberList.Sort(delegate(IUnresolvedEntity x, IUnresolvedEntity y)
            {
                return String.Compare(GetString(amb, x), GetString(amb, y), StringComparison.OrdinalIgnoreCase);
            });
        }

        string GetString(Ambience amb, IUnresolvedEntity x)
        {
            SimpleTypeResolveContext simpleTypeResolveContext = new SimpleTypeResolveContext(Document.Compilation.MainAssembly);
            IEntity entity = null;
            if (x is IUnresolvedMember)
            {
                entity = ((IUnresolvedMember)x).CreateResolved(simpleTypeResolveContext);
            }
            if (tag is IUnresolvedEntity)
            {
                return amb.GetString(entity, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.UseFullInnerTypeName | OutputFlags.ReformatDelegates);
            }
            return amb.GetString(entity, OutputFlags.IncludeGenerics | OutputFlags.IncludeParameters | OutputFlags.ReformatDelegates);
        }

        public string GetMarkup(int n)
        {
            IUnresolvedEntity x = this.memberList[n];
            return GLib.Markup.EscapeText(this.GetString(this.amb, x));
        }

        public Xwt.Drawing.Image GetIcon(int n)
        {
            return ImageService.GetIcon(memberList[n].GetStockIcon(), Gtk.IconSize.Menu);
        }

        public object GetTag(int n)
        {
            return memberList[n];
        }

        public void ActivateItem(int n)
        {
            IUnresolvedEntity unresolvedEntity = this.memberList[n];
            IExtensibleTextEditor content = this.Document.GetContent<IExtensibleTextEditor>();
            if (content != null)
            {
                content.SetCaretTo(Math.Max(1, unresolvedEntity.Region.BeginLine), Math.Max(1, unresolvedEntity.Region.BeginColumn));
            }
        }

        public int IconCount
        {
            get
            {
                return memberList.Count;
            }
        }
        #endregion
    }
}