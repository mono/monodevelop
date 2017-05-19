
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("GtkCore", 
	Namespace = "MonoDevelop",
	Version = MonoDevelop.BuildInfo.Version,
#if GNOME
	EnabledByDefault = false,
#endif
	Category = "IDE extensions")]

[assembly:AddinName ("GTK# Visual Designer")]
[assembly:AddinDescription ("Provides support for visual design of GTK# windows, dialogs and widgets")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("DesignerSupport", MonoDevelop.BuildInfo.Version)]
