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
using System.Collections.Generic;

namespace PropertyList
{
	public class PlistArray : PlistObject<List<PlistObjectBase>>, IEnumerable<PlistObjectBase>
	{
		public PlistArray () : base(new List<PlistObjectBase> ())
		{
		}

		public PlistArray (List<PlistObjectBase> value) : base(value)
		{
		}

		public PlistArray (IEnumerable value) : this()
		{
			foreach (object item in value) {
				Add (ObjectToPlistObject (item));
			}
		}

		public override void Write (System.Xml.XmlWriter writer)
		{
			writer.WriteStartElement ("array");
			foreach (PlistObjectBase o in this)
				o.Write (writer);
			writer.WriteEndElement ();
		}

		public void Clear ()
		{
			Value.Clear ();
		}

		public void Add (PlistObjectBase value)
		{
			Value.Add (value);
		}

		public void Add (IDictionary value)
		{
			Value.Add (new PlistDictionary (value));
		}

		public bool Remove (PlistObjectBase value)
		{
			return Value.Remove (value);
		}

		public bool Contains (PlistObjectBase value)
		{
			return Value.Contains (value);
		}

		public int Count {
			get { return Value.Count; }
		}

		public IEnumerator<PlistObjectBase> GetEnumerator ()
		{
			return Value.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return Value.GetEnumerator ();
		}
	}
}
