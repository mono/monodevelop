// 
// Util.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Mono.Addins.Description;

namespace MonoDevelop.AddinAuthoring
{
	public static class Util
	{
		public static string GetDisplayName (AddinDescription ad)
		{
			if (!string.IsNullOrEmpty (ad.Name))
				return ad.Name;
			else
				return ad.LocalId;
		}
		
		public static string GetDisplayName (ExtensionPoint ep)
		{
			if (string.IsNullOrEmpty (ep.Name))
				return ep.Path;
			else
				return ep.Name;
		}
		
		public static string GetDisplayName (Extension ext)
		{
			ObjectDescription ob = ext.GetExtendedObject ();
			string desc = "";
			string label;
			Extension lastExtension = ext;
			while (ob is ExtensionNodeDescription) {
				ExtensionNodeDescription en = (ExtensionNodeDescription) ob;
				if (desc.Length > 0)
					desc = " / " + desc;
				desc = en.Id + desc;
				ob = (ObjectDescription) en.Parent;
				if (ob is Extension) {
					lastExtension = (Extension) ob;
					ob = lastExtension.GetExtendedObject ();
				}
			}
			ExtensionPoint ep = ob as ExtensionPoint;
			if (ep != null) {
				if (!string.IsNullOrEmpty (ep.Name))
					label = ep.Name;
				else
					label = ep.Path;
			} else if (lastExtension != null) {
				label = lastExtension.Path;
			} else {
				label = "(Unknown Extension Point)";
			}
			if (!string.IsNullOrEmpty (desc))
				label += " / " + desc;
			return label;
		}
	}
}

