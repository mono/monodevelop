// StringRegistry.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;

namespace MonoDevelop.Projects
{
	public static class StringRegistry
	{
		static Dictionary<long, string> strings = new Dictionary<long, string> ();
		static Dictionary<string, long> ids     = new Dictionary<string, long> ();
		
		static long CalculateHash (string str)
		{
			// CONSIDER: maybe implement a real checksum algorithm here
			return str.GetHashCode ();
		}
		
		public static long GetId (string str)
		{
			if (String.IsNullOrEmpty (str))
				return -1;
			if (!ids.ContainsKey (str)) {
				lock (ids) {
					ids[str] = CalculateHash (str);
				}
			}
			return ids[str];
		}
		
		public static string GetString (long id)
		{
			if (id == -1)
				return "";
			string result;
			strings.TryGetValue (id, out result);
			return result;
		}
	}
	
	
}
