// 
// XmlNamespaceMap.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2011 Novell, Inc.
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
using System.Collections.Generic;

namespace MonoDevelop.Xml.Completion
{
	public class XmlNamespacePrefixMap
	{
		Dictionary<string,string> pfNsMap = new Dictionary<string,string> ();
		Dictionary<string,string> nsPfMap = new Dictionary<string,string> ();
		
		/// <summary>Gets the prefix registered for the namespace, empty if it's 
		/// the default namespace, or null if it's not registered.</summary>
		public string GetPrefix (string ns)
		{
			string prefix;
			if (nsPfMap.TryGetValue (ns, out prefix))
				return prefix;
			return null;
		}
		
		/// <summary>Gets the namespace registered for prefix, or default namespace if prefix is empty.</summary>
		public string GetNamespace (string prefix)
		{
			string ns;
			if (pfNsMap.TryGetValue (prefix, out ns))
				return ns;
			return null;
		}
		
		/// <summary>Registers a namespace for a prefix, or the default namespace if the prefix is empty.</summary>
		public void AddPrefix (string ns, string prefix)
		{
			nsPfMap[ns] = prefix;
			pfNsMap[prefix] = ns;
		}
	}
}

