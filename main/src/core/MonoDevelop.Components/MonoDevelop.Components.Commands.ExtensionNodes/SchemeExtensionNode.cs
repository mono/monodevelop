// SchemeExtensionNode.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.IO;
using Mono.Addins;
using System.Xml;
using MonoDevelop.Core;

namespace MonoDevelop.Components.Commands.ExtensionNodes
{
	internal class SchemeExtensionNode: ExtensionNode, KeyBindingScheme
	{
		[NodeAttribute ("_name", "Name of the key bindings scheme", Localizable=true)]
		string name;

		[NodeAttribute ("file", "Name of the key bindings file")]
		string file;

		[NodeAttribute ("resource", "Name of the resource containing the key bindings file.")]
		string resource;

		KeyBindingSet cachedSet;
		
		public string Name {
			get { return name ?? Id; }
		}
		
		public string File {
			get {
				return file;
			}
		}

		public string Resource {
			get {
				return resource;
			}
		}
		
		public Stream GetKeyBindingsSchemeStream ()
		{
			if (!string.IsNullOrEmpty (file))
				return System.IO.File.OpenRead (Addin.GetFilePath (file));
			if (!string.IsNullOrEmpty (resource))
				return Addin.GetResource (resource, true);
			throw new InvalidOperationException ("File or resource name not specified");
		}

		public KeyBindingSet GetKeyBindingSet ()
		{
			if (cachedSet != null)
				return cachedSet;
			
			XmlTextReader reader = null;
			Stream stream;
			
			try {
				stream = GetKeyBindingsSchemeStream ();
				if (stream != null) {
					reader = new XmlTextReader (stream);
					cachedSet = new KeyBindingSet (KeyBindingService.DefaultKeyBindingSet);
					cachedSet.LoadScheme (reader, Id);
					return cachedSet;
				}
			} catch (Exception e) {
				LoggingService.LogError (e.ToString ());
			} finally {
				if (reader != null)
					reader.Close ();
			}
			return null;
		}
	}
}
