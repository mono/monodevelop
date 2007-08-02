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
		
		public ResourceService ()
		{
			iconFactory = new Gtk.IconFactory ();

			// FIXME: remove this when all MonoDevelop is using Gtk+
			// stock icons
			stockMappings = new Hashtable ();
			iconFactory.AddDefault ();

			AddinManager.AddExtensionNodeHandler ("/SharpDevelop/Workbench/StockIcons", OnExtensionChange);
		}
		
		void OnExtensionChange (object sender, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				StockIconCodon icon = (StockIconCodon) args.ExtensionNode;
				System.IO.Stream s = icon.Addin.GetResource (icon.Resource);
				if (s != null) {
					using (s) {
						Gdk.Pixbuf px = new Gdk.Pixbuf (s);
						AddToIconFactory (icon.StockId, px, icon.IconSize);
					}
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
				source.Filename = Path.GetFullPath (Path.Combine ("../data/resources/icons", filename));
				source.Size = iconSize;
				iconSet.AddSource (source);

				// FIXME: temporary hack to retrieve the correct icon
				// from the filename
				stockMappings.Add (filename, stockId);
			}
			catch (GLib.GException) {
				// just discard the exception, the icon simply can't be
				// loaded
				Runtime.LoggingService.Info(typeof(ResourceService).ToString(), "Warning: can't load " + filename +
				                   " icon file");
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
			if (addin != null && filename.StartsWith ("res:"))
				return InternalGetStockIdFromResource (addin, filename);
				
			string s = (string) stockMappings [filename];
			
			if (s != null)
				return s;
			
			return filename;
		}
	}
}
