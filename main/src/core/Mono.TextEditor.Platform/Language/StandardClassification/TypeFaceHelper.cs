// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.StandardClassification.Implementation
{
    using System.Windows;
    using System.Windows.Media;

    internal static class TypeFaceHelper
    {
        private static FontStyleConverter styleConverter = new FontStyleConverter();
        private static FontWeightConverter weightConverter = new FontWeightConverter();
        private static FontStretchConverter stretchConverter = new FontStretchConverter();

        internal static Typeface CreateTypeFace(string fontFamily, string fontStyle, string fontWeight, string fontStretch, string fallbackFontFamily)
        {
            if ((fontFamily == null) || (fontStyle == null) || (fontWeight == null) || (fontStretch == null))
            {
                return null;
            }

            FontFamily family = new FontFamily(fontFamily);
            FontStyle style = (FontStyle)styleConverter.ConvertFromString(fontStyle);
            FontWeight weight = (FontWeight)weightConverter.ConvertFromString(fontWeight);
            FontStretch stretch = (FontStretch)stretchConverter.ConvertFromString(fontStretch);

            if (fallbackFontFamily == null)
                return new Typeface(family, style, weight, stretch);
            else
                return new Typeface(family, style, weight, stretch, new FontFamily(fallbackFontFamily));
        }
    }
}
