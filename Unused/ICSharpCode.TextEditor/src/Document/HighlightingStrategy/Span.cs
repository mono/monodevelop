//  Span.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
