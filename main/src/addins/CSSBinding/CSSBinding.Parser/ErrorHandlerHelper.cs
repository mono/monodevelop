//
// ErrorHandlerHelper.cs
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
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Text;

namespace CSSBinding.Parser
{
	public class ErrorHandlerHelper
	{
		public static string MakeErrorMessageFriendly(string rawErrorMessage)
		{	
			try {
				string errorMessage = rawErrorMessage;
				XDocument doc = XDocument.Load(@"..\..\src\addins\CSSBinding\CSSBinding.Parser\TokenNameMapper.xml");
				IEnumerable<XElement> tokens = doc.Root.Elements();
				// Read the entire XML
				foreach (var token in tokens)
				{
					string s1 = token.Element ("ID").Value.ToString ();
					string s2 = token.Element ("Descript").Value.ToString ();
//					Console.WriteLine("kkk :"+ s1 + " " + s2);
					errorMessage = errorMessage.Replace (token.Element ("ID").Value.ToString (), token.Element ("Descript").Value.ToString ());
				}
				return errorMessage;
			} catch (Exception e) {
				Console.WriteLine (e.Message);
				return "?";
			}


		}
	}
}

