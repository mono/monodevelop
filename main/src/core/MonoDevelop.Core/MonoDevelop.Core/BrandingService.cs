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
using System.Xml.Linq;
using System.Runtime.CompilerServices;

namespace MonoDevelop.Core
{
	/// <summary>
	/// Access to branding information. Only the ApplicationName is guaranteed to be non-null.
	/// </summary>
	public static class BrandingService
	{
		static FilePath brandingDir;
		static FilePath localizedBrandingDir;
		static XDocument brandingDocument;
		static XDocument localizedBrandingDocument;
		
		public static readonly string ApplicationName;
		public static readonly string SuiteName;
		public static readonly string ProfileDirectoryName;
		
		static BrandingService ()
		{
			try {
				FilePath asmPath = typeof (BrandingService).Assembly.Location;
				brandingDir = asmPath.ParentDirectory.Combine ("branding");
				if (!Directory.Exists (brandingDir)) {
					brandingDir = null;
				} else {
					var langCode = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
					localizedBrandingDir = brandingDir.Combine (langCode);
					if (!Directory.Exists (localizedBrandingDir)) {
						localizedBrandingDir = null;
					}
				}
				
				//read the files after detecting both directories, in case there's an error
				if (brandingDir != null) { 
					var brandingFile = brandingDir.Combine ("Branding.xml");
					if (File.Exists (brandingFile)) {
						brandingDocument = XDocument.Load (brandingFile);
					}
					if (localizedBrandingDir != null) {
						var localizedBrandingFile = brandingDir.Combine ("Branding.xml");
						if (File.Exists (localizedBrandingFile)) {
							localizedBrandingDocument = XDocument.Load (localizedBrandingFile);
						}
					}
				}
				ApplicationName = GetString ("ApplicationName");
				SuiteName = GetString ("SuiteName");
				ProfileDirectoryName = GetString ("ProfileDirectoryName");
			} catch (Exception ex) {
				LoggingService.LogError ("Could not read branding document", ex);
			}
			
			if (string.IsNullOrEmpty (ApplicationName))
				ApplicationName = "MonoDevelop";

			if (string.IsNullOrEmpty (SuiteName))
				SuiteName = ApplicationName;

			if (string.IsNullOrEmpty (ProfileDirectoryName))
				ProfileDirectoryName = ApplicationName;
		}
		
		public static string GetString (params string[] keyPath)
		{
			var el = GetElement (keyPath);
			return el == null? null : (string) el;
		}
		
		public static int? GetInt (params string[] keyPath)
		{
			return (int?) GetElement (keyPath);
		}
		
		public static bool? GetBool (params string[] keyPath)
		{
			return (bool?) GetElement (keyPath);
		}
		
		public static XElement GetElement (params string[] keyPath)
		{
			if (keyPath == null)
				throw new ArgumentNullException ();
			if (keyPath.Length == 0)
				throw new ArgumentException ();
			
			if (localizedBrandingDocument != null) {
				var el = GetElement (localizedBrandingDocument, keyPath);
				if (el != null)
					return el;
			}
			
			if (brandingDocument != null) {
				var el = GetElement (brandingDocument, keyPath);
				if (el != null)
					return el;
			}
			return null;
		}
		
		static XElement GetElement (XDocument doc, string[] keyPath)
		{
			int idx = 0;
			XElement el = doc.Root;
			do {
				el = el.Element (keyPath[idx++]);
			} while (idx < keyPath.Length && el != null);
			return el;
		}
		
		[MethodImpl (MethodImplOptions.NoInlining)]
		public static FilePath GetFile (string name)
		{
			if (localizedBrandingDir != null) {
				var file = localizedBrandingDir.Combine (name);
				if (File.Exists (file))
					return file;
			}
			
			if (brandingDir != null) {
				var file = brandingDir.Combine (name);
				if (File.Exists (file))
					return file;
			}
			
			return null;
		}
		
		[MethodImpl (MethodImplOptions.NoInlining)]
		public static Stream GetStream (string name, bool lookInCallingAssembly=false)
		{
			//read branding directory, then calling assembly's resources
			var file = GetFile (name);
			if (file != null)
				return File.OpenRead (file);
			
			if (lookInCallingAssembly)
				return Assembly.GetCallingAssembly ().GetManifestResourceStream (name);
			
			return null;
		}

		public static string BrandApplicationName (string s)
		{
			return s.Replace ("MonoDevelop", ApplicationName);
		}
	}
}