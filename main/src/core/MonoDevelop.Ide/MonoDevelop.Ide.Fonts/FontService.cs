// 
// FontService.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Linq;
using Mono.Addins;
using MonoDevelop.Core;
using Pango;

namespace MonoDevelop.Ide.Fonts
{
	public static class FontService
	{
		static List<FontDescriptionCodon> fontDescriptions = new List<FontDescriptionCodon> ();
		static Dictionary<string, FontDescription> loadedFonts = new Dictionary<string, FontDescription> ();
		static Properties fontProperties;
		static FontDescription defaultMonospaceFontDescription;
		
		internal static IEnumerable<FontDescriptionCodon> FontDescriptions {
			get {
				return fontDescriptions;
			}
		}
		
		static FontService ()
		{
			fontProperties = PropertyService.Get ("FontProperties", new Properties ());
			
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/Fonts", delegate(object sender, ExtensionNodeEventArgs args) {
				var codon = (FontDescriptionCodon)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					fontDescriptions.Add (codon);
					break;
				case ExtensionChange.Remove:
					fontDescriptions.Remove (codon);
					if (loadedFonts.ContainsKey (codon.Name))
						loadedFonts.Remove (codon.Name);
					break;
				}
			}); 
		}

		public static FontDescription DefaultMonospaceFontDescription {
			get {
				if (defaultMonospaceFontDescription == null)
					defaultMonospaceFontDescription = LoadFont (DesktopService.DefaultMonospaceFont);
				return defaultMonospaceFontDescription;
			}
		}

		static FontDescription LoadFont (string name)
		{
			return Pango.FontDescription.FromString (FilterFontName (name));
		}
		
		public static string FilterFontName (string name)
		{
			if (name == "_DEFAULT_MONOSPACE")
				return DesktopService.DefaultMonospaceFont;
			if (name == "_DEFAULT_SANS") {
				var label = new Gtk.Label ("");
				string result = label.Style.FontDescription.Family + " " + ((int)label.Style.FontDesc.Size / Pango.Scale.PangoScale);
				label.Destroy ();
				return result;
			}
			
			return name;
		}
		
		public static string GetUnderlyingFontName (string name)
		{
			var result = fontProperties.Get<string> (name);
			
			if (result == null) {
				var font = GetFont (name);
				if (font == null)
					throw new InvalidOperationException ("Font " + name + " not found.");
				return font.FontDescription;
			}
			return result;
		}

		/// <summary>
		/// Gets the font description for the provided font id
		/// </summary>
		/// <returns>
		/// The font description.
		/// </returns>
		/// <param name='name'>
		/// Identifier of the font
		/// </param>
		/// <param name='createDefaultFont'>
		/// If set to <c>false</c> and no custom font has been set, the method will return null.
		/// </param>
		public static FontDescription GetFontDescription (string name, bool createDefaultFont = true)
		{
			if (loadedFonts.ContainsKey (name))
				return loadedFonts [name];
			return loadedFonts [name] = LoadFont (GetUnderlyingFontName (name));
		}
		
		internal static FontDescriptionCodon GetFont (string name)
		{
			foreach (var d in fontDescriptions) {
				if (d.Name == name)
					return d;
			}
			LoggingService.LogError ("Font " + name + " not found.");
			return null;
		}
		
		public static void SetFont (string name, string value)
		{
			if (loadedFonts.ContainsKey (name)) 
				loadedFonts.Remove (name);
			fontProperties.Set (name, value);
			
			if (fontChangeCallbacks.ContainsKey (name)) 
				fontChangeCallbacks [name].ForEach (c => c ());
		}
		
		static Dictionary<string, List<Action>> fontChangeCallbacks = new Dictionary<string, List<Action>> ();
		public static void RegisterFontChangedCallback (string fontName, Action callback)
		{
			if (!fontChangeCallbacks.ContainsKey (fontName))
				fontChangeCallbacks [fontName] = new List<Action> ();
			fontChangeCallbacks [fontName].Add (callback);
		}
		
		public static void RemoveCallback (Action callback)
		{
			foreach (var list in fontChangeCallbacks.Values)
				list.Remove (callback);
		}
	}
}