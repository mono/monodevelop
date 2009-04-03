// Rule.cs
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
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	public class Rule
	{
		protected List<Keywords> keywords = new List<Keywords> ();
		protected List<Span> spans = new List<Span> ();
		protected Match[] matches = new Match[0];
		protected List<Marker> prevMarker = new List<Marker> ();
		
		public List<SemanticRule> SemanticRules = new List<SemanticRule> ();
		
		public virtual bool GetIsValid (Style style)
		{
			foreach (Keywords keyword in keywords) {
				if (!keyword.GetIsValid (style)) {
					System.Console.WriteLine (keyword + " failed.");
					return false;
				}
			}
			foreach (Span span in spans) {
				if (!span.GetIsValid (style)) {
					System.Console.WriteLine (span + " failed.");
					return false;
				}
			}
			foreach (Match match in matches) {
				if (!match.GetIsValid (style)) {
					System.Console.WriteLine (match + " failed.");
					return false;
				}
			}
			foreach (Marker marker in prevMarker) {
				if (!marker.GetIsValid (style)) {
					System.Console.WriteLine (marker + " failed.");
					return false;
				}
			}
			return true;
		}
		
		public string Name {
			get;
			protected set;
		}
		
		public IEnumerable<Keywords> Keywords {
			get {
				return keywords;
			}
		}
		
		public IEnumerable<Span> Spans {
			get {
				return spans;
			}
		}
		
		public Match[] Matches {
			get {
				return this.matches;
			}
		}
		
		public bool HasMatches {
			get {
				return this.matches.Length > 0;
			}
		}
		
		public bool IgnoreCase {
			get;
			protected set;
		}
		
		public string DefaultColor {
			get;
			private set;
		}

		public List<Marker> PrevMarker {
			get {
				return prevMarker;
			}
		}
		
		public Rule()
		{
		}
		
		public override string ToString ()
		{
			return String.Format ("[Rule: Name={0}, #Keywords={1}]", Name, keywords.Count);
		}
		
		protected bool ReadNode (XmlReader reader, List<Match> matchList)
		{
			switch (reader.LocalName) {
			case Match.Node:
				matchList.Add (Match.Read (reader));
				return true;
			case Span.Node:
			case Span.AltNode:
				this.spans.Add (Span.Read (reader));
				return true;
			case Mono.TextEditor.Highlighting.Keywords.Node:
				this.keywords.Add (Mono.TextEditor.Highlighting.Keywords.Read (reader, IgnoreCase));
				return true;
			case Marker.PrevMarker:
				this.prevMarker.Add (Marker.Read (reader));
				return true;
			}
			return false;
		}
		
		public class Pair<S, T>
		{
			public S o1;
			public T o2;
			
			public Pair (S o1, T o2)
			{
				this.o1 = o1;
				this.o2 = o2;
			}
			
			public override string ToString ()
			{
				return String.Format ("[Pair: o1={0}, o2={1}]", o1, o2);
			}
		}
		public Dictionary<char, Span[]>             spanStarts = new Dictionary<char, Span[]> ();
		public Dictionary<char, Pair<Keywords, object>> parseTree  = new Dictionary<char, Pair<Keywords, object>> ();
		
		protected void SetupSpanTree ()
		{
			Dictionary<char, List<Span>> tree = new Dictionary<char, List<Span>> ();
			foreach (Span span in this.spans) {
				//List<Span> list = null;
				char start = span.Begin[0];
				if (!tree.ContainsKey (start))
					tree.Add (start, new List<Span> ());
				tree[start].Add (span);
			}
			
			spanStarts.Clear ();
			foreach (KeyValuePair<char, List<Span>> spans in tree) {
				spanStarts[spans.Key] = spans.Value.ToArray ();
			}
		}
		
		void AddToParseTree (string key, Keywords value)
		{
			Dictionary<char, Pair<Keywords, object>> tree = parseTree;
			Pair<Keywords, object> pair = null;
			
			for (int i = 0; i < key.Length; i++) {
				if (!tree.ContainsKey (key[i]))
					tree.Add (key[i], new Pair<Keywords, object> (null, new Dictionary<char, Pair<Keywords, object>>()));
				pair = tree[key[i]];
				tree = (Dictionary<char, Pair<Keywords, object>>)pair.o2;
			}
			pair.o1 = value;
		}
		
		void AddWord (Keywords kw, string word, int i)
		{
			AddToParseTree (word, kw);
			char[] chars = word.ToCharArray ();
			
			if (i < word.Length) {
				chars[i] = Char.ToUpperInvariant (chars[i]);
				AddWord (kw, new string (chars), i + 1);
				
				chars[i] = Char.ToLowerInvariant (chars[i]);
				AddWord (kw, new string (chars), i + 1);
			}
		}
		
		protected void SetupParseTree ()
		{
			foreach (Keywords kw in this.keywords) {  
				foreach (string word in kw.Words)  {
					if (kw.IgnoreCase) {
						AddWord (kw, word.ToLowerInvariant (), 0);
					} else {
						AddToParseTree (word, kw);
					}
				}
			}
		}
		
		public const string Node = "Rule";
		public static Rule Read (XmlReader reader)
		{
			Rule result = new Rule ();
			result.Name         = reader.GetAttribute ("name");
			result.DefaultColor = reader.GetAttribute ("color");
			if (!String.IsNullOrEmpty (reader.GetAttribute ("ignorecase")))
				result.IgnoreCase = Boolean.Parse (reader.GetAttribute ("ignorecase"));
			List<Match> matches = new List<Match> ();
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case Node:
					return true;
				}
				return result.ReadNode (reader, matches);
			});
			result.matches = matches.ToArray ();
			result.SetupSpanTree ();
			result.SetupParseTree ();
			return result;
		}
	}
}
