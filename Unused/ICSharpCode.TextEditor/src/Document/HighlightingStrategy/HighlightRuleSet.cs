// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Drawing;
using System.Text;
using System.Xml;

using MonoDevelop.TextEditor.Util;

namespace MonoDevelop.TextEditor.Document
{
	public class HighlightRuleSet
	{
		LookupTable keyWords;
		Span [] spans;
		
		LookupTable prevMarkers;
		LookupTable nextMarkers;
		IHighlightingStrategy highlighter = null;
		bool noEscapeSequences = false;
		
		bool ignoreCase = false;
		string name     = null;
		
		bool[] delimiters = new bool[256];
		
		string      reference  = null;
		
		public Span [] Spans {
			get {
				return spans;
			}
		}
		
		internal IHighlightingStrategy Highlighter {
			get {
				return highlighter;
			}
			set {
				highlighter = value;
			}
		}
		
		public LookupTable KeyWords {
			get {
				return keyWords;
			}
		}
		
		public LookupTable PrevMarkers {
			get {
				return prevMarkers;
			}
		}
		
		public LookupTable NextMarkers {
			get {
				return nextMarkers;
			}
		}
		
		public bool[] Delimiters {
			get {
				return delimiters;
			}
		}
		
		public bool NoEscapeSequences {
			get {
				return noEscapeSequences;
			}
		}
		
		public bool IgnoreCase {
			get {
				return ignoreCase;
			}
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public string Reference {
			get {
				return reference;
			}
		}
		
		public HighlightRuleSet()
		{
			keyWords    = new LookupTable(false);
			prevMarkers = new LookupTable(false);
			nextMarkers = new LookupTable(false);
		}
		
		public HighlightRuleSet(XmlElement el)
		{
			ArrayList   spans = new ArrayList ();
			XmlNodeList nodes = el.GetElementsByTagName("KeyWords");
			
			if (el.Attributes["name"] != null) {
				Name = el.Attributes["name"].InnerText;
			}
			
			if (el.Attributes["noescapesequences"] != null) {
				noEscapeSequences = Boolean.Parse(el.Attributes["noescapesequences"].InnerText);
			}
			
			if (el.Attributes["reference"] != null) {
				reference = el.Attributes["reference"].InnerText;
			}
			
			if (el.Attributes["ignorecase"] != null) {
				ignoreCase  = Boolean.Parse(el.Attributes["ignorecase"].InnerText);
			}
			
			for (int i  = 0; i < Delimiters.Length; ++i) {
				Delimiters[i] = false;
			}
			
			if (el["Delimiters"] != null) {
				string delimiterString = el["Delimiters"].InnerText;
				foreach (char ch in delimiterString) {
					Delimiters[(int)ch] = true;
				}
			}
			
			keyWords    = new LookupTable(!IgnoreCase);
			prevMarkers = new LookupTable(!IgnoreCase);
			nextMarkers = new LookupTable(!IgnoreCase);
			
			foreach (XmlElement el2 in nodes) {
				HighlightColor color = new HighlightColor(el2);
				
				XmlNodeList keys = el2.GetElementsByTagName("Key");
				foreach (XmlElement node in keys) {
					keyWords[node.Attributes["word"].InnerText] = color;
				}
			}
			
			nodes = el.GetElementsByTagName("Span");
			foreach (XmlElement el2 in nodes) {
				spans.Add(new Span(el2));
				/*
				Span span = new Span(el2);
				Spans[span.Begin] = span;*/
			}
			
			nodes = el.GetElementsByTagName("MarkPrevious");
			foreach (XmlElement el2 in nodes) {
				PrevMarker prev = new PrevMarker(el2);
				prevMarkers[prev.What] = prev;
			}
			
			nodes = el.GetElementsByTagName("MarkFollowing");
			foreach (XmlElement el2 in nodes) {
				NextMarker next = new NextMarker(el2);
				nextMarkers[next.What] = next;
			}
			
			this.spans = (Span []) spans.ToArray (typeof (Span));
		}
	}
}
