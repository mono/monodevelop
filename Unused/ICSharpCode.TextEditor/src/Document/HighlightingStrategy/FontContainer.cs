//  FontContainer.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
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
using System.Drawing;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

using Pango;

namespace MonoDevelop.TextEditor.Document
{
	/// <summary>
	/// This class is used to generate bold, italic and bold/italic fonts out
	/// of a base font.
	/// </summary>
	public class FontContainer
	{
		static FontDescription defaultfont    = null;
		static FontDescription boldfont       = null;
		static FontDescription italicfont     = null;
		static FontDescription bolditalicfont = null;
		
		/// <value>
		/// The bold version of the base font
		/// </value>
		public static FontDescription BoldFont {
			get {
				Debug.Assert(boldfont != null, "MonoDevelop.TextEditor.Document.FontContainer : boldfont == null");
				return boldfont;
			}
		}
		
		/// <value>
		/// The italic version of the base font
		/// </value>
		public static FontDescription ItalicFont {
			get {
				Debug.Assert(italicfont != null, "MonoDevelop.TextEditor.Document.FontContainer : italicfont == null");
				return italicfont;
			}
		}
		
		/// <value>
		/// The bold/italic version of the base font
		/// </value>
		public static FontDescription BoldItalicFont {
			get {
				Debug.Assert(bolditalicfont != null, "MonoDevelop.TextEditor.Document.FontContainer : bolditalicfont == null");
				return bolditalicfont;
			}
		}
		
		/// <value>
		/// The base font
		/// </value>
		public static FontDescription DefaultFont {
			get {
				return defaultfont;
			}
			set {
				defaultfont    = value;

				boldfont = defaultfont.Copy ();
				boldfont.Weight = Weight.Bold;
				
				italicfont = defaultfont.Copy ();
				italicfont.Style = Style.Italic;

				bolditalicfont = defaultfont.Copy ();
				bolditalicfont.Style = Style.Italic;
				bolditalicfont.Weight = Weight.Bold;
			}
		}
		
//		static void CheckFontChange(object sender, PropertyEventArgs e)
//		{
//			if (e.Key == "DefaultFont") {
//				DefaultFont = ParseFont(e.NewValue.ToString());
//			}
//		}
		
		public static FontDescription ParseFont(string font)
		{
			//string[] descr = font.Split(new char[]{',', '='});
			//return new Font(descr[1], Single.Parse(descr[3]));
			return FontDescription.FromString (font);
		}
		
		static FontContainer()
		{
			DefaultFont = FontDescription.FromString ("Courier 10 Pitch, 10");
		}
	}
}
