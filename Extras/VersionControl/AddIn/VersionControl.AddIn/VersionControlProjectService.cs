using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Components.Commands;

using VersionControl.Service;

namespace VersionControl.AddIn
{
	public class VersionControlProjectService
	{
		static Gdk.Pixbuf overlay_normal;
		static Gdk.Pixbuf overlay_modified;
		static Gdk.Pixbuf overlay_removed;
		static Gdk.Pixbuf overlay_conflicted;
		static Gdk.Pixbuf overlay_added;
		internal static Gdk.Pixbuf overlay_controled;
		static Gdk.Pixbuf overlay_unversioned;

		static Gdk.Pixbuf icon_normal;
		static Gdk.Pixbuf icon_modified;
		static Gdk.Pixbuf icon_removed;
		static Gdk.Pixbuf icon_conflicted;
		static Gdk.Pixbuf icon_added;
		internal static Gdk.Pixbuf icon_controled;
		
		static VersionControlProjectService ()
		{
			overlay_normal = Gdk.Pixbuf.LoadFromResource("overlay_normal.png");
			overlay_modified = Gdk.Pixbuf.LoadFromResource("overlay_modified.png");
			overlay_removed = Gdk.Pixbuf.LoadFromResource("overlay_removed.png");
			overlay_conflicted = Gdk.Pixbuf.LoadFromResource("overlay_conflicted.png");
			overlay_added = Gdk.Pixbuf.LoadFromResource("overlay_added.png");
			overlay_controled = Gdk.Pixbuf.LoadFromResource("overlay_controled.png");
			overlay_unversioned = Gdk.Pixbuf.LoadFromResource("overlay_unversioned.png");
			
			icon_normal = Gdk.Pixbuf.LoadFromResource("overlay_normal.png");
			icon_modified = MonoDevelop.Core.Gui.Services.Resources.GetIcon ("gtk-edit", Gtk.IconSize.Menu);
			icon_removed = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.Remove, Gtk.IconSize.Menu);
			icon_conflicted = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.DialogWarning, Gtk.IconSize.Menu);
			icon_added = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Gtk.Stock.Add, Gtk.IconSize.Menu);
			icon_controled = Gdk.Pixbuf.LoadFromResource("overlay_controled.png");
		}
		
		public static Gdk.Pixbuf LoadOverlayIconForStatus(VersionStatus status)
		{
			switch (status) {
				case VersionStatus.Unchanged:
					return null;
				case VersionStatus.Modified:
					return overlay_modified;
				case VersionStatus.Conflicted:
					return overlay_conflicted;
				case VersionStatus.ScheduledAdd:
					return overlay_added;
				case VersionStatus.ScheduledDelete:
					return overlay_removed;
				case VersionStatus.UnversionedIgnored:
				case VersionStatus.Unknown:
				case VersionStatus.Unversioned:
					return overlay_unversioned;
			}
			return null;
		}
		
		public static Gdk.Pixbuf LoadIconForStatus(VersionStatus status)
		{
			switch (status) {
				case VersionStatus.Unchanged:
					return icon_normal;
				case VersionStatus.Modified:
					return icon_modified;
				case VersionStatus.Conflicted:
					return icon_conflicted;
				case VersionStatus.ScheduledAdd:
					return icon_added;
				case VersionStatus.ScheduledDelete:
					return icon_removed;
				default:
					if (status != VersionStatus.Unknown && status != VersionStatus.Unversioned && status != VersionStatus.UnversionedIgnored)
						return icon_controled;
					break;
			}
			return null;
		}

		public static string GetStatusLabel (VersionStatus status)
		{
			switch (status) {
				case VersionStatus.Unchanged:
					return "";
				case VersionStatus.Modified:
					return GettextCatalog.GetString ("Modified");
				case VersionStatus.Conflicted:
					return GettextCatalog.GetString ("Conflict");
				case VersionStatus.ScheduledAdd:
					return GettextCatalog.GetString ("Add");
				case VersionStatus.ScheduledDelete:
					return GettextCatalog.GetString ("Delete");
				default:
					if (status != VersionStatus.Unknown && status != VersionStatus.Unversioned && status != VersionStatus.UnversionedIgnored)
						return "";
					break;
			}
			return GettextCatalog.GetString ("Unversioned");
		}
		
		public static Repository GetRepository (CombineEntry entry, string path)
		{
			Repository repo = (Repository) entry.ExtendedProperties [typeof(Repository)];
			if (repo != null)
				return repo;
			
			repo = VersionControlService.GetRepositoryReference (entry.BaseDirectory, entry.Name);
			entry.ExtendedProperties [typeof(Repository)] = repo;
			
			return repo;
		}
	}
}
