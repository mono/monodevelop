// 
// CodeFormatterService.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using Mono.Addins;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.CodeFormatting
{
	[Obsolete ("Use the Microsoft.VisualStudio.Text APIs")]
	public sealed class CodeFormatterService
	{
		static List<CodeFormatterExtensionNode> nodes = new List<CodeFormatterExtensionNode> ();
		
		static CodeFormatterService ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/CodeFormatters", FormatterExtHandler);
		}
		
		static void FormatterExtHandler (object sender, ExtensionNodeEventArgs args)
		{
			switch (args.Change) {
			case ExtensionChange.Add:
				nodes.Add ((CodeFormatterExtensionNode) args.ExtensionNode);
				break;
			case ExtensionChange.Remove:
				nodes.Remove ((CodeFormatterExtensionNode) args.ExtensionNode);
				break;
			}
		}
		
		public static CodeFormatter GetFormatter (string mimeType)
		{
			//find the most specific formatter that can handle the document			var chain = DesktopService.GetMimeTypeInheritanceChain (mimeType);
			foreach (var mt in chain) {
				var node = nodes.FirstOrDefault (f => f.MimeType == mt);
				if (node != null)
					return new CodeFormatter (mimeType, node.GetFormatter ());
			}
			
			if (DesktopService.GetMimeTypeIsText (mimeType))
				return new CodeFormatter (mimeType, new DefaultCodeFormatter ());
			
			return null;
		}

		public static void Format (TextEditor editor, DocumentContext ctx, ISegment segment)
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			if (ctx == null)
				throw new ArgumentNullException ("ctx");
			if (segment == null)
				throw new ArgumentNullException ("segment");
			var fmt = GetFormatter (editor.MimeType);
			if (fmt == null)
				return;
			if (fmt.SupportsOnTheFlyFormatting) {
				fmt.OnTheFlyFormat (editor, ctx, segment);
				return;
			}
			editor.Text = fmt.FormatText (ctx.HasProject ? ctx.Project.Policies : null, editor.Text, segment);
		}
	}
}
