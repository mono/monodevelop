//
// DocumentFactory.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Core.Text;
using Mono.Addins;

namespace MonoDevelop.Ide.Editor
{
	public interface ITextEditorFactory
	{
		ITextDocument CreateNewDocument ();
		ITextDocument CreateNewDocument (ITextSource textSource, string fileName, string mimeType);

		IReadonlyTextDocument CreateNewReadonlyDocument (ITextSource textSource, string fileName, string mimeType);

		ITextEditorImpl CreateNewEditor ();
		ITextEditorImpl CreateNewEditor (IReadonlyTextDocument document);

		string[] GetSyntaxProperties (string mimeType, string name);
	}
	/*
			SyntaxMode mode = null;
			foreach (string mt in DesktopService.GetMimeTypeInheritanceChain (loadedMimeType)) {
				mode = SyntaxModeService.GetSyntaxMode (null, mt);
				if (!Equals (mode, null))
					break;
			}
			
			if (mode == null)
				return null;
			
			List<string> ctags;
			if (mode.Properties.TryGetValue ("LineComment", out ctags) && ctags.Count > 0) {
				return new string [] { ctags [0] };
			}
			List<string> tags = new List<string> ();
			if (mode.Properties.TryGetValue ("BlockCommentStart", out ctags))
				tags.Add (ctags [0]);
			if (mode.Properties.TryGetValue ("BlockCommentEnd", out ctags))
				tags.Add (ctags [0]);
			if (tags.Count == 2)
				return tags.ToArray ();
			else
				return null;

*/
	public static class DocumentFactory
	{
		static ITextEditorFactory currentFactory;

		static DocumentFactory ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/SourceEditor2/EditorFactory", delegate(object sender, ExtensionNodeEventArgs args) {
				switch (args.Change) {
				case ExtensionChange.Add:
					if (currentFactory == null)
						currentFactory = (ITextEditorFactory)args.ExtensionObject;
					break;
				}
			});
		}

		public static ITextDocument CreateNewDocument ()
		{
			return currentFactory.CreateNewDocument ();
		}

		public static ITextDocument CreateNewDocument (ITextSource textSource, string fileName, string mimeType = null)
		{
			return currentFactory.CreateNewDocument (textSource, fileName, mimeType); 
		}

		public static ITextDocument LoadDocument (string fileName, string mimeType)
		{
			return currentFactory.CreateNewDocument (StringTextSource.ReadFrom (fileName), fileName, mimeType); 
		}

		public static IReadonlyTextDocument CreateNewReadonlyDocument (ITextSource textSource, string fileName, string mimeType = null)
		{
			return currentFactory.CreateNewDocument (textSource, fileName, mimeType); 
		}

		public static TextEditor CreateNewEditor ()
		{
			return new TextEditor (currentFactory.CreateNewEditor ());
		}

		public static TextEditor CreateNewEditor (IReadonlyTextDocument document)
		{
			return new TextEditor (currentFactory.CreateNewEditor (document));
		}

		public static string[] GetSyntaxProperties (string mimeType, string name)
		{
			return currentFactory.GetSyntaxProperties (mimeType, name);
		}
	}
}