// Keywords.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	public class Keywords
	{
		List<string>  words = new List<string> ();
		
		public string Color {
			get;
			set;
		}

		public IEnumerable<string> Words {
			get {
				return words;
			}
			set {
				words = new List<string> (value);
			}
		}

		public bool IgnoreCase {
			get;
			private set;
		}
		
		public Keywords ()
		{
		}
		
		public virtual bool GetIsValid (ColorScheme style)
		{
			if (style.GetChunkStyle (Color) == null) {
				System.Console.WriteLine("color:" + Color + " not found.");
				return false;
			}
			return true;
		}
		
		public const string Node = "Keywords";
		
		public static Keywords Read (XmlReader reader, bool ignoreCaseDefault)
		{
			Keywords result = new Keywords ();
			
			result.Color      = reader.GetAttribute ("color");
			if (!String.IsNullOrEmpty (reader.GetAttribute ("ignorecase"))) {
				result.IgnoreCase = Boolean.Parse (reader.GetAttribute ("ignorecase"));
			} else {
				result.IgnoreCase = ignoreCaseDefault;
			}
			
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case "Word":
					result.words.Add (reader.ReadElementString ());
					return true;
				};
				return false;
			});
			return result;
		}
	}
}
