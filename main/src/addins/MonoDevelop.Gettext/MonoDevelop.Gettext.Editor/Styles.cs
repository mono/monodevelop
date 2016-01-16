//
// Styles.cs
//
// Author:
//       Vsevolod Kukol <sevo@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://www.xamarin.com)
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
using MonoDevelop.Ide;
using Xwt.Drawing;

namespace MonoDevelop.Gettext
{
	public static class Styles
	{
		public static POEditorStyle POEditor { get; internal set; }

		public class POEditorStyle
		{
			public Gdk.Color EntryUntranslatedBackgroundColor { get; internal set; }
			public Gdk.Color EntryMissingBackgroundColor { get; internal set; }
			public Gdk.Color EntryFuzzyBackgroundColor { get; internal set; }
			public Gdk.Color TabBarBackgroundColor { get; internal set; }
		}

		static Styles ()
		{
			LoadStyles ();
			MonoDevelop.Ide.Gui.Styles.Changed +=  (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			POEditor = new POEditorStyle ();
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light) {
				POEditor.EntryUntranslatedBackgroundColor = new Gdk.Color (234, 232, 227);
				POEditor.EntryMissingBackgroundColor = new Gdk.Color (237, 226, 187);
				POEditor.EntryFuzzyBackgroundColor = new Gdk.Color (255, 199, 186);
				POEditor.TabBarBackgroundColor = new Gdk.Color (241, 241, 241);
			} else {
				POEditor.EntryUntranslatedBackgroundColor = new Gdk.Color (255, 238, 194);
				POEditor.EntryMissingBackgroundColor = new Gdk.Color (255, 0, 255); // TODO: VV
				POEditor.EntryFuzzyBackgroundColor = new Gdk.Color (255, 195, 183);
				POEditor.TabBarBackgroundColor = new Gdk.Color (51, 51, 51);
			}
		}
	}
}

