﻿//
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
using System.Windows.Media;
using System.Windows;

namespace WindowsPlatform
{
	public static class Styles
	{
		public static Brush MainToolbarBackgroundBrush { get; private set; }
		public static Brush MainToolbarForegroundBrush { get; private set; }
		public static Brush MainToolbarDisabledForegroundBrush { get; private set; }
		public static Brush MainToolbarShadowBrush { get; private set; }
		public static Brush MainToolbarSeparatorBrush { get; private set; }
		public static Brush MainToolbarButtonPressedBackgroundBrush { get; private set; }
		public static Brush MainToolbarButtonPressedBorderBrush { get; private set; }

		public static Brush MenuBarBackgroundBrush { get; private set; }
		public static Brush MenuBarForegroundBrush { get; private set; }
		public static Brush MenuBarBorderBrush { get; private set; }
		public static Brush MenuBackgroundBrush { get; private set; }
		public static Brush MenuForegroundBrush { get; private set; }
		public static Brush MenuBorderBrush { get; private set; }
		public static Brush MenuHighlightBackgroundBrush { get; private set; }
		public static Brush MenuHighlightBorderBrush { get; private set; }
		public static Brush MenuSelectedBackgroundBrush { get; private set; }
		public static Brush MenuSelectedBorderBrush { get; private set; }
		public static Brush MenuDisabledForegroundBrush { get; private set; }
		public static Brush MenuSeparatorBrush { get; private set; }

		public static Brush StatusBarBackgroundBrush { get; private set; }
		public static Brush StatusBarTextBrush { get; private set; }
		public static Brush StatusBarErrorTextBrush { get; private set; }
		public static Brush StatusBarWarningTextBrush { get; private set; }
		public static Brush StatusBarReadyTextBrush { get; private set; }
		public static Brush StatusBarProgressBorderBrush { get; private set; }
		public static Brush StatusBarProgressBackgroundBrush { get; private set; }
		public static Brush SearchBarBorderBrush { get; private set; }
		public static Brush SearchBarBackgroundBrush { get; private set; }
		public static Brush SearchBarTextBrush { get; private set; }
		
		static Styles ()
		{
			MainToolbarBackgroundBrush = Brushes.Transparent;
			MainToolbarForegroundBrush = new SolidColorBrush (new Color { A = 0xFF, R = 0x22, G = 0x22, B = 0x22 });
			MainToolbarDisabledForegroundBrush = Brushes.Gray;
			MainToolbarShadowBrush = Brushes.Gray;
			MainToolbarSeparatorBrush = new SolidColorBrush (new Color { A = 0xFF, R = 0x7d, G = 0x7d, B = 0x7d });
			MainToolbarButtonPressedBackgroundBrush = new SolidColorBrush (new Color { A = 0x66, R = 0x00, G = 0x8B, B = 0xFF });
			MainToolbarButtonPressedBorderBrush = new SolidColorBrush (new Color { A = 0x66, R = 0x00, G = 0x8B, B = 0xFF });

			MenuBarBackgroundBrush = SystemColors.MenuBarBrush;
			MenuBarForegroundBrush = SystemColors.MenuTextBrush;
			MenuBarBorderBrush = new SolidColorBrush (new Color { A = 0xFF, R = 0x99, G = 0x99, B = 0x99 });
			MenuBackgroundBrush = Brushes.White;
			MenuForegroundBrush = SystemColors.MenuTextBrush;
			MenuBorderBrush = new SolidColorBrush (new Color { A = 0xFF, R = 0x99, G = 0x99, B = 0x99 });
			MenuSeparatorBrush = new SolidColorBrush (new Color { A = 0xFF, R = 0xD7, G = 0xD7, B = 0xD7 });
			MenuHighlightBackgroundBrush = new SolidColorBrush (new Color { A = 0x66, R = 0x00, G = 0x8B, B = 0xFF });
			MenuHighlightBorderBrush = new SolidColorBrush (new Color { A = 0x66, R = 0x00, G = 0x8B, B = 0xFF });
			MenuSelectedBackgroundBrush = new SolidColorBrush (new Color { A = 0x66, R = 0x00, G = 0x8B, B = 0xFF });
			MenuSelectedBorderBrush = new SolidColorBrush (new Color { A = 0x66, R = 0x00, G = 0x8B, B = 0xFF });
			MenuDisabledForegroundBrush = new SolidColorBrush (new Color { A = 0xFF, R = 0x70, G = 0x70, B = 0x70 });

			StatusBarBackgroundBrush = new SolidColorBrush (new Color { A = 0xFF, R = 0xE5, G = 0xE5, B = 0xE5 });
			StatusBarTextBrush = MainToolbarForegroundBrush;
			StatusBarErrorTextBrush = StatusBarTextBrush;
			StatusBarWarningTextBrush = StatusBarTextBrush;
			StatusBarReadyTextBrush = Brushes.Gray;
			StatusBarProgressBorderBrush = new SolidColorBrush (new Color { A = 0xFF, R = 0xD9, G = 0xDC, B = 0xE1 });
			StatusBarProgressBackgroundBrush = new SolidColorBrush (new Color { A = 0xFF, R = 0xB3, G = 0xE7, B = 0x70 });
			SearchBarBorderBrush = Brushes.LightGray;
			SearchBarBackgroundBrush = Brushes.White;
			SearchBarTextBrush = MainToolbarForegroundBrush;
		}
	}
}

