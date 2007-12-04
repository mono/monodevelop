//  Ime.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Shinsaku Nakagawa <shinsaku@users.sourceforge.jp>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// Used internally, not for own use.
	/// </summary>
	/*
	internal class Ime
	{
		public Ime(IntPtr hWnd, Font font)
		{
			hIMEWnd = ImmGetDefaultIMEWnd(hWnd);
			this.font = font;
			SetIMEWindowFont(font);
		}

		private Font font = null;
		public Font Font
		{
			get {
				return font;				
			}
			set {
				if (font.Equals(value) == false) {
					SetIMEWindowFont(value);
					font = value;			
				}
			}
		}
		
		[ DllImport("imm32.dll") ]
		private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);
		
		[ DllImport("user32.dll") ]
		private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, COMPOSITIONFORM lParam);
		[ DllImport("user32.dll") ]
		private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, LOGFONT lParam);
		
		[ StructLayout(LayoutKind.Sequential) ]
		private class COMPOSITIONFORM
		{
			public int dwStyle = 0;
			public POINT ptCurrentPos = null;
			public RECT rcArea = null;
		}
		
		[ StructLayout(LayoutKind.Sequential) ]
		private class POINT
		{
			public int x = 0;
			public int y = 0;
		}
		
		[ StructLayout(LayoutKind.Sequential) ]
		private class RECT
		{
			public int left = 0;
			public int top = 0;
			public int right = 0;
			public int bottom = 0;
		}
		
		private const int WM_IME_CONTROL = 0x0283;
		
		private const int IMC_SETCOMPOSITIONWINDOW = 0x000c;
		private IntPtr hIMEWnd;
		private const int CFS_POINT = 0x0002;
		
		[ StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto) ]
		private class LOGFONT
		{
			public int lfHeight = 0;
			public int lfWidth = 0;
			public int lfEscapement = 0;
			public int lfOrientation = 0;
			public int lfWeight = 0;
			public byte lfItalic = 0;
			public byte lfUnderline = 0;
			public byte lfStrikeOut = 0;
			public byte lfCharSet = 0;
			public byte lfOutPrecision = 0;
			public byte lfClipPrecision = 0;
			public byte lfQuality = 0;
			public byte lfPitchAndFamily = 0;
			[ MarshalAs(UnmanagedType.ByValTStr, SizeConst=32) ] public string lfFaceName = null;
		}
		private const int IMC_SETCOMPOSITIONFONT = 0x000a;
		
		const byte FF_MODERN = 48;
		const byte FIXED_PITCH = 1;
		
		private void SetIMEWindowFont(Font f)
		{
			LOGFONT lf = new LOGFONT();
			f.ToLogFont(lf);
			lf.lfPitchAndFamily = FIXED_PITCH | FF_MODERN;
			
			SendMessage(
			            hIMEWnd,
			            WM_IME_CONTROL,
			            IMC_SETCOMPOSITIONFONT,
			            lf
			            );
		}
		
		
		public void SetIMEWindowLocation(int x, int y)
		{
			
			POINT p = new POINT();
			p.x = x;
			p.y = y;
			
			COMPOSITIONFORM lParam = new COMPOSITIONFORM();
			lParam.dwStyle = CFS_POINT;
			lParam.ptCurrentPos = p;
			lParam.rcArea = new RECT();
			
			SendMessage(
			            hIMEWnd,
			            WM_IME_CONTROL,
			            IMC_SETCOMPOSITIONWINDOW,
			            lParam
			            );
		}
	}*/
}
