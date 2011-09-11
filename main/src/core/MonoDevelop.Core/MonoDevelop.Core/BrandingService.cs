// 
// BrandingService.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using System.Reflection;
using System.IO;
using System.Xml;


namespace MonoDevelop.Core
{
	/// <summary>
	/// Access to branding information. Only the ApplicationName is guaranteed to be non-null.
	/// </summary>
	public static class BrandingService
	{
		static FilePath brandingDir;
		
		public static readonly XmlDocument BrandingDocument;
		public static readonly string ApplicationName;
		
		static BrandingService ()
		{
			try {
				FilePath asmPath = typeof (BrandingService).Assembly.Location;
				brandingDir = asmPath.ParentDirectory.Combine ("branding");
				if (!Directory.Exists (brandingDir))
					brandingDir = null;
				
				using (var stream = OpenStream ("BrandingData.xml")) {
					if (stream != null) {
						var doc = new XmlDocument ();
						doc.Load (stream);
						BrandingDocument = doc;
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Could not read branding document", ex);
				ApplicationName = "MonoDevelop";
			}
			
			try {
				ApplicationName = BrandingDocument["ApplicationName"].GetAttribute ("value");
			} catch (Exception e) {
				LoggingService.LogError ("Error while reading application name from branding xml.", e);
			}
		}
		
		public static Stream OpenStream (string name)
		{
			//read file first, fall back to resources
			if (brandingDir != null) {
				var file = brandingDir.Combine (name);
				if (File.Exists (file))
					return File.OpenRead (file);
			}
			return Assembly.GetEntryAssembly ().GetManifestResourceStream (name);
		}
	}
}