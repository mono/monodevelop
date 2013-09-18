//
// CodeSegment.cs
//
// Author:
//       Diyoda Sajjana <>
//
// Copyright (c) 2013 Diyoda Sajjana
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
using MonoDevelop.CSSBinding.Parse.Interfaces;

namespace MonoDevelop.CSSBinding.Parse.Models
{
	public class CodeSegment : ISegment
	{
		private string _text ;

		public CodeSegment(string text, Location start, Location end)
		{
			this.StartLocation = start;
			this.Text = text;
			this.EndLocation = end;
		}

		public CodeSegment(string text, Location start, Location end , CodeSegmentType type)
		{
			this.StartLocation = start;
			this.Text = text;
			this.EndLocation = end;
			this.Type = type;
		}


		public string Text { 
			get{

				if(_text !=null )
					return _text;
				else return "Not Defined";
			} 
			private set{ 
				_text = GetFoldingStringTag (value);
			} }
		public CodeSegmentType Type { get; set; }
		public Location TagStartLocation { get; set; }
		public Location StartLocation { get; private set; }
		public Location EndLocation { get; set; }

		private string GetFoldingStringTag(string fullString)
		{
			return fullString.Split(new char[]{'{'} ).GetValue(0).ToString() +"{..." ;
		}
	}

	public enum CodeSegmentType
	{
		CSSElement,
		Comment

	}
}

