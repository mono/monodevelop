//
// PlatformService.cs
//
// Author:
//   Geoff Norton  <gnorton@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Utilities;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Extensions;

namespace MonoDevelop.Ide.Desktop
{
	class MimeTypes
	{
		public static MimeTypes Instance { get; } = new MimeTypes ();

		List<MimeTypeNode> mimeTypeNodes = new List<MimeTypeNode> ();

		MimeTypes ()
		{
			if (AddinManager.IsInitialized) {
				AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/MimeTypes", delegate (object sender, ExtensionNodeEventArgs args) {
					var newList = new List<MimeTypeNode> (mimeTypeNodes);
					var mimeTypeNode = (MimeTypeNode)args.ExtensionNode;
					switch (args.Change) {
					case ExtensionChange.Add:
						// initialize child nodes.
						mimeTypeNode.ChildNodes.GetEnumerator ();
						newList.Add (mimeTypeNode);
						break;
					case ExtensionChange.Remove:
						newList.Remove (mimeTypeNode);
						break;
					}
					mimeTypeNodes = newList;
				});
			}
		}

		public MimeTypeNode FindMimeType (string type)
		{
			foreach (MimeTypeNode mt in mimeTypeNodes) {
				if (mt.Id == type)
					return mt;
			}
			return null;
		}

		Lazy<IFileToContentTypeService> fileToContentTypeService = CompositionManager.GetExport<IFileToContentTypeService> ();

		public MimeTypeNode FindMimeTypeForFile (string fileName)
		{
			try {
				IContentType contentType = fileToContentTypeService.Value.GetContentTypeForFilePath (fileName);
				if (contentType != PlatformCatalog.Instance.ContentTypeRegistryService.UnknownContentType) {
					string mimeType = PlatformCatalog.Instance.MimeToContentTypeRegistryService.GetMimeType (contentType);
					if (mimeType != null) {
						MimeTypeNode mt = FindMimeType (mimeType);
						if (mt != null) {
							return mt;
						}
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError ("IFilePathToContentTypeProvider query failed", ex);
			}

			foreach (MimeTypeNode mt in mimeTypeNodes) {
				if (mt.SupportsFile (fileName))
					return mt;
			}
			return null;
		}

		public bool GetMimeTypeIsText (string mimeType)
		{
			return GetMimeTypeIsSubtype (mimeType, "text/plain");
		}

		public bool GetMimeTypeIsSubtype (string subMimeType, string baseMimeType)
		{
			foreach (string mt in GetMimeTypeInheritanceChain (subMimeType))
				if (mt == baseMimeType)
					return true;
			return false;
		}

		public IEnumerable<string> GetMimeTypeInheritanceChain (string mimeType)
		{
			yield return mimeType;

			while (mimeType != null && mimeType != "text/plain" && mimeType != "application/octet-stream") {
				MimeTypeNode mt = FindMimeType (mimeType);
				if (mt != null && !string.IsNullOrEmpty (mt.BaseType))
					mimeType = mt.BaseType;
				else {
					if (mimeType.EndsWith ("+xml", StringComparison.Ordinal))
						mimeType = "application/xml";
					else if (mimeType.StartsWith ("text/", StringComparison.Ordinal))
						mimeType = "text/plain";
					else
						break;
				}
				yield return mimeType;
			}
		}

		public string GetMimeTypeForRoslynLanguage (string roslynLanguage)
		{
			foreach (var mt in mimeTypeNodes) {
				if (mt.RoslynName == roslynLanguage)
					return mt.Id;
			}
			return null;
		}

		public IEnumerable<string> GetMimeTypeInheritanceChainForRoslynLanguage (string roslynLanguage)
		{
			var mime = GetMimeTypeForRoslynLanguage (roslynLanguage);
			if (mime == null) {
				return null;
			}
			return GetMimeTypeInheritanceChain (mime);
		}
	}
}
