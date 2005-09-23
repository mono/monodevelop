// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//   license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Threading;
using System.Resources;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using System.Runtime.InteropServices;

using MonoDevelop.Core.Properties;
using MonoDevelop.Services;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Core.Services
{
	/// <summary>
	/// This Class contains two ResourceManagers, which handle string and image resources
	/// for the application. It do handle localization strings on this level.
	/// </summary>
	public class ImageButton : Gtk.Button
	{
		public ImageButton (string stock, string label)
		{
			Gtk.HBox hbox1 = new Gtk.HBox(false,0);
			hbox1.PackStart(new Gtk.Image(stock, Gtk.IconSize.Button), false, true, 0);
			hbox1.PackStart(new Gtk.Label(label), true, true, 0);
			this.Add(hbox1);
		}
	}
	
	public class ResourceService : AbstractService
	{
		static Gtk.IconFactory iconFactory = null;
		static Hashtable stockMappings = null;
		
		static ArrayList addinIcons = new ArrayList ();
		static ArrayList addins = new ArrayList ();
		
		static ResourceService()
		{
			iconFactory = new Gtk.IconFactory ();

			// FIXME: remove this when all MonoDevelop is using Gtk+
			// stock icons
			stockMappings = new Hashtable ();
			iconFactory.AddDefault ();
		}
		
		public override void InitializeService ()
		{
			base.InitializeService();

			StockIconCodon[] icons = (StockIconCodon[])(AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/StockIcons").BuildChildItems(null)).ToArray(typeof(StockIconCodon));
			foreach (StockIconCodon icon in icons) {
				foreach (Assembly a in icon.AddIn.RuntimeLibraries.Values) {
					try {
						Gdk.Pixbuf px = new Gdk.Pixbuf (a, icon.Resource);
						AddToIconFactory (icon.StockId, px, icon.IconSize);
						break;
					} catch {}
				}
			}
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
			string stockid = GetStockId (name);
			if (stockid != null) {
				Gtk.IconSet iconset = Gtk.IconFactory.LookupDefault (stockid);
				if (iconset != null) {
					return iconset.RenderIcon (Gtk.Widget.DefaultStyle, Gtk.TextDirection.None, Gtk.StateType.Normal, size, null, null);
				}
			}
			
			return null;
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
			// Try stock icons first
			Gdk.Pixbuf pix = GetIcon (name, size);
			if (pix == null) {
				// Try loading directly from disk then
				pix = new Gdk.Pixbuf("../data/resources/icons/" + name);
			}
			return pix;
		}

		public Gtk.Image GetImage (string name, Gtk.IconSize size)
		{
			string stock = GetStockId (name);
			if (stock != null)
				return new Gtk.Image (stock, size);
			return new Gtk.Image (GetBitmap (name));
		}
		
		internal static void AddToIconFactory (string stockId,
		                                       string filename,
						       Gtk.IconSize iconSize)
		{
			try {
				Gtk.IconSet iconSet = iconFactory.Lookup (stockId);
				if (iconSet == null) {
					iconSet = new Gtk.IconSet ();
					iconFactory.Add (stockId, iconSet);
				}

				Gtk.IconSource source = new Gtk.IconSource ();
				source.Filename = Path.GetFullPath (Path.Combine ("../data/resources/icons", filename));
				source.Size = iconSize;
				iconSet.AddSource (source);

				// FIXME: temporary hack to retrieve the correct icon
				// from the filename
				stockMappings.Add (filename, stockId);
			}
			catch (GLib.GException ex) {
				// just discard the exception, the icon simply can't be
				// loaded
				Runtime.LoggingService.Info(typeof(ResourceService).ToString(), "Warning: can't load " + filename +
				                   " icon file");
			}
		}
		
		internal static void AddToIconFactory (string stockId,
		                                       Gdk.Pixbuf pixbuf,
						       Gtk.IconSize iconSize)
		{
			Gtk.IconSet iconSet = iconFactory.Lookup (stockId);
			if (iconSet == null) {
				iconSet = new Gtk.IconSet ();
				iconFactory.Add (stockId, iconSet);
			}

			Gtk.IconSource source = new Gtk.IconSource ();
			source.Pixbuf = pixbuf;
			source.Size = iconSize;
			iconSet.AddSource (source);
		}
		
		static public string GetStockIdFromResource (AddIn addin, string id)
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
				foreach (Assembly asm in addin.RuntimeLibraries.Values) {
					try {
						Gdk.Pixbuf pix = new Gdk.Pixbuf (asm, id);
						AddToIconFactory (sid, pix, Gtk.IconSize.Invalid);
						break;
					} catch {}
				}
				hash [sid] = sid;
			}
			return sid;
		}

		internal static void AddToIconFactory (string stockId, string filename)
		{
			AddToIconFactory (stockId, filename, Gtk.IconSize.Invalid);
		}
		
		internal static void AddDefaultStockMapping (string stockFile, string nativeStock)
		{
			stockMappings.Add (stockFile, nativeStock);
		}

		public static string GetStockId (string filename)
		{
			return GetStockId (null, filename);
		}
		
		public static string GetStockId (AddIn addin, string filename)
		{
			if (addin != null && filename.StartsWith ("res:"))
				return GetStockIdFromResource (addin, filename);
				
			string s = (string) stockMappings [filename];
			
			if (s != null)
				return s;
			
			return filename;
		}
	}
}
