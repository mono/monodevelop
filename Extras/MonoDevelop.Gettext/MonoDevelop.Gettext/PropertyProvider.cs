

using System;
using System.ComponentModel;

using MonoDevelop.Projects;
using MonoDevelop.DesignerSupport;

namespace MonoDevelop.Gettext
{
	public class PropertyProvider : IPropertyProvider
	{
		public bool SupportsObject (object o)
		{
			return o is ProjectFile;
		}

		public object CreateProvider (object o)
		{
			return new ProjectFileWrapper ((ProjectFile)o);
		}
		
		public class ProjectFileWrapper: CustomDescriptor
		{
			ProjectFile file;
			
			public ProjectFileWrapper (ProjectFile file)
			{
				this.file = file;
			}
			
			const string scanForTranslationsProperty = "Gettext.ScanForTranslations";
			[Category ("Gettext translation")]
			[DisplayName ("Scan for translations")]
			[Description ("Include this file in the translation scan.")]
			public bool ScanForTranslations {
				get {
					object result = file.ExtendedProperties [scanForTranslationsProperty];
					return result == null ? true : (bool)result;
				}
				set {
					if (value) {
						file.ExtendedProperties.Remove (scanForTranslationsProperty);
					} else {
						file.ExtendedProperties [scanForTranslationsProperty] = value;
					}
				}
			}
		}
		
	}
}
