//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Extensions;

namespace MonoDevelop.TextEditor
{
	static class WpfImageFactory
	{
		const string IconsExtensionPath = "/MonoDevelop/Core/StockIcons";

		//runs cctor
		public static void EnsureInitialized ()
		{
		}

		static WpfImageFactory ()
		{
			Task.Run (LoadImageLibrary);
		}

		static void LoadImageLibrary ()
		{
			try {
				LoadImageCatalogCore ();
			} catch (Exception ex) {
				LoggingService.LogError ("Could not load image catalog", ex);
			}
		}

		static void LoadImageCatalogCore ()
		{
			FilePath cacheDir = Path.Combine (Environment.CurrentDirectory, "VSImageCatalog");
			string manifestFile = Path.Combine (cacheDir, "Manifest.xml");

			//FIXME: invalidate the cached manifest
			if (!File.Exists (manifestFile)) {
				BuildManifest (manifestFile, cacheDir);
			}

			//FIXME: set up the library cache storage 
			ImageLibrary.Load (manifestFile, true, new MonoDevelopTracer (nameof (WpfImageFactory), level: System.Diagnostics.SourceLevels.All));
		}

		static void BuildManifest (string manifestFile, FilePath cacheDir)
		{
			var manifest = new StringBuilder ();
			manifest.AppendLine ("<ImageManifest xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema' xmlns='http://schemas.microsoft.com/VisualStudio/ImageManifestSchema/2014'>");
			manifest.AppendLine ("<Images>");

			var images = new Dictionary<ImageId, List<StockIconCodon>> ();

			foreach (var imageNode in AddinManager.GetExtensionNodes<StockIconCodon> (IconsExtensionPath)) {
				if (imageNode.ImageId == default || (string.IsNullOrEmpty (imageNode.Resource) && string.IsNullOrEmpty (imageNode.File))) {
					continue;
				}
				if (!images.TryGetValue (imageNode.ImageId, out var list)) {
					images.Add (imageNode.ImageId, list = new List<StockIconCodon> ());
				}
				list.Add (imageNode);
			}

			foreach (var image in images) {
				manifest.AppendLine ($"\t<Image Guid='{image.Key.Guid}' ID='{image.Key.Id}'>");

				StockIconCodon wildcard = null;
				int wildcardSize = 0;
				foreach (var source in image.Value) {
					var size = GetSize (source.IconSize);
					if (size > wildcardSize) {
						wildcard = source;
					}
				}

				foreach (var source in image.Value) {
					AppendSource (manifest, source, source == wildcard, cacheDir);
				}

				manifest.AppendLine ("\t</Image>");
			}

			manifest.AppendLine ("</Images>");
			manifest.AppendLine ("</ImageManifest>");

			File.WriteAllText (manifestFile, manifest.ToString ());
		}

		static string GetDarkVariantFile (string path)
		{
			return Path.Combine (Path.GetDirectoryName (path), Path.GetFileNameWithoutExtension (path) + "~dark" + Path.GetExtension (path));
		}

		static void AppendSource (StringBuilder manifest, StockIconCodon source, bool isWildcard, string cacheDir)
		{
			var size = GetSize (source.IconSize);
			string path = source.File;
			string darkPath = null;
			if (!string.IsNullOrEmpty (source.File)) {
				path = source.Addin.GetFilePath (path);
				darkPath = GetDarkVariantFile (path);
				if (!File.Exists (darkPath)) {
					darkPath = null;
				}
			} else {
				// there is no way to load from embedded resources
				//so we have to copy them into a a cache directory
				var dir = Path.Combine (cacheDir, source.Addin.Id);
				Directory.CreateDirectory (dir);

				path = Path.Combine (dir, source.Resource);
				var resource = source.Addin.GetResource (source.Resource);
				using (var file = File.Create (path)) {
					resource.CopyTo (file);
				}

				var darkName = Path.GetFileNameWithoutExtension (source.Resource) + "~dark" + Path.GetExtension (source.Resource);
				var darkResource = source.Addin.GetResource (darkName);
				if (darkResource != null) {
					darkPath = Path.Combine (dir, darkName);
					using (var darkFile = File.Create (darkPath)) {
						darkResource.CopyTo (darkFile);
					}
				}
			}

			string MakeUrl (string s) => "pack://siteoforigin:,,," + FileService.AbsoluteToRelativePath (Environment.CurrentDirectory, s).Replace('\\', '/');

			if (isWildcard) {
				if (darkPath != null) {
					manifest.AppendLine ($"\t\t<Source Uri='{MakeUrl(path)}' Background='Light'><SizeRange MinSize='1' MaxSize='1000' /></Source>");
					manifest.AppendLine ($"\t\t<Source Uri='{MakeUrl(darkPath)}' Background='Dark'><SizeRange MinSize='1' MaxSize='1000' /></Source>");
				} else {
					manifest.AppendLine ($"\t\t<Source Uri='{MakeUrl(path)}' Background='Light'><SizeRange MinSize='1' MaxSize='1000' /></Source>");
				}
			} else {
				if (darkPath != null) {
					manifest.AppendLine ($"\t\t<Source Uri='{MakeUrl(path)}' Background='Light'><Size Value='{size}' /></Source>");
					manifest.AppendLine ($"\t\t<Source Uri='{MakeUrl(darkPath)}' Background='Dark'><Size Value='{size}' /></Source>");
				} else {
					manifest.AppendLine ($"\t\t<Source Uri='{MakeUrl(path)}' Background='Light'><Size Value='{size}' /></Source>");
				}
			}
		}

		static int GetSize (Gtk.IconSize size)
		{
			switch (size) {
			case Gtk.IconSize.Menu: return 16;
			case Gtk.IconSize.SmallToolbar: return 16;
			case Gtk.IconSize.LargeToolbar: return 22;
			case Gtk.IconSize.Button: return 20;
			case Gtk.IconSize.Dnd: return 32;
			case Gtk.IconSize.Dialog: return 48;
			}
			return 1000;
		}
	}
}
