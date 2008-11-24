// Span.cs
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
using System.Diagnostics;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	public class Span
	{
		string color;
		string tagColor;
		string rule;
		string begin;
		string end;
		string constraint;
		string nextColor;
		string escape;
		
		bool   stopAtEol;
			
		public Span ()
		{
		}
		
		public const string Node    = "Span";
		public const string AltNode = "EolSpan";
		
		public string Begin {
			get {
				return begin;
			}
		}

		public string End {
			get {
				return end;
			}
		}

		public string Color {
			get {
				return color;
			}
		}
		
		public string TagColor {
			get {
				return tagColor;
			}
		}

		public string Escape {
			get {
				return escape;
			}
		}

		public bool StopAtEol {
			get {
				return stopAtEol;
			}
		}

		public string Rule {
			get {
				return rule;
			}
		}

		public string Constraint {
			get {
				return constraint;
			}
		}

		public string NextColor {
			get {
				return nextColor;
			}
		}
		
		public override string ToString ()
		{
			return String.Format ("[Span: Color={0}, Rule={1}, Begin={2}, End={3}, Escape={4}, stopAtEol={5}]", color, rule, begin, end, String.IsNullOrEmpty (escape) ? "not set" : "'" + escape +"'", stopAtEol);
		}
		
		public static Span Read (XmlReader reader)
		{
			Span result = new Span ();
			
			result.rule       = reader.GetAttribute ("rule");
			result.color      = reader.GetAttribute ("color");
			result.tagColor   = reader.GetAttribute ("tagColor");
			result.constraint = reader.GetAttribute ("constraint");
			result.nextColor  = reader.GetAttribute ("nextColor");
			
			result.escape = reader.GetAttribute ("escape");
			
			string stopateol = reader.GetAttribute ("stopateol");
			if (!String.IsNullOrEmpty (stopateol)) {
				result.stopAtEol = Boolean.Parse (stopateol);
			}
			if (reader.LocalName == AltNode) {
				result.begin     = reader.ReadElementString ();
				result.stopAtEol = true;
			} else {
				XmlReadHelper.ReadList (reader, Node, delegate () {
					switch (reader.LocalName) {
					case "Begin":
						result.begin = reader.ReadElementString ();
						return true;
					case "End":
						result.end = reader.ReadElementString ();
						return true;
					}
					return false;
				});
			}
			return result;
		}
	}
}
