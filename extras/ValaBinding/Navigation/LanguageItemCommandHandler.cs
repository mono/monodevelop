//
// LanguageItemCommandHandler.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
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
using System.IO;

using Mono.Addins;

using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;

using MonoDevelop.ValaBinding.Parser;
using MonoDevelop.ValaBinding.Parser.Afrodite;

namespace MonoDevelop.ValaBinding.Navigation
{
    public class LanguageItemCommandHandler : NodeCommandHandler
    {
        /// <summary>
        /// Jump to a node's declaration when it's activated
        /// </summary>
        public override void ActivateItem()
        {
            Symbol item = (Symbol)CurrentNode.DataItem;

            if (null != item && 0 < item.SourceReferences.Count)
            {
                SourceReference reference = item.SourceReferences[0];

                IdeApp.Workbench.OpenDocument(reference.File, reference.FirstLine, reference.FirstColumn, Ide.Gui.OpenDocumentOptions.Default);
            }
        }
    }
}
