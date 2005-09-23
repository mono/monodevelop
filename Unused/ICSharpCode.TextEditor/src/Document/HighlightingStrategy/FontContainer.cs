// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

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
