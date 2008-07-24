// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//   license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Resources;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Runtime.InteropServices;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Codons;
using Mono.Addins;
using Mono.Addins.Setup;

namespace MonoDevelop.Core.Gui.Dialogs
{
	/// <summary>
	/// This Class contains two ResourceManagers, which handle string and image resources
	/// for the application. It do handle localization strings on this level.
	/// </summary>
	internal class ImageButton : Gtk.Button
	{
		public ImageButton (string stock, string label)
		{
			Gtk.HBox hbox1 = new Gtk.HBox(false,0);
			hbox1.PackStart(new Gtk.Image(stock, Gtk.IconSize.Button), false, true, 0);
			hbox1.PackStart(new Gtk.Label(label), true, true, 0);
			this.Add(hbox1);
		}
	}
}
	
namespace MonoDevelop.Core.Gui
{
	public class ResourceService
	{
		Gtk.IconFactory iconFactory = null;
		Hashtable stockMappings = null;
		
		ArrayList addinIcons = new ArrayList ();
		ArrayList addins = new ArrayList ();
		Dictionary<string,string> composedIcons = new Dictionary<string,string> ();
		
		public ResourceService ()
		{
			iconFactory = new Gtk.IconFactory ();

			// FIXME: remove this when all MonoDevelop is using Gtk+
			// stock icons
			stockMappings = new Hashtable ();
			iconFactory.AddDefault ();

			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Core/StockIcons", OnExtensionChange);
		}
		
		void OnExtensionChange (object sender, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				StockIconCodon icon = (StockIconCodon) args.ExtensionNode;
				if (!string.IsNullOrEmpty (icon.Resource)) {
					System.IO.Stream s = icon.Addin.GetResource (icon.Resource);
					if (s != null) {
						using (s) {
							Gdk.Pixbuf px = new Gdk.Pixbuf (s);
							AddToIconFactory (icon.StockId, px, icon.IconSize);
						}
					}
				} else if (!string.IsNullOrEmpty (icon.IconId)) {
					string iid = InternalGetStockId (args.ExtensionNode.Addin, icon.IconId);
					AddToIconFactory (icon.StockId, GetIcon (iid, icon.IconSize), icon.IconSize);
				}
			}
		}
		
		static ResourceService Instance {
			get { return MonoDevelop.Core.Gui.Services.Resources; }
		}
		
		/// <summary>
		/// The LoadFont routines provide a safe way to load fonts.
		/// </summary>
		/// <param name="fontName">The name of the font to load.</param>
		/// <param name="size">The size of the font to load.</param>
		/// <returns>
		/// The font to load or the menu font, if the requested font couldn't be loaded.
		/// </returns>
		public Font LoadFont(string fontName, int size)
		{
			return LoadFont(fontName, size, FontStyle.Regular);
		}
		
		/// <summary>
		/// The LoadFont routines provide a safe way to load fonts.
		/// </summary>
		/// <param name="fontName">The name of the font to load.</param>
		/// <param name="size">The size of the font to load.</param>
		/// <param name="style">The <see cref="System.Drawing.FontStyle"/> of the font</param>
		/// <returns>
		/// The font to load or the menu font, if the requested font couldn't be loaded.
		/// </returns>
		public Font LoadFont(string fontName, int size, FontStyle style)
		{
			try {
				return new Font(fontName, size, style);
			} catch (Exception) {
				//return SystemInformation.MenuFont;
				return null;
			}
		}
		
		/// <summary>
		/// The LoadFont routines provide a safe way to load fonts.
		/// </summary>
		/// <param name="fontName">The name of the font to load.</param>
		/// <param name="size">The size of the font to load.</param>
		/// <param name="unit">The <see cref="System.Drawing.GraphicsUnit"/> of the font</param>
		/// <returns>
		/// The font to load or the menu font, if the requested font couldn't be loaded.
		/// </returns>
		public Font LoadFont(string fontName, int size, GraphicsUnit unit)
		{
			return LoadFont(fontName, size, FontStyle.Regular, unit);
		}
		
		/// <summary>
		/// The LoadFont routines provide a safe way to load fonts.
		/// </summary>
		/// <param name="fontName">The name of the font to load.</param>
		/// <param name="size">The size of the font to load.</param>
		/// <param name="style">The <see cref="System.Drawing.FontStyle"/> of the font</param>
		/// <param name="unit">The <see cref="System.Drawing.GraphicsUnit"/> of the font</param>
		/// <returns>
		/// The font to load or the menu font, if the requested font couldn't be loaded.
		/// </returns>

		//FIXME: Convert to Pango.FontDescription
		public Font LoadFont(string fontName, int size, FontStyle style, GraphicsUnit unit)
		{
			//try {
				return new Font(fontName, size, style);
			//} catch (Exception) {
				//return new Gtk.Label ("-").Style.FontDescription;
			//}
		}
		
		/// <summary>
		/// Returns a icon from the resource database, it handles localization
		/// transparent for the user. In the resource database can be a bitmap
		/// instead of an icon in the dabase. It is converted automatically.
		/// </summary>
		/// <returns>
		/// The icon in the (localized) resource database.
		/// </returns>
		/// <param name="name">
		/// The name of the requested icon.
		/// </param>
		/// <exception cref="ResourceNotFoundException">
		/// Is thrown when the GlobalResource manager can't find a requested resource.
		/// </exception>

		public Gdk.Pixbuf GetIcon (string name)
		{
			return GetIcon (name, Gtk.IconSize.Button);
		}
		
		public Gdk.Pixbuf GetIcon (string name, Gtk.IconSize size)
		{
			//if an icon name begins with '#', we assume it's a hex colour
			if (name.Length > 0 && name[0] == '#')
				return GetColourBlock (name, size);
			
			string stockid = InternalGetStockId (name);
			if (stockid != null) {
				Gtk.IconSet iconset = Gtk.IconFactory.LookupDefault (stockid);
				if (iconset != null) {
					return iconset.RenderIcon (Gtk.Widget.DefaultStyle, Gtk.TextDirection.None, Gtk.StateType.Normal, size, null, null);
				}
			}
			try {
				int w, h;
				Gtk.Icon.SizeLookup (size, out w, out h);
				return Gtk.IconTheme.Default.LoadIcon (stockid, h, (Gtk.IconLookupFlags) 0);
			} catch { 
			}
			
			return null;
		}
		
		//ONLY handles hex colour names of the form "#RRGGBB"
		Gdk.Pixbuf GetColourBlock (string name, Gtk.IconSize size)
		{
			int w, h;
			if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out w, out h))
				w = h = 22;
			Gdk.Pixbuf p = new Gdk.Pixbuf (Gdk.Colorspace.Rgb, true, 8, w, h);
			uint colour;
			if (!TryParseColourFromHex (name, false, out colour))
				//if lookup fails, make it transparent
				colour = 0xFFFFFF00;
			p.Fill (colour);
			return p;
		}
		
		bool TryParseColourFromHex (string str, bool alpha, out uint val)
		{
			val = 0x00000000;
			if (str.Length != (alpha? 9 : 7))
				return false;
			
			for (int stringIndex = 1; stringIndex < str.Length; stringIndex++) {
				uint bits;
				switch (str[stringIndex]) {
				case '0': bits = 0; break;
				case '1': bits = 1; break;
				case '2': bits = 2; break;
				case '3': bits = 3; break;
				case '4': bits = 4; break;
				case '5': bits = 5; break;
				case '6': bits = 6; break;
				case '7': bits = 7; break;
				case '8': bits = 8; break;
				case '9': bits = 9; break;
				case 'A': case 'a': bits = 10; break;
				case 'B': case 'b': bits = 11; break;
				case 'C': case 'c': bits = 12; break;
				case 'D': case 'd': bits = 13; break;
				case 'E': case 'e': bits = 14; break;
				case 'F': case 'f': bits = 15; break;
				default: return false;
				}
				
				val = (val << 4) | bits;
			}
			if (!alpha)
				val = (val << 8) | 0xFF;
			return true;
		}
		
		/// <summary>
		/// Returns a bitmap from the resource database, it handles localization
		/// transparent for the user. 
		/// </summary>
		/// <returns>
		/// The bitmap in the (localized) resource database.
		/// </returns>
		/// <param name="name">
		/// The name of the requested bitmap.
		/// </param>
		/// <exception cref="ResourceNotFoundException">
		/// Is thrown when the GlobalResource manager can't find a requested resource.
		/// </exception>

		public Gdk.Pixbuf GetBitmap (string name)
		{
			return GetBitmap (name, Gtk.IconSize.Button);
		}

		public Gdk.Pixbuf GetBitmap(string name, Gtk.IconSize size)
		{
			return GetIcon (name, size);
		}

		public Gtk.Image GetImage (string name, Gtk.IconSize size)
		{
			string stock = GetStockId (name);
			if (stock != null)
				return new Gtk.Image (stock, size);
			return new Gtk.Image (GetBitmap (name));
		}
		
		public static string GetStockIdFromResource (RuntimeAddin addin, string id)
		{
			return Instance.InternalGetStockIdFromResource (addin, id);
		}

		public static string GetStockId (string filename)
		{
			return Instance.InternalGetStockId (filename);
		}

		public static string GetStockId (RuntimeAddin addin, string filename)
		{
			return Instance.InternalGetStockId (addin, filename);
		}

		internal void AddToIconFactory (string stockId, string filename, Gtk.IconSize iconSize)
		{
			try {
				Gtk.IconSet iconSet = iconFactory.Lookup (stockId);
				if (iconSet == null) {
					iconSet = new Gtk.IconSet ();
					iconFactory.Add (stockId, iconSet);
				}

				Gtk.IconSource source = new Gtk.IconSource ();
				source.Filename = Path.GetFullPath (Path.Combine (Path.Combine ( Path.Combine ( Path.Combine (
				                                    "..", "data"), "resources"), "icons"), filename));
				source.Size = iconSize;
				iconSet.AddSource (source);

				// FIXME: temporary hack to retrieve the correct icon
				// from the filename
				stockMappings.Add (filename, stockId);
			}
			catch (GLib.GException) {
				// just discard the exception, the icon simply can't be
				// loaded
				LoggingService.LogWarning (typeof(ResourceService).ToString() + " can't load " + filename + " icon file");
			}
		}
		
		internal void AddToIconFactory (string stockId, Gdk.Pixbuf pixbuf, Gtk.IconSize iconSize)
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
		
		string InternalGetStockIdFromResource (RuntimeAddin addin, string id)
		{
			if (!id.StartsWith ("res:"))
				return id;
			
			id = id.Substring (4);
			int aid = addins.IndexOf (addin);
			Hashtable hash;
			if (aid == -1) {
				aid = addins.Add (addin);
				hash = new Hashtable ();
				addinIcons.Add (hash);
			} else {
				hash = (Hashtable) addinIcons [aid];
			}
			string sid = "__asm" + aid + "__" + id;
			if (!hash.Contains (sid)) {
				
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
		
		string GetComposedIcon (string[] ids)
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
					Gdk.Pixbuf px = GetBitmap (ids[n], sz);
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
		
		Gdk.Pixbuf MergeIcons (Gdk.Pixbuf icon1, Gdk.Pixbuf icon2)
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

		internal void AddToIconFactory (string stockId, string filename)
		{
			AddToIconFactory (stockId, filename, Gtk.IconSize.Invalid);
		}
		
		internal void AddDefaultStockMapping (string stockFile, string nativeStock)
		{
			stockMappings.Add (stockFile, nativeStock);
		}

		internal string InternalGetStockId (string filename)
		{
			return InternalGetStockId (null, filename);
		}
		
		internal string InternalGetStockId (RuntimeAddin addin, string filename)
		{
			if (filename.IndexOf ('|') == -1)
				return PrivGetStockId (addin, filename);
			
			string[] parts = filename.Split ('|');
			for (int n=0; n<parts.Length; n++) {
				parts [n] = PrivGetStockId (addin, parts[n]);
			}
			return GetComposedIcon (parts);
		}
		
		string PrivGetStockId (RuntimeAddin addin, string filename)
		{
			if (addin != null && filename.StartsWith ("res:"))
				return InternalGetStockIdFromResource (addin, filename);
				
			string s = (string) stockMappings [filename];
			
			if (s != null)
				return s;
			
			return filename;
		}
	}
}
