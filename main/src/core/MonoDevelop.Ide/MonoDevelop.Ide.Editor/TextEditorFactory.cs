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
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor
{
	public static class TextEditorFactory
	{
		static ITextEditorFactory currentFactory;

		static TextEditorFactory ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/Editor/EditorFactory", delegate(object sender, ExtensionNodeEventArgs args) {
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

		public static ITextDocument CreateNewDocument(string fileName, string mimeType = null)
		{
			if (fileName == null)
				throw new System.ArgumentNullException(nameof(fileName));
			return currentFactory.CreateNewDocument(fileName, mimeType);
		}

		public static ITextDocument CreateNewDocument (ITextSource textSource, string fileName, string mimeType = null)
		{
			if (textSource == null)
				throw new System.ArgumentNullException ("textSource");
			return currentFactory.CreateNewDocument (textSource, fileName, mimeType); 
		}

		public static ITextDocument LoadDocument (string fileName, string mimeType = null)
		{
			if (fileName == null)
				throw new System.ArgumentNullException ("fileName");
			return currentFactory.CreateNewDocument (StringTextSource.ReadFrom (fileName), fileName, mimeType); 
		}

		public static IReadonlyTextDocument CreateNewReadonlyDocument (ITextSource textSource, string fileName, string mimeType = null)
		{
			if (textSource == null)
				throw new System.ArgumentNullException ("textSource");
			return currentFactory.CreateNewDocument (textSource, fileName, mimeType); 
		}

		static ConfigurationProperty<double> zoomLevel = ConfigurationProperty.Create ("Editor.ZoomLevel", 1.0d);

		public static TextEditor CreateNewEditor(string fileName, string mimeType, TextEditorType textEditorType = TextEditorType.Default)
		{
			var result = new TextEditor(currentFactory.CreateNewEditor(fileName, mimeType), textEditorType);
			InitializeTextEditor(result);
			return result;
		}

		public static TextEditor CreateNewEditor(TextEditorType textEditorType = TextEditorType.Default)
		{
			var result = new TextEditor(currentFactory.CreateNewEditor(), textEditorType);
			InitializeTextEditor(result);
			return result;
		}

		private static void InitializeTextEditor(TextEditor textEditor)
		{
			textEditor.ZoomLevel = zoomLevel;

			textEditor.ZoomLevelChanged += delegate {
				zoomLevel.Value = textEditor.ZoomLevel;
			};
		}

		public static TextEditor CreateNewEditor (IReadonlyTextDocument document, TextEditorType textEditorType = TextEditorType.Default)
		{
			if (document == null)
				throw new System.ArgumentNullException ("document");
			var result = new TextEditor (currentFactory.CreateNewEditor (document), textEditorType) {
				ZoomLevel = zoomLevel
			};
			result.ZoomLevelChanged += delegate {
				zoomLevel.Value = result.ZoomLevel;
			};
			return result;
		}

		public static TextEditor CreateNewEditor (DocumentContext ctx, TextEditorType textEditorType = TextEditorType.Default)
		{
			var result = CreateNewEditor (textEditorType);
			result.InitializeExtensionChain (ctx);
			return result;
		}

		public static TextEditor CreateNewEditor (DocumentContext ctx, IReadonlyTextDocument document, TextEditorType textEditorType = TextEditorType.Default)
		{
			var result = CreateNewEditor (document, textEditorType);
			result.InitializeExtensionChain (ctx);
			return result;
		}
	}
}