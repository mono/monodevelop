//
// Util.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

#if MAC
using System;
using System.Collections.Generic;
using Foundation;
using ObjCRuntime;
using AppKit;
using Xwt;
using Xwt.Drawing;
using Xwt.Mac;

namespace MonoDevelop.Components.Mac
{
	static class Util
	{
		public static NSColor ToNSColor (this Color col)
		{
			return NSColor.FromDeviceRgba ((float)col.Red, (float)col.Green, (float)col.Blue, (float)col.Alpha);
		}

		public static NSColor ToNSColor (this Cairo.Color col)
		{
			return NSColor.FromDeviceRgba ((float)col.R, (float)col.G, (float)col.B, (float)col.A);
		}

		static readonly CoreGraphics.CGColorSpace DeviceRgbColorSpace = CoreGraphics.CGColorSpace.CreateDeviceRGB ();

		public static CoreGraphics.CGColor ToCGColor (this Cairo.Color col)
		{
			return new CoreGraphics.CGColor (DeviceRgbColorSpace, new nfloat[] {
				(nfloat)col.R, (nfloat)col.G, (nfloat)col.B, (nfloat)col.A
			});
		}
		
		public static CoreGraphics.CGColor ToCGColor (this Color col)
		{
			return new CoreGraphics.CGColor (DeviceRgbColorSpace, new nfloat[] {
				(nfloat)col.Red, (nfloat)col.Green, (nfloat)col.Blue, (nfloat)col.Alpha
			});
		}

		public static NSAttributedString ToAttributedString (this FormattedText ft)
			=> ToAttributedString (ft, null);

		public static NSAttributedString ToAttributedString (
			this FormattedText ft,
			Action<NSMutableAttributedString, NSRange> beforeAttribution)
		{
			NSMutableAttributedString ns = new NSMutableAttributedString (ft.Text);
			ns.BeginEditing ();
			beforeAttribution?.Invoke (ns, new NSRange (0, ns.Length));
			foreach (var att in ft.Attributes) {
				var r = new NSRange (att.StartIndex, att.Count);
				if (att is BackgroundTextAttribute) {
					var xa = (BackgroundTextAttribute)att;
					ns.AddAttribute (NSStringAttributeKey.BackgroundColor, xa.Color.ToNSColor (), r);
				}
				else if (att is ColorTextAttribute) {
					var xa = (ColorTextAttribute)att;
					ns.AddAttribute (NSStringAttributeKey.ForegroundColor, xa.Color.ToNSColor (), r);
				}
				else if (att is UnderlineTextAttribute) {
					var xa = (UnderlineTextAttribute)att;
					int style = xa.Underline ? (int)NSUnderlineStyle.Single : 0;
					ns.AddAttribute (NSStringAttributeKey.UnderlineStyle, (NSNumber)style, r);
				}
				else if (att is FontStyleTextAttribute) {
					var xa = (FontStyleTextAttribute)att;
					if (xa.Style == FontStyle.Italic) {
						ns.ApplyFontTraits (NSFontTraitMask.Italic, r);
					} else if (xa.Style == FontStyle.Oblique) {
						ns.AddAttribute (NSStringAttributeKey.Obliqueness, (NSNumber)0.2f, r);
					} else {
						ns.AddAttribute (NSStringAttributeKey.Obliqueness, (NSNumber)0.0f, r);
						ns.ApplyFontTraits (NSFontTraitMask.Unitalic, r);
					}
				}
				else if (att is FontWeightTextAttribute) {
					var xa = (FontWeightTextAttribute)att;
					var trait = xa.Weight >= FontWeight.Bold ? NSFontTraitMask.Bold : NSFontTraitMask.Unbold;
					ns.ApplyFontTraits (trait, r);
				}
				else if (att is LinkTextAttribute) {
					var xa = (LinkTextAttribute)att;
					ns.AddAttribute (NSStringAttributeKey.Link, new NSUrl (xa.Target.ToString ()), r);
					ns.AddAttribute (NSStringAttributeKey.ForegroundColor, Ide.Gui.Styles.LinkForegroundColor.ToNSColor (), r);
					ns.AddAttribute (NSStringAttributeKey.UnderlineStyle, NSNumber.FromInt32 ((int)NSUnderlineStyle.Single), r);
				}
				else if (att is StrikethroughTextAttribute) {
					var xa = (StrikethroughTextAttribute)att;
					int style = xa.Strikethrough ? (int)NSUnderlineStyle.Single : 0;
					ns.AddAttribute (NSStringAttributeKey.StrikethroughStyle, (NSNumber)style, r);
				}
				else if (att is FontTextAttribute) {
					var xa = (FontTextAttribute)att;
					var nf = (NSFont)Toolkit.GetBackend (xa.Font);
					ns.AddAttribute (NSStringAttributeKey.Font, nf, r);
				}
			}
			ns.EndEditing ();
			return ns;
		}
	}

	public interface ICopiableObject
	{
		void CopyFrom (object other);
	}
}

#endif
