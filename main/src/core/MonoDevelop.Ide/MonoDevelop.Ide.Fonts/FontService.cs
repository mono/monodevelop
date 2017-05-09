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

		static string defaultMonospaceFontName = String.Empty;
		static FontDescription defaultMonospaceFont = new FontDescription ();

		static void LoadDefaults ()
		{
			if (defaultMonospaceFont != null) {
				defaultMonospaceFont.Dispose ();
			}

			#pragma warning disable 618
			defaultMonospaceFontName = DesktopService.DefaultMonospaceFont;
			defaultMonospaceFont = FontDescription.FromString (defaultMonospaceFontName);
			#pragma warning restore 618
		}
		
		internal static IEnumerable<FontDescriptionCodon> FontDescriptions {
			get {
				return fontDescriptions;
			}
		}
		
		internal static void Initialize ()
		{
			if (fontProperties != null)
				throw new InvalidOperationException ("Already initialized");

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

			LoadDefaults ();
		}

		public static FontDescription MonospaceFont { get { return defaultMonospaceFont; } }
		public static FontDescription SansFont { get { return Gui.Styles.DefaultFont; } }

		public static string MonospaceFontName { get { return defaultMonospaceFontName; } }
		public static string SansFontName { get { return Gui.Styles.DefaultFontName; } }

		[Obsolete ("Use MonospaceFont")]
		public static FontDescription DefaultMonospaceFontDescription {
			get {
				if (defaultMonospaceFont == null)
					defaultMonospaceFont = LoadFont (DesktopService.DefaultMonospaceFont);
				return defaultMonospaceFont;
			}
		}

		static FontDescription LoadFont (string name)
		{
			var fontName = FilterFontName (name);
			return FontDescription.FromString (fontName);
		}
		
		public static string FilterFontName (string name)
		{
			switch (name) {
			case "_DEFAULT_MONOSPACE":
				return defaultMonospaceFontName;
			case "_DEFAULT_SANS":
				return SansFontName;
			default:
				return name;
			}
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

			var font = GetFont (name);
			if (font != null && font.FontDescription == value) {
				fontProperties.Set (name, null);
			} else {
				fontProperties.Set (name, value);
			}
			List<Action> callbacks;
			if (fontChangeCallbacks.TryGetValue (name, out callbacks)) {
				callbacks.ForEach (c => c ());
			}
		}

		internal static ConfigurationProperty<FontDescription> GetFontProperty (string name)
		{
			return new FontConfigurationProperty (name);
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
			foreach (var list in fontChangeCallbacks.Values.ToList ())
				list.Remove (callback);
		}
	}

	class FontConfigurationProperty: ConfigurationProperty<FontDescription>
	{
		string name;

		public FontConfigurationProperty (string name)
		{
			this.name = name;
			FontService.RegisterFontChangedCallback (name, OnChanged);
		}

		protected override FontDescription OnGetValue ()
		{
			return FontService.GetFontDescription (name);
		}

		protected override bool OnSetValue (FontDescription value)
		{
			FontService.SetFont (name, value.ToString ());
			return true;
		}
	}

	public static class FontExtensions
	{
		public static FontDescription CopyModified (this FontDescription font, double? scale = null, Pango.Weight? weight = null)
		{
			font = font.Copy ();

			if (scale.HasValue)
				Scale (font, scale.Value);

			if (weight.HasValue)
				font.Weight = weight.Value;

			return font;
		}
		public static FontDescription CopyModified (this FontDescription font, int absoluteResize, Pango.Weight? weight = null)
		{
			font = font.Copy ();

			ResizeAbsolute (font, absoluteResize);

			if (weight.HasValue)
				font.Weight = weight.Value;

			return font;
		}

		static void Scale (FontDescription font, double scale)
		{
			if (font.SizeIsAbsolute) {
				font.AbsoluteSize = scale * font.Size;
			} else {
				var size = font.Size;
				if (size == 0)
					size = (int)(10 * Pango.Scale.PangoScale); 
				font.Size = (int)(Pango.Scale.PangoScale * (int)(scale * size / Pango.Scale.PangoScale));
			}
		}

		static void ResizeAbsolute (FontDescription font, int pt)
		{
			if (font.SizeIsAbsolute) {
				font.AbsoluteSize = font.Size + pt;
			} else {
				var size = font.Size;
				if (size == 0)
					size = (int)((10 + pt) * Pango.Scale.PangoScale);
				font.Size = (int)(Pango.Scale.PangoScale * (int)(pt + size / Pango.Scale.PangoScale));
			}
		}

		public static FontDescription ToPangoFont (this Xwt.Drawing.Font font)
		{
			var backend = Xwt.Toolkit.GetBackend (font) as FontDescription;
			if (backend != null)
				return backend.Copy ();
			return FontDescription.FromString (font.ToString ());
		}

		public static Xwt.Drawing.Font ToXwtFont (this FontDescription font)
		{
			return font.ToXwtFont (null);
		}

		public static Xwt.Drawing.Font ToXwtFont (this FontDescription font, Xwt.Toolkit withToolkit)
		{
			var toolkit = withToolkit ?? Xwt.Toolkit.CurrentEngine;
			Xwt.Drawing.Font xwtFont = null;
			toolkit.Invoke (() => xwtFont = Xwt.Drawing.Font.FromName (font.ToString ()));
			return xwtFont;
		}
	}
}
