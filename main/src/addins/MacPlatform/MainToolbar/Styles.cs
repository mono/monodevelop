//
// Styles.cs
//
// Author:
//       Vsevolod Kukol <sevo@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)
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
using Stetic;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	public static class Styles
	{
		public static Color BaseBackgroundColor { get; private set; }
		public static Color BaseForegroundColor { get; private set; }

		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed +=  (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			//BaseBackgroundColor = Ide.Gui.Styles.BaseBackgroundColor;
			//BaseForegroundColor = Ide.Gui.Styles.BaseForegroundColor;
			BaseBackgroundColor = new Color (0, 0, 1);
			BaseForegroundColor = new Color (0, 1, 0);

			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light) {
			} else {

			}
		}
	}
}

