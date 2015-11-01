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
using System.Windows.Media;

namespace WindowsPlatform
{
	public static class Styles
	{
		public static Brush MainToolbarBackgroundBrush { get; private set; }
		public static Brush MainToolbarForegroundBrush { get; private set; }
		public static Brush MainToolbarShadowBrush { get; private set; }
		public static Brush MainMenuBackgroundBrush { get; private set; }
		public static Brush StatusBarBackgroundBrush { get; private set; }
		public static Brush StatusBarTextBrush { get; private set; }
		public static Brush StatusBarErrorTextBrush { get; private set; }
		public static Brush StatusBarWarningTextBrush { get; private set; }
		public static Brush StatusBarReadyTextBrush { get; private set; }
		public static Brush SearchBarBorderBrush { get; private set; }
		public static Brush SearchBarBackgroundBrush { get; private set; }
		public static Brush SearchBarTextBrush { get; private set; }
		
		static Styles ()
		{
			MainToolbarBackgroundBrush = Brushes.Transparent;
			MainToolbarForegroundBrush = Brushes.Black;
			MainToolbarShadowBrush = Brushes.Gray;
			MainMenuBackgroundBrush = Brushes.Transparent;
			StatusBarBackgroundBrush = Brushes.LightGray;
			StatusBarTextBrush = MainToolbarForegroundBrush;
			StatusBarErrorTextBrush = Brushes.Red;
			StatusBarWarningTextBrush = Brushes.Orange;
			StatusBarReadyTextBrush = Brushes.Gray;
			SearchBarBorderBrush = Brushes.LightGray;
			SearchBarBackgroundBrush = Brushes.White;
			SearchBarTextBrush = MainToolbarForegroundBrush;
		}
	}
}

