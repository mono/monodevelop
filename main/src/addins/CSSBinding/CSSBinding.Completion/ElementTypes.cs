//
// ElementTypes.cs
//
// Author:
//       Diyoda Sajjana <>
//
// Copyright (c) 2013 Diyoda Sajjana
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

namespace MonoDevelop.CSSBinding.Completion
{
	public class ElementTypes
	{
		public ElementTypes ()
		{
		}

		public static readonly ICollection<string> KeyWord = new string[] {
			"align-content","align-items","align-self","alignment-adjust","alignment-baseline",
			"all","anchor-point","animation","animation-delay","animation-direction",
			"animation-duration","animation-iteration-count","animation-name","animation-play-state",
			"animation-timing-function","appearance","azimuth","backface-visibility","background",
			"background-attachment","background-clip","background-color","background-image",
			"background-origin","background-position","background-repeat","background-size",
			"baseline-shift","binding", "bleed", "bookmark-label", "bookmark-level", "bookmark-state",
			"bookmark-target", "border", "border-bottom", "border-bottom-color", "border-bottom-left-radius", 
			"border-bottom-right-radius", "border-bottom-style", "border-bottom-width","border-collapse","border-color",
			"border-image","border-image-outset","border-image-repeat","border-image-slice","border-image-source","border-image-width",
			"border-left","border-left-color","border-left-style","border-left-width",
			"border-radius","border-right","border-right-color","border-right-style","border-right-width",
			"border-spacing","border-style","border-top","border-top-color","border-top-left-radius","border-top-right-radius",
			"border-top-style","border-top-width","border-width","bottom","box-decoration-break","box-shadow","box-sizing","break-after",
			"break-before","break-inside","caption-side","clear","clip","color","color-profile","column-count","column-fill","column-gap",
			"column-rule","column-rule-color","column-rule-style",
			"column-rule-width","column-span","column-width","columns","content","counter-increment", "counter-reset", "crop", "cue", "cue-after", 
			"cue-before", "cursor", "direction", "display", "dominant-baseline", "drop-initial-after-adjust", "drop-initial-after-align",
			"drop-initial-before-adjust", "drop-initial-before-align", "drop-initial-size", "drop-initial-value", "elevation", "empty-cells",
			"fit", "fit-position", "flex", "flex-basis", "flex-direction", "flex-flow", "flex-grow",
			"flex-shrink", "flex-wrap", "float", "float-offset", "font", "font-feature-settings", "font-family", "font-kerning",
			"font-language-override", "font-size", "font-size-adjust", "font-stretch", "font-style", "font-synthesis", "font-variant", 
			"font-variant-alternates", "font-variant-caps", "font-variant-east-asian", "font-variant-ligatures", "font-variant-numeric", 
			"font-variant-position", "font-weight", "grid-cell","grid-column", "grid-column-align", "grid-column-sizing", "grid-column-span",
			"grid-columns", "grid-flow", "grid-row", "grid-row-align","grid-row-sizing","grid-row-span","grid-rows","grid-template", 
			"hanging-punctuation", "height", "hyphens", "icon", "image-orientation", "image-rendering", "image-resolution", "ime-mode",
			"inline-box-align", "justify-content", "left", "letter-spacing", "line-break", "line-height", "line-stacking", "line-stacking-ruby",
			"line-stacking-shift", "line-stacking-strategy", "list-style", "list-style-image", "list-style-position", "list-style-type", "margin", 
			"margin-bottom", "margin-left", "margin-right", "margin-top", "marker-offset", "marks", "marquee-direction", "marquee-loop",
			"marquee-play-count", "marquee-speed", "marquee-style", "max-height", "max-width", 
			"min-height", "min-width", "move-to", "nav-down", "nav-index", "nav-left", "nav-right", 
			"nav-up", "opacity", "order", "orphans", "outline", "outline-color", "outline-offset", "outline-style", "outline-width", 
			"overflow", "overflow-style",
			"overflow-wrap", "overflow-x", "overflow-y",
			"padding", "padding-bottom", "padding-left", "padding-right", "padding-top", "page", "page-break-after", "page-break-before",
			"page-break-inside", 
			"page-policy", "pause", "pause-after", "pause-before", "perspective", "perspective-origin", "pitch", "pitch-range", "play-during",
			"position", "presentation-level", 
			"punctuation-trim", "quotes", "rendering-intent", "resize", "rest", "rest-after", "rest-before", "richness", "right", "rotation", 
			"rotation-point", "ruby-align", 
			"ruby-overhang", "ruby-position", "ruby-span", "size", "speak", "speak-as", "speak-header", 
			"speak-numeral", "speak-punctuation", "speech-rate", "stress", "string-set", "tab-size", "table-layout", "target", 
			"target-name", "target-new", "target-position",
			"text-align", "text-align-last", "text-decoration", 
			"text-decoration-color", "text-decoration-line","text-decoration-skip","text-decoration-style","text-emphasis",
			"text-emphasis-color","text-emphasis-position","text-emphasis-style","text-height","text-indent","text-justify",
			"text-outline","text-overflow","text-shadow","text-space-collapse","text-transform","text-underline-position",
			"text-wrap","top","transform","transform-origin","transform-style","transition","transition-delay","transition-duration",
			"transition-property","transition-timing-function","unicode-bidi","vertical-align","visibility","voice-balance",
			"voice-duration","voice-family","voice-pitch","voice-range","voice-rate","voice-stress","voice-volume","volume","white-space","widows",
			"width","word-break","word-spacing","word-wrap","z-index",
		};

		public static bool IsKeyWord (string elementName)
		{
			return KeyWord.Contains (elementName.ToLower ());
		}
	}
}

