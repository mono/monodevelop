// ImageService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using Mono.Addins;
using MonoDevelop.Core.Gui.Codons;
using System.IO;

namespace MonoDevelop.Core.Gui
{
	public static class ImageService
	{
		static Gtk.IconFactory iconFactory = new Gtk.IconFactory ();
		static Dictionary<string, string> stockMappings = new Dictionary<string, string> ();
		static List<Dictionary<string, string>> addinIcons = new List<Dictionary<string, string>> ();
		static List<RuntimeAddin> addins = new List<RuntimeAddin> ();
		static Dictionary<string,string> composedIcons = new Dictionary<string,string> ();
		
		static ImageService ()
		{
			iconFactory.AddDefault ();
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/StockIcons", delegate(object sender, ExtensionNodeEventArgs args) {
				StockIconCodon iconCodon = (StockIconCodon)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					if (!string.IsNullOrEmpty (iconCodon.Resource)) {
						using (System.IO.Stream stream = iconCodon.Addin.GetResource (iconCodon.Resource)) {
							if (stream != null)
								AddToIconFactory (iconCodon.StockId, new Gdk.Pixbuf (stream), iconCodon.IconSize);
						}
					} else if (!string.IsNullOrEmpty (iconCodon.IconId)) {
						AddToIconFactory (iconCodon.StockId, GetPixbuf (InternalGetStockId (args.ExtensionNode.Addin, iconCodon.IconId), iconCodon.IconSize), iconCodon.IconSize);
					}
					break;
				}
			});
		}
		
		public static void Initialize ()
		{
			//forces static constructor to run
		}
		
		public static Gdk.Pixbuf MakeTransparent (Gdk.Pixbuf icon, double opacity)
		{
			Gdk.Pixbuf gicon = icon.Copy ();
			gicon.Fill (0);
			gicon = gicon.AddAlpha (true, 0, 0, 0);
			icon.Composite (gicon, 0, 0, icon.Width, icon.Height, 0, 0, 1, 1, Gdk.InterpType.Bilinear, (int)(256 * opacity));
			return gicon;
		}
		
		public static Gdk.Pixbuf GetPixbuf (string name)
		{
			return GetPixbuf (name, Gtk.IconSize.Button);
		}
		
		public static Gdk.Pixbuf GetPixbuf (string name, Gtk.IconSize size)
		{
			if (string.IsNullOrEmpty (name)) {
				LoggingService.LogWarning ("Empty icon requested. Stack Trace: " + Environment.NewLine + Environment.StackTrace);
				return GetColourBlock ("#FF0000", size);
			}
			
			//if an icon name begins with '#', we assume it's a hex colour
			if (name[0] == '#')
				return GetColourBlock (name, size);
			
			string stockid = InternalGetStockId (name);
			if (string.IsNullOrEmpty (stockid)) {
				LoggingService.LogWarning ("Can't get stock id for " + name +" : " + Environment.NewLine + Environment.StackTrace);
				return GetColourBlock ("#FF0000", size);
			}
			
			Gtk.IconSet iconset = Gtk.IconFactory.LookupDefault (stockid);
			if (iconset != null) 
				return iconset.RenderIcon (Gtk.Widget.DefaultStyle, Gtk.TextDirection.None, Gtk.StateType.Normal, size, null, null);
			
			if (Gtk.IconTheme.Default.HasIcon (stockid)) {
				int w, h;
				Gtk.Icon.SizeLookup (size, out w, out h);
				Gdk.Pixbuf result = Gtk.IconTheme.Default.LoadIcon (stockid, h, (Gtk.IconLookupFlags) 0);
				return result;
			}
			LoggingService.LogWarning ("Can't lookup icon: " + name);
			return GetColourBlock ("#FF0000FF", size);
		}
		
		static Gdk.Pixbuf GetColourBlock (string name, Gtk.IconSize size)
		{
			int w, h;
			if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out w, out h))
				w = h = 22;
			Gdk.Pixbuf result = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, w, h);
			uint color;
			if (!TryParseColor (name, out color))
				color = 0xFFFFFF00;
			result.Fill (color);
			return result;
		}
		
		static bool TryParseColor (string colorString, out uint val)
		{
			Gdk.Color color = new Gdk.Color ();
			if (Gdk.Color.Parse (colorString, ref color))  {
				val = color.Pixel;
				return true;
			}
			val = 0;
			return false;
		}
		
		public static Gtk.Image GetImage (string name, Gtk.IconSize size)
		{
			string stock = GetStockId (name);
			if (stock != null)
				return new Gtk.Image (stock, size);
			return new Gtk.Image (GetPixbuf (name, size));
		}
		
		public static string GetStockIdFromResource (RuntimeAddin addin, string id)
		{
			return InternalGetStockIdFromResource (addin, id);
		}

		public static string GetStockId (string filename)
		{
			return InternalGetStockId (filename);
		}

		public static string GetStockId (RuntimeAddin addin, string filename)
		{
			return InternalGetStockId (addin, filename);
		}

		internal static void AddToIconFactory (string stockId, string filename, Gtk.IconSize iconSize)
		{
			try {
				Gtk.IconSet iconSet = iconFactory.Lookup (stockId);
				if (iconSet == null) {
					iconSet = new Gtk.IconSet ();
					iconFactory.Add (stockId, iconSet);
				}

				Gtk.IconSource source = new Gtk.IconSource ();
				source.Filename = Path.GetFullPath(Path.Combine (Path.Combine (Path.Combine ( Path.Combine ("..", "data"), "resources"), "icons"), filename));
				source.Size = iconSize;
				iconSet.AddSource (source);
				stockMappings.Add (filename, stockId);

			}
			catch (GLib.GException) {
				// just discard the exception, the icon simply can't be
				// loaded
				LoggingService.LogWarning (typeof(ImageService).ToString() + " can't load " + filename + " icon file");
			}
		}
		
		public static void AddToIconFactory (string stockId, Gdk.Pixbuf pixbuf, Gtk.IconSize iconSize)
		{
			Gtk.IconSet iconSet = iconFactory.Lookup (stockId);
			if (iconSet == null) {
				iconSet = new Gtk.IconSet ();
				iconFactory.Add (stockId, iconSet);
			}

			Gtk.IconSource source = new Gtk.IconSource ();
			source.Pixbuf = pixbuf;
			source.Size = iconSize;
			if (iconSize == Gtk.IconSize.Invalid)
				source.SizeWildcarded = true;
			else
				source.SizeWildcarded = false;
			iconSet.AddSource (source);
		}
		
		static string InternalGetStockIdFromResource (RuntimeAddin addin, string id)
		{
			if (!id.StartsWith ("res:"))
				return id;
			
			id = id.Substring (4);
			int aid = addins.IndexOf (addin);
			Dictionary<string, string> hash;
			if (aid == -1) {
				aid = addins.Count;
				addins.Add (addin);
				hash = new Dictionary<string, string> ();
				addinIcons.Add (hash);
			} else {
				hash = addinIcons [aid];
			}
			string sid = "__asm" + aid + "__" + id;
			if (!hash.ContainsKey (sid)) {
				System.IO.Stream s = addin.GetResource (id);
				if (s != null) {
					using (s) {
						Gdk.Pixbuf pix = new Gdk.Pixbuf (s);
						AddToIconFactory (sid, pix, Gtk.IconSize.Invalid);
					}
				}
				hash [sid] = sid;
			}
			return sid;
		}
		
		static string GetComposedIcon (string[] ids)
		{
			string id = string.Join ("_", ids);
			string cid;
			if (composedIcons.TryGetValue (id, out cid))
				return cid;
			
			foreach (object o in Enum.GetValues (typeof(Gtk.IconSize))) {
				Gtk.IconSize sz = (Gtk.IconSize) o;
				if (sz == Gtk.IconSize.Invalid)
					continue;
				Gdk.Pixbuf icon = null;
				for (int n=0; n<ids.Length; n++) {
					Gdk.Pixbuf px = GetPixbuf (ids[n], sz);
					if (px != null)
						icon = MergeIcons (icon, px);
					else {
						icon = null;
						break;
					}
				}
				if (icon != null)
					AddToIconFactory (id, icon, sz);
			}
			composedIcons [id] = id;
			return id;
		}
		
		static Gdk.Pixbuf MergeIcons (Gdk.Pixbuf icon1, Gdk.Pixbuf icon2)
		{
			if (icon1 == null)
				return icon2;
			if (icon2 == null)
				return icon1;
			Gdk.Pixbuf res = new Gdk.Pixbuf (icon1.Colorspace, icon1.HasAlpha, icon1.BitsPerSample, icon1.Width, icon1.Height);
			res.Fill (0);
			icon1.CopyArea (0, 0, icon1.Width, icon1.Height, res, 0, 0);
			icon2.Composite (res, 0,  0, icon2.Width, icon2.Height, 0,  0, 1, 1, Gdk.InterpType.Bilinear, 255);
			return res;
		}

		internal static void AddToIconFactory (string stockId, string filename)
		{
			AddToIconFactory (stockId, filename, Gtk.IconSize.Invalid);
		}
		
		internal static void AddDefaultStockMapping (string stockFile, string nativeStock)
		{
			stockMappings.Add (stockFile, nativeStock);
		}

		internal static string InternalGetStockId (string filename)
		{
			return InternalGetStockId (null, filename);
		}
		
		internal static string InternalGetStockId (RuntimeAddin addin, string filename)
		{
			if (filename.IndexOf ('|') == -1)
				return PrivGetStockId (addin, filename);
			
			string[] parts = filename.Split ('|');
			for (int n=0; n<parts.Length; n++) {
				parts [n] = PrivGetStockId (addin, parts[n]);
			}
			return GetComposedIcon (parts);
		}
		
		static string PrivGetStockId (RuntimeAddin addin, string filename)
		{
			if (addin != null && filename.StartsWith ("res:"))
				return InternalGetStockIdFromResource (addin, filename);
				
			string result;
			if (stockMappings.TryGetValue (filename, out result))
				return result;
			
			return filename;
		}
	}
}
