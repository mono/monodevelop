﻿//
// RazorUtils.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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

using System.Linq;
using System.Web.Razor.Parser.SyntaxTree;

namespace MonoDevelop.AspNet.Mvc.Parser
{
	public static class RazorUtils
	{
		public static  string GetShortName (Block block)
		{
			string name = "Razor code...";
			if (block.Children.Count () > 1) {
				var node = block.Children.ElementAt (1) as Span;
				if (node != null) {
					string sym = node.Symbols.ElementAt (0).Content;
					name = "@" + sym + "";
					if (sym == "{")
						name += "}";
					else if (sym == "(")
						name += ")";
					else if (sym == "*")
						name = "@* comment";
				}
			}
			return name;
		}
	}
}
