//
// Taken from PodSleuth (http://git.gnome.org/cgit/podsleuth)
//  
// Author:
//       Aaron Bockover <abockover@novell.com>
// 
// Copyright (c) 2007-2009 Novell, Inc. (http://www.novell.com)
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
using System.Collections;

namespace PropertyList
{
	public abstract class PlistObjectBase
	{
		public abstract void Write (System.Xml.XmlTextWriter writer);

		protected static PlistObjectBase ObjectToPlistObject (object value)
		{
			if (value is string) {
				return new PlistString ((string)value);
			} else if (value is bool) {
				return new PlistBoolean ((bool)value);
			} else if (value is double) {
				return new PlistReal ((double)value);
			} else if (value is int) {
				return new PlistInteger ((int)value);
			} else if (value is IEnumerable) {
				return new PlistArray ((IEnumerable)value);
			} else if (value is IDictionary) {
				return new PlistDictionary ((IDictionary)value);
			}

			throw new InvalidCastException (String.Format ("`{0}' cannot be converted to a PlistObjectBase", value.GetType ()));
		}

		public static implicit operator PlistObjectBase (string value)
		{
			return new PlistString (value);
		}

		public static implicit operator PlistObjectBase (int value)
		{
			return new PlistInteger (value);
		}

		public static implicit operator PlistObjectBase (double value)
		{
			return new PlistReal (value);
		}

		public static implicit operator PlistObjectBase (bool value)
		{
			return new PlistBoolean (value);
		}

		public static implicit operator PlistObjectBase (object[] value)
		{
			return new PlistArray (value);
		}

		public static implicit operator PlistObjectBase (ArrayList value)
		{
			return new PlistArray (value);
		}

		public static implicit operator PlistObjectBase (Hashtable value)
		{
			return new PlistDictionary (value);
		}
	}
}
