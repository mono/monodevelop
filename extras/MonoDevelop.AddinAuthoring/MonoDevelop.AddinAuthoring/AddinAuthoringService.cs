// AddinAuthoringService.cs
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
using System.Collections.Generic;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	public static class AddinAuthoringService
	{
		public static string GetRegistryName (string regPath)
		{
			foreach (RegistryExtensionNode node in GetRegistries ()) {
				if (Path.GetFullPath (node.RegistryPath) == Path.GetFullPath (regPath))
					return node.Name;
			}
			return regPath;
		}
		
		public static IEnumerable<RegistryExtensionNode> GetRegistries ()
		{
			foreach (RegistryExtensionNode node in AddinManager.GetExtensionNodes ("MonoDevelop/AddinAuthoring/AddinRegistries"))
				yield return node;
		}
		
		internal static string NormalizeUserPath (string path)
		{
			if (path.StartsWith ("~")) {
				string absRegistryPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				return Path.Combine (absRegistryPath, path.Substring (2));
			}
			else
				return path;
		}
	}
}
