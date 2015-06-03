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

namespace MonoDevelop.Components.Mac
{
	static class Util
	{
		static Selector selCopyWithZone = new Selector ("copyWithZone:");
		static Selector selRetainCount = new Selector ("retainCount");
		static DateTime lastCopyPoolDrain = DateTime.Now;
		static List<object> copyPool = new List<object> ();

		/// <summary>
		/// Implements the NSCopying protocol in a class. The class must implement ICopiableObject.
		/// The method ICopiableObject.CopyFrom will be called to make the copy of the object
		/// </summary>
		/// <typeparam name="T">Type for which to enable copying</typeparam>
		public static void MakeCopiable<T> () where T:ICopiableObject
		{
			Class c = new Class (typeof(T));
			c.AddMethod (selCopyWithZone.Handle, new Func<IntPtr, IntPtr, IntPtr, IntPtr> (MakeCopy), "i@:@");
		}

		static IntPtr MakeCopy (IntPtr sender, IntPtr sel, IntPtr zone)
		{
			var thisOb = (ICopiableObject) Runtime.GetNSObject (sender);

			// Makes a copy of the object by calling the default implementation of copyWithZone
			IntPtr copyHandle = Messaging.IntPtr_objc_msgSendSuper_IntPtr(((NSObject)thisOb).SuperHandle, selCopyWithZone.Handle, zone);
			var copyOb = (ICopiableObject) Runtime.GetNSObject (copyHandle);

			// Copy of managed data
			copyOb.CopyFrom (thisOb);

			// Copied objects are for internal use of the Cocoa framework. We need to keep a reference of the
			// managed object until the the framework doesn't need it anymore.

			if ((DateTime.Now - lastCopyPoolDrain).TotalSeconds > 2)
				DrainObjectCopyPool ();

			copyPool.Add (copyOb);

			return ((NSObject)copyOb).Handle;
		}

		public static void DrainObjectCopyPool ()
		{
			// Objects in the pool have been created by Cocoa, so there should be no managed references
			// other than the ones we keep in the pool. An object can be removed from the pool if it
			// has only 1 reference left (the managed one)

			List<NSObject> markedForDelete = new List<NSObject> ();

			foreach (NSObject ob in copyPool) {
				uint count = Messaging.UInt32_objc_msgSend (ob.Handle, selRetainCount.Handle);
				if (count == 1)
					markedForDelete.Add (ob);
			}
			foreach (NSObject ob in markedForDelete)
				copyPool.Remove (ob);

			lastCopyPoolDrain = DateTime.Now;
		}

		public static NSColor ToNSColor (this Color col)
		{
			return NSColor.FromDeviceRgba ((float)col.Red, (float)col.Green, (float)col.Blue, (float)col.Alpha);
		}

		static Selector applyFontTraits = new Selector ("applyFontTraits:range:");

		public static NSAttributedString ToAttributedString (this FormattedText ft)
		{
			NSMutableAttributedString ns = new NSMutableAttributedString (ft.Text);
			ns.BeginEditing ();
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
					int style = xa.Underline ? 0x01 /*NSUnderlineStyleSingle*/ : 0;
					ns.AddAttribute (NSStringAttributeKey.UnderlineStyle, (NSNumber)style, r);
				}
				else if (att is FontStyleTextAttribute) {
					var xa = (FontStyleTextAttribute)att;
					if (xa.Style == FontStyle.Italic) {
						Messaging.void_objc_msgSend_int_NSRange (ns.Handle, applyFontTraits.Handle, (IntPtr)(long)NSFontTraitMask.Italic, r);
					} else if (xa.Style == FontStyle.Oblique) {
						ns.AddAttribute (NSStringAttributeKey.Obliqueness, (NSNumber)0.2f, r);
					} else {
						ns.AddAttribute (NSStringAttributeKey.Obliqueness, (NSNumber)0.0f, r);
						Messaging.void_objc_msgSend_int_NSRange (ns.Handle, applyFontTraits.Handle, (IntPtr)(long)NSFontTraitMask.Unitalic, r);
					}
				}
				else if (att is FontWeightTextAttribute) {
					var xa = (FontWeightTextAttribute)att;
					var trait = xa.Weight >= FontWeight.Bold ? NSFontTraitMask.Bold : NSFontTraitMask.Unbold;
					Messaging.void_objc_msgSend_int_NSRange (ns.Handle, applyFontTraits.Handle, (IntPtr)(long) trait, r);
				}
				else if (att is LinkTextAttribute) {
					var xa = (LinkTextAttribute)att;
					ns.AddAttribute (NSStringAttributeKey.Link, new NSUrl (xa.Target.ToString ()), r);
					ns.AddAttribute (NSStringAttributeKey.ForegroundColor, NSColor.Blue, r);
					ns.AddAttribute (NSStringAttributeKey.UnderlineStyle, NSNumber.FromInt32 ((int)NSUnderlineStyle.Single), r);
				}
				else if (att is StrikethroughTextAttribute) {
					var xa = (StrikethroughTextAttribute)att;
					int style = xa.Strikethrough ? 0x01 /*NSUnderlineStyleSingle*/ : 0;
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
