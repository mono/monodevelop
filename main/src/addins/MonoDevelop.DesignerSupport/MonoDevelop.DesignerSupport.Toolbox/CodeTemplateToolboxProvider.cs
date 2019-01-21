// 
// CodeTemplateToolboxProvider.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	
	public class CodeTemplateToolboxProvider : IToolboxDynamicProvider
	{
		static string category = MonoDevelop.Core.GettextCatalog.GetString ("Text Snippets");

		public System.Collections.Generic.IEnumerable<ItemToolboxNode> GetDynamicItems (IToolboxConsumer consumer)
		{
			var content = consumer as ViewContent;
			if (content == null || !content.IsFile)
				yield break;
			// Hack: Ensure that this category is only filled if the current page is a text editor.
			if (!(content is ITextEditorResolver))
				yield break;
			foreach (CodeTemplate ct in CodeTemplateService.GetCodeTemplatesForFile (content.ContentName)) {
				if (ct.CodeTemplateContext != CodeTemplateContext.Standard)
					continue;
				yield return new TemplateToolboxNode (ct) {
					Category = category,
					Icon = ImageService.GetIcon ("md-template", Gtk.IconSize.Menu)
				};
			}
		}

		public event EventHandler ItemsChanged {
			add { CodeTemplateService.TemplatesChanged += value; }
			remove { CodeTemplateService.TemplatesChanged -= value; }
		}
	}
}
