// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;

namespace MonoDevelop.TextEditor.Document
{
	public class Span
	{
		bool        stopEOL;
		HighlightColor color;
		HighlightColor beginColor = null;
		HighlightColor endColor = null;
		char[]      begin = null;
		char[]      end   = null;
		string      name  = null;
		string      rule  = null;
		HighlightRuleSet ruleSet = null;
		bool        noEscapeSequences = false;
		
		internal HighlightRuleSet RuleSet {
			get {
				return ruleSet;
			}
			set {
				ruleSet = value;
			}
		}

		public bool StopEOL {
			get {
				return stopEOL;
			}
		}
		
		public HighlightColor Color {
			get {
				return color;
			}
		}
		
		public HighlightColor BeginColor {
			get {		
				if(beginColor != null) {
					return beginColor;
				} else {
					return color;
				}
			}
		}
		
		public HighlightColor EndColor {
			get {
				return endColor!=null ? endColor : color;
			}
		}
		
		public char[] Begin {
			get {
				return begin;
			}
		}
		
		public char[] End {
			get {
				return end;
			}
		}
		
		public string Name {
			get {
				return name;
			}
		}
		
		public string Rule {
			get {
				return rule;
			}
		}
		
		public bool NoEscapeSequences {
			get {
				return noEscapeSequences;
			}
		}
		
		public Span(XmlElement span)
		{
			color   = new HighlightColor(span);
			
			if (span.Attributes["rule"] != null) {
				rule = span.Attributes["rule"].InnerText;
			}
			
			if (span.Attributes["noescapesequences"] != null) {
				noEscapeSequences = Boolean.Parse(span.Attributes["noescapesequences"].InnerText);
			}
			
			name    = span.Attributes["name"].InnerText;
			stopEOL = Boolean.Parse(span.Attributes["stopateol"].InnerText);
			begin   = span["Begin"].InnerText.ToCharArray();
			beginColor = new HighlightColor(span["Begin"], color);
			
			if (span["End"] != null) {
				end  = span["End"].InnerText.ToCharArray();
				endColor = new HighlightColor(span["End"], color);
			}
		}
	}
}
