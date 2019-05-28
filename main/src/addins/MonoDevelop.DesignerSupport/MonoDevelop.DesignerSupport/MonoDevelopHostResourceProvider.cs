
/* 
 * MonoDevelopHostResourceProvider.cs: The pad that holds the MD property grid. Can also 
 * hold custom grid widgets.
 *
 * Copyright (C) 2019 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#if MAC
using AppKit;
using MonoDevelop.Components;
using MonoDevelop.Ide;
using Xamarin.PropertyEditing.Mac;
using static Xamarin.PropertyEditing.Mac.NamedResources;

namespace MonoDevelop.DesignerSupport
{
	internal class MonoDevelopHostResourceProvider : HostResourceProvider
	{
		public MonoDevelopHostResourceProvider ()
		{
			CurrentAppearance = NSAppearance.GetAppearance (IdeApp.Preferences.UserInterfaceTheme == Theme.Dark ? NSAppearance.NameDarkAqua : NSAppearance.NameAqua);
		}

		public override NSColor GetNamedColor (string name)
		{
			switch (name) {
			case Checkerboard0Color:
				return Styles.PropertyPad.Checkerboard0;
			case Checkerboard1Color:
				return Styles.PropertyPad.Checkerboard1;
			case DescriptionLabelColor:
				return NSColor.SecondaryLabelColor;
			case ForegroundColor:
				return (HslColor)Ide.Gui.Styles.BaseForegroundColor;
			case PadBackgroundColor:
				return (HslColor)Ide.Gui.Styles.PadBackground;
			case PanelTabBackground:
				return Styles.PropertyPad.PanelTabBackground;
			case TabBorderColor:
				return Styles.PropertyPad.TabBorderColor;
			case ValueBlockBackgroundColor:
				return Styles.PropertyPad.ValueBlockBackgroundColor;
			}
			return base.GetNamedColor (name);
		}
	}
}
#endif