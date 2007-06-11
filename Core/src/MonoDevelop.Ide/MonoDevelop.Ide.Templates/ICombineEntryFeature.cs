
using System;
using System.Collections.Generic;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Ide.Templates
{
	public interface ICombineEntryFeature
	{
		bool SupportsCombineEntry (Solution parentCombine, IProject entry);
		string Title { get; }
		Gtk.Widget CreateFeatureEditor (Solution parentCombine, IProject entry);
		bool IsEnabled (Solution parentCombine, IProject entry);
		string Validate (Solution parentCombine, IProject entry, Gtk.Widget editor);
		void ApplyFeature (Solution parentCombine, IProject entry, Gtk.Widget editor);
	}
	
	internal class CombineEntryFeatures
	{
		public static ICombineEntryFeature[] GetFeatures (Solution parentCombine, IProject entry)
		{
			List<ICombineEntryFeature> list = new List<ICombineEntryFeature> ();
			foreach (ICombineEntryFeature e in AddinManager.GetExtensionObjects ("/MonoDevelop/Workbench/ProjectFeatures", typeof(ICombineEntryFeature), true)) {
				if (e.SupportsCombineEntry (parentCombine, entry))
					list.Add (e);
			}
			return list.ToArray ();
		}
	}
}
