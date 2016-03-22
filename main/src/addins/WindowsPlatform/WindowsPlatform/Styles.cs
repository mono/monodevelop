//
// Styles.cs
//
// Author:
//       Vsevolod Kukol <sevo@sevo.org>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using MonoDevelop.Ide;

namespace WindowsPlatform
{
	public static class Styles
	{
		static Brush mainToolbarBackgroundBrush;
		static Brush mainToolbarForegroundBrush;
		static Brush mainToolbarDisabledForegroundBrush;
		static Brush mainToolbarShadowBrush;
		static Brush mainToolbarSeparatorBrush;
		static Brush mainToolbarButtonPressedBackgroundBrush;
		static Brush mainToolbarButtonPressedBorderBrush;
		static Brush menuBarBackgroundBrush;
		static Brush menuBarForegroundBrush;
		static Brush menuBarBorderBrush;
		static Brush menuBarHighlightBackgroundBrush;
		static Brush menuBarHighlightBorderBrush;

        static Brush menuBackgroundBrush;
		static Brush menuForegroundBrush;
		static Brush menuBorderBrush;
		static Brush menuHighlightBackgroundBrush;
		static Brush menuHighlightBorderBrush;
		static Brush menuSelectedBackgroundBrush;
		static Brush menuSelectedBorderBrush;
		static Brush menuDisabledForegroundBrush;
		static Brush menuSeparatorBrush;

		static Brush statusBarBackgroundBrush;
		static Brush statusBarTextBrush;
		static Brush statusBarErrorTextBrush;
		static Brush statusBarWarningTextBrush;
		static Brush statusBarReadyTextBrush;
		static Brush statusBarProgressBorderBrush;
		static Brush statusBarProgressBackgroundBrush;
		static Brush searchBarBorderBrush;
		static Brush searchBarBackgroundBrush;
		static Brush searchBarTextBrush;

		public static Brush MainToolbarBackgroundBrush {
			get { return mainToolbarBackgroundBrush; }
			private set { mainToolbarBackgroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MainToolbarForegroundBrush {
			get { return mainToolbarForegroundBrush; }
			private set { mainToolbarForegroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MainToolbarDisabledForegroundBrush {
			get { return mainToolbarDisabledForegroundBrush; }
			private set { mainToolbarDisabledForegroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MainToolbarShadowBrush {
			get { return mainToolbarShadowBrush; }
			private set { mainToolbarShadowBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MainToolbarSeparatorBrush {
			get { return mainToolbarSeparatorBrush; }
			private set { mainToolbarSeparatorBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MainToolbarButtonPressedBackgroundBrush {
			get { return mainToolbarButtonPressedBackgroundBrush; }
			set { mainToolbarButtonPressedBackgroundBrush = value; RaisePropertyChanged (); }
		}


		public static Brush MainToolbarButtonPressedBorderBrush {
			get { return mainToolbarButtonPressedBorderBrush; }
			set { mainToolbarButtonPressedBorderBrush = value; RaisePropertyChanged (); }
		}
		
		public static Brush MenuBarBackgroundBrush {
			get { return menuBarBackgroundBrush; }
			private set { menuBarBackgroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuBarForegroundBrush {
			get { return menuBarForegroundBrush; }
			private set { menuBarForegroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuBarBorderBrush {
			get { return menuBarBorderBrush; }
			private set { menuBarBorderBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuBarHighlightBackgroundBrush {
			get { return menuBarHighlightBackgroundBrush; }
			private set { menuBarHighlightBackgroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuBarHighlightBorderBrush {
			get { return menuBarHighlightBorderBrush; }
			private set { menuBarHighlightBorderBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuBackgroundBrush {
			get { return menuBackgroundBrush; }
			private set { menuBackgroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuForegroundBrush {
			get { return menuForegroundBrush; }
			private set { menuForegroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuBorderBrush {
			get { return menuBorderBrush; }
			private set { menuBorderBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuHighlightBackgroundBrush {
			get { return menuHighlightBackgroundBrush; }
			private set { menuHighlightBackgroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuHighlightBorderBrush {
			get { return menuHighlightBorderBrush; }
			private set { menuHighlightBorderBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuSelectedBackgroundBrush {
			get { return menuSelectedBackgroundBrush; }
			private set { menuSelectedBackgroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuSelectedBorderBrush {
			get { return menuSelectedBorderBrush; }
			private set { menuSelectedBorderBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuDisabledForegroundBrush {
			get { return menuDisabledForegroundBrush; }
			private set { menuDisabledForegroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush MenuSeparatorBrush {
			get { return menuSeparatorBrush; }
			set { menuSeparatorBrush = value; RaisePropertyChanged (); }
		}

		public static Brush StatusBarBackgroundBrush {
			get { return statusBarBackgroundBrush; }
			private set { statusBarBackgroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush StatusBarTextBrush {
			get { return statusBarTextBrush; }
			private set { statusBarTextBrush = value; RaisePropertyChanged (); }
		}

		public static Brush StatusBarErrorTextBrush {
			get { return statusBarErrorTextBrush; }
			private set { statusBarErrorTextBrush = value; RaisePropertyChanged (); }
		}

		public static Brush StatusBarWarningTextBrush {
			get { return statusBarWarningTextBrush; }
			private set { statusBarWarningTextBrush = value; RaisePropertyChanged (); }
		}

		public static Brush StatusBarReadyTextBrush {
			get { return statusBarReadyTextBrush; }
			private set { statusBarReadyTextBrush = value; RaisePropertyChanged (); }
		}

		public static Brush StatusBarProgressBorderBrush {
			get { return statusBarProgressBorderBrush; }
			private set { statusBarProgressBorderBrush = value; RaisePropertyChanged (); }
		}

		public static Brush StatusBarProgressBackgroundBrush {
			get { return statusBarProgressBackgroundBrush; }
			private set { statusBarProgressBackgroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush SearchBarBorderBrush {
			get { return searchBarBorderBrush; }
			private set { searchBarBorderBrush = value; RaisePropertyChanged (); }
		}

		public static Brush SearchBarBackgroundBrush {
			get { return searchBarBackgroundBrush; }
			private set { searchBarBackgroundBrush = value; RaisePropertyChanged (); }
		}

		public static Brush SearchBarTextBrush {
			get { return searchBarTextBrush; }
			private set { searchBarTextBrush = value; RaisePropertyChanged (); }
		}

		static Styles ()
		{
			Xwt.Drawing.Context.RegisterStyles ("hover", "pressed", "disabled");
			LoadStyles ();
			MonoDevelop.Ide.Gui.Styles.Changed += (o, e) => LoadStyles ();
		}

		public static Color ColorFromHex (string s, double alpha = 1.0)
		{
			if (s.StartsWith ("#", StringComparison.Ordinal))
				s = s.Substring (1);
			if (s.Length == 3)
				s = "" + s[0]+s[0]+s[1]+s[1]+s[2]+s[2];
			byte r = byte.Parse (s.Substring (0,2), System.Globalization.NumberStyles.HexNumber);
			byte g = byte.Parse (s.Substring (2,2), System.Globalization.NumberStyles.HexNumber);
			byte b = byte.Parse (s.Substring (4,2), System.Globalization.NumberStyles.HexNumber);
			byte a = (byte)(alpha * 255d);
			return new Color { R = r, G = g, B = b, A = a };
		}

		public static Color WithAlpha (this Color color, double alpha)
		{
			color.A = (byte)(alpha * 255d);
			return color;
		}

		public static void LoadStyles ()
		{
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Light) {
				MainToolbarBackgroundBrush = new SolidColorBrush (ColorFromHex("FFFFFF", 0));
				MainToolbarForegroundBrush = new SolidColorBrush (ColorFromHex("222222"));
				MainToolbarDisabledForegroundBrush = new SolidColorBrush (ColorFromHex("808080"));
				MainToolbarShadowBrush = new SolidColorBrush (ColorFromHex("808080"));
				MainToolbarSeparatorBrush = new SolidColorBrush (ColorFromHex("7D7D7D"));
				MainToolbarButtonPressedBackgroundBrush = new SolidColorBrush (ColorFromHex("008BFF", 0.4));
				MainToolbarButtonPressedBorderBrush = new SolidColorBrush (ColorFromHex("008BFF", 0.4));

				MenuBarBackgroundBrush = SystemColors.MenuBarBrush;
				MenuBarForegroundBrush = SystemColors.MenuTextBrush;
				MenuBarBorderBrush = new SolidColorBrush (ColorFromHex("999999"));
				MenuBarHighlightBackgroundBrush = new SolidColorBrush (ColorFromHex("C3E3FE"));
				MenuBarHighlightBorderBrush = new SolidColorBrush (ColorFromHex("C3E3FE"));

				MenuBackgroundBrush = new SolidColorBrush (ColorFromHex("FFFFFF"));
				MenuForegroundBrush = new SolidColorBrush (ColorFromHex("000000"));
				MenuBorderBrush = new SolidColorBrush (ColorFromHex("999999"));
				MenuSeparatorBrush = new SolidColorBrush (ColorFromHex("EAEAEA"));
				MenuHighlightBackgroundBrush = new SolidColorBrush (ColorFromHex("008BFF", 0.2));
				MenuHighlightBorderBrush = new SolidColorBrush (ColorFromHex("008BFF", 0.2));
				MenuSelectedBackgroundBrush = new SolidColorBrush (ColorFromHex("008BFF", 0.2));
				MenuSelectedBorderBrush = new SolidColorBrush (ColorFromHex("008BFF", 0.2));
				MenuDisabledForegroundBrush = new SolidColorBrush (ColorFromHex("A0A0A0"));

				StatusBarBackgroundBrush = new SolidColorBrush (ColorFromHex("E5E5E5"));
				StatusBarTextBrush = MainToolbarForegroundBrush;
				StatusBarErrorTextBrush = StatusBarTextBrush;
				StatusBarWarningTextBrush = StatusBarTextBrush;
				StatusBarReadyTextBrush = new SolidColorBrush (ColorFromHex("808080"));
				StatusBarProgressBorderBrush = new SolidColorBrush (ColorFromHex("D9DCE1"));
				StatusBarProgressBackgroundBrush = new SolidColorBrush (ColorFromHex("B3E770"));
				SearchBarBorderBrush = new SolidColorBrush (ColorFromHex("D3D3D3"));
				SearchBarBackgroundBrush = new SolidColorBrush (ColorFromHex("FFFFFF"));
				SearchBarTextBrush = MainToolbarForegroundBrush;
			} else {
				MainToolbarBackgroundBrush = new SolidColorBrush (ColorFromHex("303030"));
				MainToolbarForegroundBrush = new SolidColorBrush (ColorFromHex("bfbfbf"));
				MainToolbarDisabledForegroundBrush = new SolidColorBrush (ColorFromHex("808080"));
				MainToolbarShadowBrush = new SolidColorBrush (ColorFromHex("747474"));
				MainToolbarSeparatorBrush = new SolidColorBrush (ColorFromHex("7D7D7D"));
				MainToolbarButtonPressedBackgroundBrush = new SolidColorBrush (ColorFromHex("008BFF", 0.4));
				MainToolbarButtonPressedBorderBrush = new SolidColorBrush (ColorFromHex("008BFF", 0.4));

				MenuBarBackgroundBrush = MainToolbarBackgroundBrush;
				MenuBarForegroundBrush = MainToolbarForegroundBrush;
				MenuBarBorderBrush = new SolidColorBrush (ColorFromHex("5D5D5D"));
				MenuBarHighlightBackgroundBrush = new SolidColorBrush (ColorFromHex("8ECAFF", 0.3));
				MenuBarHighlightBorderBrush = new SolidColorBrush (ColorFromHex("8ECAFF", 0.3));

				MenuBackgroundBrush = MainToolbarBackgroundBrush;
				MenuForegroundBrush = MainToolbarForegroundBrush;
				MenuBorderBrush = new SolidColorBrush (ColorFromHex("5D5D5D"));
				MenuSeparatorBrush = new SolidColorBrush (ColorFromHex("444444"));
				MenuHighlightBackgroundBrush = new SolidColorBrush (ColorFromHex("8ECAFF", 0.3));
				MenuHighlightBorderBrush = new SolidColorBrush (ColorFromHex("8ECAFF", 0.3));
				MenuSelectedBackgroundBrush = MenuHighlightBackgroundBrush;
				MenuSelectedBorderBrush = MenuHighlightBorderBrush;
				MenuDisabledForegroundBrush = new SolidColorBrush (ColorFromHex("707070"));

				StatusBarBackgroundBrush = new SolidColorBrush (ColorFromHex("3D3D3D"));
				StatusBarTextBrush = MainToolbarForegroundBrush;
				StatusBarErrorTextBrush = StatusBarTextBrush;
				StatusBarWarningTextBrush = StatusBarTextBrush;
				StatusBarReadyTextBrush = new SolidColorBrush (ColorFromHex("D3D3D3"));
				StatusBarProgressBorderBrush = new SolidColorBrush (ColorFromHex("444444"));
				StatusBarProgressBackgroundBrush = new SolidColorBrush (ColorFromHex("516833"));
				SearchBarBorderBrush = new SolidColorBrush (ColorFromHex("1A1A1A"));
				SearchBarBackgroundBrush = new SolidColorBrush (ColorFromHex("222222"));
				SearchBarTextBrush = MainToolbarForegroundBrush;
			}
		}

		public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;

		static void RaisePropertyChanged ([CallerMemberName] string propName = null)
		{
			if (StaticPropertyChanged != null)
				StaticPropertyChanged (null, new PropertyChangedEventArgs (propName));
		}
	}
}

