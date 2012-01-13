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

namespace Mono.Debugging.Client
{
	[Serializable]
	public class FunctionBreakpoint : Breakpoint
	{
		public FunctionBreakpoint (string functionName, string language) : base (null, 1/*, 1*/)
		{
			FunctionName = functionName;
			Language = language;
		}
		
		internal FunctionBreakpoint (XmlElement elem) : base (elem)
		{
			FunctionName = elem.GetAttribute ("function");
			
			string s = elem.GetAttribute ("language");
			if (string.IsNullOrEmpty (s))
				Language = "C#";
			else
				Language = s;
			
			s = elem.GetAttribute ("params");
			if (!string.IsNullOrEmpty (s))
				ParamTypes = s.Split (new char [] { ',' });
		}
		
		internal override XmlElement ToXml (XmlDocument doc)
		{
			XmlElement elem = base.ToXml (doc);
			
			elem.SetAttribute ("function", FunctionName);
			elem.SetAttribute ("language", Language);
			
			if (ParamTypes != null)
				elem.SetAttribute ("params", string.Join (",", ParamTypes));
			
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
		
		public void SetResolvedFileName (string resolvedFileName)
		{
			FileName = resolvedFileName;
		}
		
		public override void CopyFrom (BreakEvent ev)
		{
			base.CopyFrom (ev);
			
			FunctionBreakpoint bp = (FunctionBreakpoint) ev;
			FunctionName = bp.FunctionName;
		}
	}
}
