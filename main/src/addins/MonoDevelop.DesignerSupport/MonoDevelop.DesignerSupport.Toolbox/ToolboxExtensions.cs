//
// Copyright (C) 2018 Microsoft Corp
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

using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	/// <summary>
	/// Helpers for working with toolbox interfaces
	/// </summary>
	public static class ToolboxExtensions
	{
		/// <summary>
		/// If the consumer is a ViewContent, get content from it by type
		/// </summary>
		public static T GetContent<T> (this IToolboxConsumer consumer) where T : class
		{
			return (consumer as ViewContent)?.GetContent<T> ();
		}

		/// <summary>
		/// If the consumer is a ViewContent, returns its parent Document
		/// </summary>
		public static Document GetDocument (this IToolboxConsumer consumer)
		{
			return (consumer as ViewContent)?.WorkbenchWindow?.Document;
		}

		/// <summary>
		/// Returns true if the consumer is a text editor and can handle text toolbox nodes
		/// </summary>
		public static bool IsTextEditor (this IToolboxConsumer consumer, out TextEditor editor)
		{
			editor = consumer.DefaultItemDomain == "Text" ? GetDocument (consumer)?.Editor : null;
			return editor != null;
		}
	}
}
