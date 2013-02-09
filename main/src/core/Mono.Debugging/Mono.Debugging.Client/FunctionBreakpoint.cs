// 
// FunctionBreakpoint.cs
//  
// Author: Jeffrey Stedfast <jeff@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using System.Xml;
using System.Collections.Generic;

namespace Mono.Debugging.Client
{
	[Serializable]
	public class FunctionBreakpoint : Breakpoint
	{
		public FunctionBreakpoint (string functionName, string language) : base (null, 1, 1)
		{
			FunctionName = functionName;
			Language = language;
		}

		static bool SkipArrayRank (string text, ref int index, int endIndex)
		{
			char c;

			while (index < endIndex) {
				if ((c = text[index++]) == ']')
					return true;

				if (c != ',' && !char.IsWhiteSpace (c) && !char.IsDigit (c))
					return false;
			}

			return false;
		}

		static bool SkipGenericArgs (string text, ref int index, int endIndex)
		{
			char c;

			while (index < endIndex) {
				if ((c = text[index++]) == '>')
					return true;

				if (c == '<') {
					if (!SkipGenericArgs (text, ref index, endIndex))
						return false;

					while (index < endIndex && !char.IsWhiteSpace (text[index]))
						index++;

					// the only chars allowed after a '>' are: '>', '[', and ','
					if (">[,".IndexOf (text[index]) == -1)
						return false;
				} else if (c == '[') {
					if (!SkipArrayRank (text, ref index, endIndex))
						return false;

					while (index < endIndex && !char.IsWhiteSpace (text[index]))
						index++;

					// the only chars allowed after a ']' are: '>', '[', and ','
					if (">[,".IndexOf (text[index]) == -1)
						return false;
				} else if (c != '.' && c != '+' && c != ',' && !char.IsWhiteSpace (c) && !char.IsLetterOrDigit (c)) {
					return false;
				}
			}

			return false;
		}

		public static bool TryParseParameters (string text, int startIndex, int endIndex, out string[] paramTypes)
		{
			List<string> parsedParamTypes = new List<string> ();
			int index = startIndex;
			int start;
			char c;

			paramTypes = null;

			while (index < endIndex) {
				while (char.IsWhiteSpace (text[index]))
					index++;

				start = index;
				while (index < endIndex) {
					if ((c = text[index]) == ',')
						break;

					index++;

					if (c == '<') {
						if (!SkipGenericArgs (text, ref index, endIndex))
							return false;
					} else if (c == '[') {
						if (!SkipArrayRank (text, ref index, endIndex))
							return false;
					}
				}

				parsedParamTypes.Add (text.Substring (start, index - start).TrimEnd ());

				if (index < endIndex)
					index++;
			}

			paramTypes = parsedParamTypes.ToArray ();

			return true;
		}
		
		internal FunctionBreakpoint (XmlElement elem) : base (elem)
		{
			FunctionName = elem.GetAttribute ("function");
			
			string text = elem.GetAttribute ("language");
			if (string.IsNullOrEmpty (text))
				Language = "C#";
			else
				Language = text;

			if (elem.HasAttribute ("params")) {
				string[] paramTypes;

				text = elem.GetAttribute ("params").Trim ();
				if (text.Length > 0 && TryParseParameters (text, 0, text.Length, out paramTypes))
					ParamTypes = paramTypes;
				else
					ParamTypes = new string[0];
			}

			FileName = null;
		}
		
		internal override XmlElement ToXml (XmlDocument doc)
		{
			XmlElement elem = base.ToXml (doc);
			
			elem.SetAttribute ("function", FunctionName);
			elem.SetAttribute ("language", Language);
			
			if (ParamTypes != null)
				elem.SetAttribute ("params", string.Join (", ", ParamTypes));
			
			return elem;
		}
		
		public string FunctionName {
			get; set;
		}
		
		public string Language {
			get; set;
		}
		
		public string[] ParamTypes {
			get; set;
		}
		
		public override void CopyFrom (BreakEvent ev)
		{
			base.CopyFrom (ev);
			
			FunctionBreakpoint bp = (FunctionBreakpoint) ev;
			FunctionName = bp.FunctionName;
			ParamTypes = bp.ParamTypes;
		}
	}
}
