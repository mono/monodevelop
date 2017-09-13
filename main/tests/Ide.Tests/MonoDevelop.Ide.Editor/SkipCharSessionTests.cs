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
using NUnit.Framework;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using Gtk;
using Mono.TextEditor;
using Gdk;
using System.Reflection;
using MonoDevelop.SourceEditor;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class SkipCharSessionTests : IdeTestBase
	{
		[TestCase]
		public void TestBug58764 ()
		{
			var tww = new TestWorkbenchWindow ();
			var content = new TestViewContent ();
			tww.ViewContent = content;

			var document = new Document (tww);

			var editor = TextEditorFactory.CreateNewEditor (document);
			editor.MimeType = "text/xml";
			const string originalText = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentPage xmlns=""http://xamarin.com/schemas/2014/forms""
             xmlns:x=""http://schemas.microsoft.com/winfx/2009/xaml""
             x:Class=""XamlSamples.HelloXamlPage"">
             <Grid HeightRequest=
</ContentPage>";
			editor.Text = originalText;
			var offset = editor.Text.IndexOf ("HeightRequest=", StringComparison.Ordinal) + "HeightRequest=".Length;
			editor.GetContent<ITextEditorImpl> ().CaretOffset = offset;
			//Reason why we use GetNativeWidget, and navigate to ExtensibleTextEditor child
			//and execute KeyPress on it instead something more abstract is...
			//EditSession key processing is done inside ExtensibleTextEditor.
			var extensibleEditor = FindChild<ExtensibleTextEditor> (editor.GetNativeWidget<Container> ());
			extensibleEditor.OnIMProcessedKeyPressEvent ((Gdk.Key)'"', '"', Gdk.ModifierType.None);
			extensibleEditor.OnIMProcessedKeyPressEvent (Gdk.Key.BackSpace, '\0', Gdk.ModifierType.None);
			extensibleEditor.OnIMProcessedKeyPressEvent ((Gdk.Key)'"', '"', Gdk.ModifierType.None);
			Assert.AreEqual (originalText.Insert (offset, "\"\""), editor.Text);
		}

		private T FindChild<T> (Container container) where T : Widget
		{
			foreach (var child in container.Children)
				if (child is T) {
					return (T)child;
				} else if (child is Container) {
					var foundChild = FindChild<T> ((Container)child);
					if (foundChild != null)
						return foundChild;
				}
			return default (T);
		}
	}
}
