// 
// IdeServices.FontService.cs
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
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Core;
using Pango;
using System.Globalization;
#if MAC
using AppKit;
#endif

namespace MonoDevelop.Ide.Fonts
{
	[DefaultServiceImplementation]
	public class FontService: Service
	{
		List<FontDescriptionCodon> fontDescriptions = new List<FontDescriptionCodon> ();
		Dictionary<string, FontDescription> loadedFonts = new Dictionary<string, FontDescription> ();
		Properties fontProperties;
		DesktopService desktopService;

		string defaultMonospaceFontName = String.Empty;
		FontDescription defaultMonospaceFont = new FontDescription ();

		static FontService ()
		{
			InitializeFontTables ();
		}

		void LoadDefaults ()
		{
			if (defaultMonospaceFont != null) {
				defaultMonospaceFont.Dispose ();
			}

			#pragma warning disable 618
			defaultMonospaceFontName = desktopService.DefaultMonospaceFont;
			defaultMonospaceFont = FontDescription.FromString (defaultMonospaceFontName);
			#pragma warning restore 618
		}
		
		internal IEnumerable<FontDescriptionCodon> FontDescriptions {
			get {
				return fontDescriptions;
			}
		}

		protected override async Task OnInitialize (ServiceProvider serviceProvider)
		{
			desktopService = await serviceProvider.GetService<DesktopService> ();
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

		protected override Task OnDispose ()
		{
			if (pangoContext != null) {
				pangoContext.Dispose ();
				pangoContext = null;
			}
			return base.OnDispose ();
		}

		public FontDescription MonospaceFont { get { return defaultMonospaceFont; } }
		public FontDescription SansFont { get { return Gui.Styles.DefaultFont; } }

		public string MonospaceFontName { get { return defaultMonospaceFontName; } }
		public string SansFontName { get { return Gui.Styles.DefaultFontName; } }

		[Obsolete ("Use MonospaceFont")]
		public FontDescription DefaultMonospaceFontDescription {
			get {
				if (defaultMonospaceFont == null)
					defaultMonospaceFont = LoadFont (desktopService.DefaultMonospaceFont);
				return defaultMonospaceFont;
			}
		}

		FontDescription LoadFont (string name)
		{
			var fontName = FilterFontName (name);
			if (TryParsePangoFont (fontName, out var result))
				return result;
			LoggingService.LogError ("Can't load font : " + name);
			return null;
		}
		
		public string FilterFontName (string name)
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
		
		public string GetUnderlyingFontName (string name)
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
		public FontDescription GetFontDescription (string name, bool createDefaultFont = true)
		{
			if (loadedFonts.ContainsKey (name))
				return loadedFonts [name];
			if (TryParsePangoFont(GetUnderlyingFontName (name), out var result))
				return loadedFonts [name] = result;
			return null;
		}

		internal FontDescriptionCodon GetFont (string name)
		{
			foreach (var d in fontDescriptions) {
				if (d.Name == name)
					return d;
			}
			LoggingService.LogError ("Font " + name + " not found.");
			return null;
		}

		public void SetFont (string name, string value)
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

		internal ConfigurationProperty<FontDescription> GetFontProperty (string name)
		{
			return new FontConfigurationProperty (name);
		}

		Dictionary<string, List<Action>> fontChangeCallbacks = new Dictionary<string, List<Action>> ();
		public void RegisterFontChangedCallback (string fontName, Action callback)
		{
			if (!fontChangeCallbacks.ContainsKey (fontName))
				fontChangeCallbacks [fontName] = new List<Action> ();
			fontChangeCallbacks [fontName].Add (callback);
		}

		public void RemoveCallback (Action callback)
		{
			foreach (var list in fontChangeCallbacks.Values.ToList ())
				list.Remove (callback);
		}

#if MAC

		static bool TryGetWeight (string token, out int weight)
		{
			if (fontIndexTable.TryGetValue (token, out weight))
				return true;
			weight = 5;
			return false;
		}

		static bool TryParseTraits (string token, ref NSFontTraitMask traits)
		{
			switch (token) {
			case "Small-Caps":
				traits |= NSFontTraitMask.SmallCaps;
				return true;
			case "Ultra-Condensed":
			case "Extra-Condensed":
			case "UltraCondensed":
				traits |= NSFontTraitMask.Condensed;
				return true;
			case "Semi-Condensed":
			case "Normal":
				return true;
			case "Semi-Expanded":
			case "Expanded":
			case "Extra-Expanded":
			case "Ultra-Expanded":
				traits |= NSFontTraitMask.Expanded;
				return true;
			}
			return false;
		}

		public bool TryParseNSFont (string fontDescription, out NSFont font)
		{
			double size = -1;
			int weight = 5;

			int i = fontDescription.LastIndexOf (' ');
			int lasti = fontDescription.Length;
			var traits = (NSFontTraitMask)0;
			while (i >= 0) {
				var potentialFontName = fontDescription.Substring (0, lasti);
				if (lasti > 0 && NSFontManager.SharedFontManager.AvailableFontFamilies.Contains (potentialFontName))
					break;
				string token = fontDescription.Substring (i + 1, lasti - i - 1);
				if (size < 0) { // take only first number
					double siz;
					if (double.TryParse (token, NumberStyles.Any, CultureInfo.InvariantCulture, out siz)) {
						size = siz;
					}
				} else {
					if (TryGetWeight (token, out int parsedWeight))
						weight = parsedWeight;
					else
						TryParseTraits (token, ref traits);
				}
				lasti = i;
				i = fontDescription.LastIndexOf (' ', i - 1);
			}
			if (lasti <= 0) {
				font = null;
				return false;
			}
			string familyName = fontDescription.Substring (0, lasti);
			font = NSFontManager.SharedFontManager.FontWithFamily (familyName, traits, weight, (nfloat)size);
			return font != null;
		}

		readonly static string [] fontNameTable = {
			"Thin", // 1
			"Ultra-Light",
			"Light",
			"Book",
			"Normal",
			"Medium",
			"Retina",
			"Semibold",
			"Bold",
			"Ultra-Bold",
			"Heavy",
			"Ultra-Heavy",
			"Ultra-Heavy2",
			"Ultra-Heavy3",
			"Ultra-Heavy4" // 15
		};
		readonly static Dictionary<string, int> fontIndexTable = new Dictionary<string, int> ();

		private static void InitializeFontTables ()
		{
			for (int i = 0; i < fontNameTable.Length; i++)
				fontIndexTable [fontNameTable [i]] = i + 1;
		}

		static string GetFontWeightString (nint w) => fontNameTable [Math.Min (fontNameTable.Length - 1, Math.Max (0, w - 1))];

		public string GetFontDescription (NSFont font)
		{
			if (font == null) 
				throw new ArgumentNullException (nameof (font));

			var sb = StringBuilderCache.Allocate ();

			sb.Append (font.FamilyName);

			var weight = NSFontManager.SharedFontManager.WeightOfFont (font);
			if (weight != 5) {
				sb.Append (' ');
				sb.Append (GetFontWeightString (weight));
			}

			sb.Append (' ');
			sb.Append (font.PointSize);

			return StringBuilderCache.ReturnAndFree (sb);
		}

#endif
		Pango.Context pangoContext;
		HashSet<string> installedFonts = new HashSet<string> ();

		static bool TryGetPangoWeight (string token, out Pango.Weight weight)
		{
			switch (token) {
			case "Thin":
				weight = (Pango.Weight)100;
				return true;
			case "Ultra-Light":
				weight = Pango.Weight.Ultralight; // 200
				return true;
			case "Light":
				weight = Pango.Weight.Light; // 300
				return true;
			case "Book":
				weight = (Pango.Weight)380;
				return true;
			case "Normal":
				weight = Pango.Weight.Normal; // 400
				return true;
			case "Medium":
				weight = Pango.Weight.Normal;
				return true;
			case "Retina":
				weight = Pango.Weight.Normal;
				return true;
			case "Semibold":
				weight = Pango.Weight.Semibold; // 600
				return true;
			case "Bold":
				weight = Pango.Weight.Bold; // 700
				return true;
			case "Ultra-Bold":
				weight = Pango.Weight.Ultrabold; // 800
				return true;
			case "Heavy":
				weight = Pango.Weight.Heavy; // 900
				return true;
			case "Ultra-Heavy":
			case "Ultra-Heavy2":
			case "Ultra-Heavy3":
			case "Ultra-Heavy4":
				weight = (Pango.Weight)1000;
				return true;
			}
			weight = Pango.Weight.Normal;
			return false;
		}

		public bool TryParsePangoFont (string fontDescription, out FontDescription font)
		{
			if (pangoContext == null) {
				pangoContext = Gdk.PangoHelper.ContextGet ();
				foreach (var fontFamily in pangoContext.FontMap.Families) {
					Console.WriteLine (fontFamily.Name);
					installedFonts.Add (fontFamily.Name);
				}
			}

			double size = -1;
			var weight = Pango.Weight.Normal;

			int i = fontDescription.LastIndexOf (' ');
			int lasti = fontDescription.Length;
			var style = Pango.Style.Normal;
			var variant = Pango.Variant.Normal;
			while (i >= 0) {
				var potentialFontName = fontDescription.Substring (0, lasti);
				if (lasti > 0 && installedFonts.Contains (potentialFontName))
					break;
				string token = fontDescription.Substring (i + 1, lasti - i - 1);
				if (size < 0) { // take only first number
					double siz;
					if (double.TryParse (token, NumberStyles.Any, CultureInfo.InvariantCulture, out siz)) {
						size = siz;
					}
				} else {
					if (TryGetPangoWeight (token, out var parsedWeight))
						weight = parsedWeight;
					else if (Enum.TryParse<Pango.Style> (token, out var parsedStyle))
						style = parsedStyle;
					else if (token == "Small-Caps")
						variant = Variant.SmallCaps;
				}
				lasti = i;
				i = fontDescription.LastIndexOf (' ', i - 1);
			}
			if (lasti <= 0) {
				font = null;
				return false;
			}
			string familyName = fontDescription.Substring (0, lasti);

			font = new FontDescription {
				Family = familyName,
				Weight = weight,
				Style = style,
				Size = (int)(size * Pango.Scale.PangoScale),
				Variant = variant
			};
			return true;
		}
	}

	class FontConfigurationProperty: ConfigurationProperty<FontDescription>
	{
		string name;

		public FontConfigurationProperty (string name)
		{
			this.name = name;
			IdeServices.FontService.RegisterFontChangedCallback (name, OnChanged);
		}

		protected override FontDescription OnGetValue ()
		{
			return IdeServices.FontService.GetFontDescription (name);
		}

		protected override bool OnSetValue (FontDescription value)
		{
			IdeServices.FontService.SetFont (name, value.ToString ());
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
