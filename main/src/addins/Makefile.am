
DEFAULTSUBDIRS = MacPlatform GnomePlatform MonoDevelop.DesignerSupport Deployment MonoDevelop.Autotools MonoDevelop.Gettext CSharpBinding ILAsmBinding NUnit MonoDeveloperExtensions VersionControl VBNetBinding prj2make-sharp-lib WelcomePage ChangeLogAddIn MonoDevelop.GtkCore AspNetAddIn MonoDevelop.WebReferences MonoDevelop.RegexToolkit CBinding MonoDevelop.AssemblyBrowser Mono.Texteditor MonoDevelop.Moonlight

if ENABLE_GTKSOURCEVIEW
SUBDIRS = $(DEFAULTSUBDIRS) MonoDevelop.SourceEditor 
else
if ENABLE_GTKSOURCEVIEW2
SUBDIRS = $(DEFAULTSUBDIRS) SourceEditor2
else
SUBDIRS = $(DEFAULTSUBDIRS) MonoDevelop.SourceEditor2
endif
endif

