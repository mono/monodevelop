// IXmlProvider.cs
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
using System.IO;
using System.Reflection;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	public interface IStreamProvider
	{
		Stream Open ();
	}

	[Obsolete("Do not use this anymore. Use ResourceStreamProvider.")]
	public class ResourceXmlProvider : ResourceStreamProvider
	{
		public ResourceXmlProvider (Assembly assembly, string manifestResourceName) : base(assembly, manifestResourceName)
		{

		}
	}
	public class ResourceStreamProvider : IStreamProvider
	{
		Assembly assembly;
		string   manifestResourceName;
		
		public string ManifestResourceName {
			get {
				return manifestResourceName;
			}
		}
		
		public Assembly Assembly {
			get {
				return assembly;
			}
		}
		
		public ResourceStreamProvider (Assembly assembly, string manifestResourceName)
		{
			this.assembly             = assembly;
			this.manifestResourceName = manifestResourceName;
		}
		
		public Stream Open ()
		{
			return assembly.GetManifestResourceStream (this.ManifestResourceName);
		}
	}
	
	public class UrlStreamProvider : IStreamProvider
	{
		string  url;
		
		public string Url {
			get {
				return url;
			}
		}
		
		public UrlStreamProvider (string url)
		{
			this.url = url;
		}
		
		public Stream Open ()
		{
			return File.OpenRead (url);
		}
	}
	
}
