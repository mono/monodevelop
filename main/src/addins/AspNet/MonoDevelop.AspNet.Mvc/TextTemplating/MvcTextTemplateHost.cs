// 
// MvcTextTemplateHost.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.TextTemplating;

namespace MonoDevelop.AspNet.Mvc.TextTemplating
{
	public class MvcTextTemplateHost : MonoDevelopTemplatingHost
	{
		public MvcTextTemplateHost ()
		{
			Imports.Add ("MonoDevelop.AspNet.Mvc.TextTemplating");
			Refs.Add (typeof (MvcTextTemplateHost).Assembly.Location);
		}
		
		public string ItemName { get; set; }
		public string NameSpace { get; set; }
		
		#region Controller
		
		public string ExtraActionMethods { get; set; }
		public bool ControllerRootName { get; set; }
		
		#endregion
		
		#region View
		
		public bool IsViewUserControl { get; set; }
		
		public bool IsViewContentPage { get; set; }
		
		public bool IsViewPage { get; set; } 
		
		public string MasterPage { get; set; }
		
		public string ContentPlaceholder { get; set; }
		
		public List<System.String> ContentPlaceHolders  { get; set; }
		
		public string LanguageExtension { get; set; }
		
		public string ViewDataTypeString { get; set; }

		public string ViewDataTypeGenericString {
			get	{ return String.IsNullOrEmpty (ViewDataTypeString) ? "" : "<" + ViewDataTypeString + ">"; }
		}
		
		public Type ViewDataType { get; set; }
		
		#endregion
	}
}
