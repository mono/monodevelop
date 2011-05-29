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
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	[Flags]
	public enum SpanBeginFlags
	{
		None = 0,
		StartsLine = 1,
		FirstNonWs = 2
	}

	[Flags]
	public enum SpanExitFlags
	{
		None = 0,
		CancelSpan = 1
	}
	
	[Flags]
	public enum SpanEndFlags
	{
		None = 0
	}

	public class Span
	{
		public Span ()
		{
		}

		public virtual bool GetIsValid (ColorSheme style)
		{
			return (string.IsNullOrEmpty (Color) || style.GetChunkStyle (Color) != null) &&
			        (string.IsNullOrEmpty (TagColor) || style.GetChunkStyle (TagColor) != null) &&
			        (string.IsNullOrEmpty (NextColor) || style.GetChunkStyle (NextColor) != null);
		}

		public const string Node    = "Span";
		public const string AltNode = "EolSpan";

		public Regex Begin {
			get;
			set;
		}

		public Regex Exit {
			get;
			set;
		}

		public Regex End {
			get;
			set;
		}

		public string Color {
			get;
			set;
		}

		public string TagColor {
			get;
			set;
		}

		public string Escape {
			get;
			set;
		}

		public bool StopAtEol {
			get;
			set;
		}

		public string Rule {
			get;
			set;
		}

		public string NextColor {
			get;
			set;
		}

		public string Continuation {
			get;
			set;
		}

		public SpanEndFlags EndFlags {
			get;
			set;
		}

		public SpanBeginFlags BeginFlags {
			get;
			set;
		}

		public SpanExitFlags ExitFlags {
			get;
			set;
		}

		public override string ToString ()
		{
			return String.Format ("[Span: Color={0}, Rule={1}, Begin={2}, End={3}, Escape={4}, stopAtEol={5}]", Color, Rule, Begin, End, String.IsNullOrEmpty (Escape) ? "not set" : "'" + Escape +"'", StopAtEol);
		}

		static void AddFlags (HashSet<string> hashSet, string flags)
		{
			if (String.IsNullOrEmpty (flags))
				return;
			foreach (string flag in flags.Split(',', ';')) {
				hashSet.Add (flag.Trim ());
			}
		}

		public static Span Read (XmlReader reader)
		{
			Span result = new Span ();

			result.Rule       = reader.GetAttribute ("rule");
			result.Color      = reader.GetAttribute ("color");
			result.TagColor   = reader.GetAttribute ("tagColor");
			result.NextColor  = reader.GetAttribute ("nextColor");

			result.Escape = reader.GetAttribute ("escape");

			string stopateol = reader.GetAttribute ("stopateol");
			if (!String.IsNullOrEmpty (stopateol)) {
				result.StopAtEol = Boolean.Parse (stopateol);
			}

			if (reader.LocalName == AltNode) {
				string beginFlags = reader.GetAttribute ("flags");
				if (!string.IsNullOrEmpty (beginFlags))
					result.BeginFlags = (SpanBeginFlags)Enum.Parse (typeof(SpanBeginFlags), beginFlags);
				result.Continuation = reader.GetAttribute ("continuation");
				result.Begin        = new Regex (reader.ReadElementString ());
				result.StopAtEol = true;
			} else {
				XmlReadHelper.ReadList (reader, Node, delegate () {
					switch (reader.LocalName) {
					case "Begin":
						string beginFlags = reader.GetAttribute ("flags");
						if (!string.IsNullOrEmpty (beginFlags))
							result.BeginFlags = (SpanBeginFlags)Enum.Parse (typeof(SpanBeginFlags), beginFlags);
						result.Begin = new Regex (reader.ReadElementString ());
						return true;
					case "End":
						string endFlags = reader.GetAttribute ("flags");
						if (!string.IsNullOrEmpty (endFlags))
							result.EndFlags = (SpanEndFlags)Enum.Parse (typeof(SpanEndFlags), endFlags);
						result.End = new Regex (reader.ReadElementString ());
						return true;
					case "Exit":
						string exitFlags = reader.GetAttribute ("flags");
						if (!string.IsNullOrEmpty (exitFlags))
							result.ExitFlags = (SpanExitFlags)Enum.Parse (typeof(SpanExitFlags), exitFlags);
						result.Exit = new Regex (reader.ReadElementString ());
						return true;
					}
					return false;
				});
			}
			return result;
		}
	}
}
