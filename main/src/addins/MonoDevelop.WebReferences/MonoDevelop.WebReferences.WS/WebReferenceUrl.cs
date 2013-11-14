// 
// WebReferenceUrl.cs
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

using MonoDevelop.Projects;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.WebReferences.WS
{
	public class WebReferenceUrl: ProjectItem
	{
		public WebReferenceUrl ()
		{
			UrlBehavior = "Dynamic";
		}
		
		public WebReferenceUrl (string url): this ()
		{
			Include = url;
			UpdateFromURL = url;
		}
		
		[ItemProperty]
		public string Include { get; private set; }
		
		[ItemProperty]
		public string UrlBehavior { get; set; }
		
		[ProjectPathItemProperty]
		public FilePath RelPath { get; set; }
		
		[ItemProperty]
		public string UpdateFromURL { get; private set; }
		
		[ItemProperty]
		public string ServiceLocationURL { get; set; }
		
		[ItemProperty]
		public string CachedDynamicPropName { get; set; }
		
		[ItemProperty]
		public string CachedAppSettingsObjectName { get; set; }
		
		[ItemProperty]
		public string CachedSettingsPropName {get; set; }
	}
}

