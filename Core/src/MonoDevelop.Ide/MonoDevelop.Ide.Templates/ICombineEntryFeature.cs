
using System;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Ide.Templates
{
	public interface ICombineEntryFeature
	{
		bool SupportsCombineEntry (Combine parentCombine, CombineEntry entry);
		string Title { get; }
		Gtk.Widget CreateFeatureEditor (Combine parentCombine, CombineEntry entry);
		bool IsEnabled (Combine parentCombine, CombineEntry entry);
		string Validate (Combine parentCombine, CombineEntry entry, Gtk.Widget editor);
		void ApplyFeature (Combine parentCombine, CombineEntry entry, Gtk.Widget editor);
	}
	
	internal class CombineEntryFeatures
	{
		public static ICombineEntryFeature[] GetFeatures (Combine parentCombine, CombineEntry entry)
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
