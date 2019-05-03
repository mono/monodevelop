//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Windows;

using AppKit;

using Gdk;

using Microsoft.VisualStudio.Text.Classification;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Projects;

namespace MonoDevelop.TextEditor
{
	[ExportDocumentControllerFactory (FileExtension = "*", InsertBefore = "TextEditor")]
	class CocoaTextViewDisplayBinding : TextViewDisplayBinding<CocoaTextViewImports>
	{
		static CocoaTextViewDisplayBinding ()
		{
			Microsoft.VisualStudio.UI.GettextCatalog.Initialize (GettextCatalog.GetString, GettextCatalog.GetString);
			Microsoft.VisualStudio.Text.Editor.Implementation.CocoaLocalEventMonitor.FilterGdkEvents += (enable) => {
				if (enable)
					Gdk.Window.AddFilterForAll (Filter);
				else
					Gdk.Window.RemoveFilterForAll (Filter);
			};
		}

		static FilterReturn Filter (IntPtr xevent, Event evnt)
		{
			return FilterReturn.Remove;
		}

		protected override DocumentController CreateContent (CocoaTextViewImports imports)
		{
			return new CocoaTextViewContent (imports);
		}

		protected override ThemeToClassification CreateThemeToClassification (IEditorFormatMapService editorFormatMapService)
			=> new CocoaThemeToClassification (editorFormatMapService);

		class CocoaThemeToClassification : ThemeToClassification
		{
			public CocoaThemeToClassification (IEditorFormatMapService editorFormatMapService) : base (editorFormatMapService) {}

			protected override void AddFontToDictionary (ResourceDictionary resourceDictionary, string appearanceCategory, string fontName, double fontSize)
			{
				if (appearanceCategory == "tooltip")
					return;

				if (fontSize <= 0)
					fontSize = NSFont.SystemFontSize;

				var pangoFontDescription = $"{fontName} {fontSize}";

				NSFont nsFont;

				try {
					nsFont = GetNSFontFromPangoFontDescription (pangoFontDescription);
				} catch (Exception e) {
					nsFont = null;
					LoggingService.LogInternalError (
						$"Exception attempting to map Pango font description '{pangoFontDescription}' to an NSFont",
						e);
				}

				if (nsFont == null) {
					LoggingService.LogWarning (
						$"Unable to map Pango font description '{pangoFontDescription}' " +
						$"to NSFont; falling back to system default at {fontSize} pt");
					nsFont = NSFontWorkarounds.UserFixedPitchFontOfSize ((nfloat)fontSize);
				}

				fontSize = nsFont.PointSize;

				LoggingService.LogInfo ($"Mapped Pango font description '{pangoFontDescription}' to NSFont '{nsFont}'");

				resourceDictionary [ClassificationFormatDefinition.TypefaceId] = nsFont;
				resourceDictionary [ClassificationFormatDefinition.FontRenderingSizeId] = fontSize;
			}

			static NSFont GetNSFontFromPangoFontDescription (string fontDescription)
				=> GetNSFontFromPangoFontDescription (Pango.FontDescription.FromString (fontDescription));

			static NSFont GetNSFontFromPangoFontDescription (Pango.FontDescription fontDescription)
			{
				if (fontDescription == null)
					return null;

				return NSFontManager.SharedFontManager.FontWithFamilyWorkaround (
					fontDescription.Family,
					fontDescription.Style == Pango.Style.Italic || fontDescription.Style == Pango.Style.Oblique
						? NSFontTraitMask.Italic
						: 0,
					NormalizeWeight (fontDescription.Weight),
					fontDescription.Size / (nfloat)Pango.Scale.PangoScale);

				/// <summary>
				/// Normalizes a Pango font weight (100-1000 scale) to a weight
				/// suitable for NSFontDescription.FontWithFamily (0-15 scale).
				/// </summary>
				int NormalizeWeight (Pango.Weight pangoWeight)
				{
					double Normalize (double value, double inMin, double inMax, double outMin, double outMax)
						=> (outMax - outMin) / (inMax - inMin) * (value - inMax) + outMax;

					return (int)Math.Round (Normalize ((int)pangoWeight, 100, 1000, 0, 15));
				}
			}
		}
	}
}