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
		protected Span[] spans = new Span[0];
		protected Match[] matches = new Match[0];
		protected Marker[] prevMarker = new Marker[0];
		
		public List<SemanticRule> SemanticRules = new List<SemanticRule> ();
		
		protected Dictionary<string, List<string>> properties = new Dictionary<string, List<string>> ();
		
		public Dictionary<string, List<string>> Properties {
			get {
				return properties;
			}
		}
		
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
		
		public Span[] Spans {
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
			protected set;
		}
		
		string delimiter;
		public string Delimiter {
			get { 
				return string.IsNullOrEmpty (delimiter) ? mode.Delimiter : delimiter; 
			}
			protected set { delimiter = value; }
		}

		public Marker[] PrevMarker {
			get {
				return prevMarker;
			}
		}
		
		SyntaxMode mode;
		
		public Rule (SyntaxMode mode)
		{
			this.mode = mode;
		}
		
		public virtual Rule GetRule (string name)
		{
			return mode.GetRule (name);
		}
		
		public override string ToString ()
		{
			return String.Format ("[Rule: Name={0}, #Keywords={1}]", Name, keywords.Count);
		}
		
		public class KeyTable 
		{
			public Keywords   keywords = null;
			public KeyTable[] table    = new KeyTable[tableLength];
		}
		const int tableLength = 255;
		protected KeyTable[] table = new KeyTable[tableLength];
		public KeyTable[] Table {
			get {
				return table;
			}
		}
		void AddToTable (Keywords keywords, char[] word)
		{
			KeyTable[] curTable = table;
			for (int i = 0; i < word.Length; i++) {
				uint idx = (uint)word[i];
				if (idx >= tableLength)
					throw new ArgumentOutOfRangeException (word + " contains invalid chars.");
				if (curTable[idx] == null)
					curTable[idx] = new KeyTable ();
				if (i == word.Length -1) {
					curTable[idx].keywords = keywords;
					break;
				}
				curTable = curTable[idx].table;
			}
		}
		
		public Keywords GetKeyword (Document doc, int offset, int length)
		{
			KeyTable[] curTable = table;
			int max = offset + length - 1;
			uint idx;
			for (int i = offset; i < max; i++) {
				idx = (uint)(IgnoreCase ? Char.ToUpper (doc.GetCharAt (i)) : doc.GetCharAt (i));
				if (idx >= tableLength || curTable[idx] == null)
					return null;
				curTable = curTable[idx].table;
			}
			idx = (uint)(IgnoreCase ? Char.ToUpper (doc.GetCharAt (max)) : doc.GetCharAt (max));
			if (idx >= tableLength || curTable[idx] == null)
				return null;
			return curTable[idx].keywords;
		}
		
		
		protected bool ReadNode (XmlReader reader, List<Match> matchList, List<Span> spanList, List<Marker> prevMarkerList)
		{
			switch (reader.LocalName) {
			case "Delimiters":
				this.Delimiter = reader.ReadElementString ();
				return true;
			case "Property":
				string name  = reader.GetAttribute ("name");
				string value = reader.ReadElementString ();
				
				if (!properties.ContainsKey (name))
					properties[name] = new List<string> ();
				properties[name].Add (value);
				return true;
			case Match.Node:
				matchList.Add (Match.Read (reader));
				return true;
			case Span.Node:
			case Span.AltNode:
				spanList.Add (Span.Read (reader));
				return true;
			case Mono.TextEditor.Highlighting.Keywords.Node:
				Keywords keywords = Mono.TextEditor.Highlighting.Keywords.Read (reader, IgnoreCase);
				this.keywords.Add (keywords);
				foreach (string word in keywords.Words) {
					AddToTable (keywords, (keywords.IgnoreCase ? word.ToUpper () :  word).ToCharArray ());
				}
				return true;
			case Marker.PrevMarker:
				prevMarkerList.Add (Marker.Read (reader));
				return true;
			}
			return false;
		}
		
//		public class Pair<S, T>
//		{
//			public S o1;
//			public T o2;
//			
//			public Pair (S o1, T o2)
//			{
//				this.o1 = o1;
//				this.o2 = o2;
//			}
//			
//			public override string ToString ()
//			{
//				return String.Format ("[Pair: o1={0}, o2={1}]", o1, o2);
//			}
//		}
//		public Dictionary<char, Pair<Keywords, object>> parseTree  = new Dictionary<char, Pair<Keywords, object>> ();
//		
//		void AddToParseTree (string key, Keywords value)
//		{
//			Dictionary<char, Pair<Keywords, object>> tree = parseTree;
//			Pair<Keywords, object> pair = null;
//			
//			for (int i = 0; i < key.Length; i++) {
//				if (!tree.ContainsKey (key[i]))
//					tree.Add (key[i], new Pair<Keywords, object> (null, new Dictionary<char, Pair<Keywords, object>>()));
//				pair = tree[key[i]];
//				tree = (Dictionary<char, Pair<Keywords, object>>)pair.o2;
//			}
//			pair.o1 = value;
//		}
//		
//		void AddWord (Keywords kw, string word, int i)
//		{
//			AddToParseTree (word, kw);
//			char[] chars = word.ToCharArray ();
//			
//			if (i < word.Length) {
//				chars[i] = Char.ToUpperInvariant (chars[i]);
//				AddWord (kw, new string (chars), i + 1);
//				
//				chars[i] = Char.ToLowerInvariant (chars[i]);
//				AddWord (kw, new string (chars), i + 1);
//			}
//		}
//		
//		protected void SetupParseTree ()
//		{
//			foreach (Keywords kw in this.keywords) {  
//				foreach (string word in kw.Words)  {
//					if (kw.IgnoreCase) {
//						AddWord (kw, word.ToLowerInvariant (), 0);
//					} else {
//						AddToParseTree (word, kw);
//					}
//				}
//			}
//		}
		
		public const string Node = "Rule";
		public static Rule Read (SyntaxMode mode, XmlReader reader, bool ignoreCaseDefault)
		{
			Rule result = new Rule (mode);
			result.Name         = reader.GetAttribute ("name");
			result.DefaultColor = reader.GetAttribute ("color");
			result.IgnoreCase   = ignoreCaseDefault;
			if (!String.IsNullOrEmpty (reader.GetAttribute ("ignorecase")))
				result.IgnoreCase = Boolean.Parse (reader.GetAttribute ("ignorecase"));
			List<Match> matches = new List<Match> ();
			List<Span> spanList = new List<Span> ();
			List<Marker> prevMarkerList = new List<Marker> ();
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case Node:
					return true;
				}
				return result.ReadNode (reader, matches, spanList, prevMarkerList);
			});
			result.matches = matches.ToArray ();
			result.spans   = spanList.ToArray ();
			result.prevMarker = prevMarkerList.ToArray ();
			return result;
		}
	}
}
