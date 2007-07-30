
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Gettext
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Gettext.dll
REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Components.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Core.Gui.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Deployment.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Ide.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.dll \
	-r:$(top_builddir)/build/AddIns/MonoDevelop.Projects.Gui.dll \
	-r:$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	-r:$(top_srcdir)/contrib/Mono.Addins.dll \
	-r:Mono.Posix \
	-r:System \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	Commands.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Gettext.Editor.CatalogHeadersWidget.cs \
	gtk-gui/MonoDevelop.Gettext.POEditorWidget.cs \
	gtk-gui/MonoDevelop.Gettext.TranslationProjectOptionsDialog.cs \
	gtk-gui/MonoDevelop.Gettext.Translator.LanguageChooserDialog.cs \
	MonoDevelop.Gettext.Editor/CatalogEditor.cs \
	MonoDevelop.Gettext.Editor/CatalogHeadersWidget.cs \
	MonoDevelop.Gettext.Editor/GettextEditorDisplayBinding.cs \
	MonoDevelop.Gettext.Editor/GtkSpell.cs \
	MonoDevelop.Gettext.Editor/POEditorWidget.cs \
	MonoDevelop.Gettext.NodeBuilders/TranslationNodeBuilder.cs \
	MonoDevelop.Gettext.NodeBuilders/TranslationProjectNodeBuilder.cs \
	MonoDevelop.Gettext.Translator/LanguageChooserDialog.cs \
	MonoDevelop.Gettext/Catalog.cs \
	MonoDevelop.Gettext/CatalogDeletedEntry.cs \
	MonoDevelop.Gettext/CatalogEntry.cs \
	MonoDevelop.Gettext/CatalogHeaders.cs \
	MonoDevelop.Gettext/CatalogParser.cs \
	MonoDevelop.Gettext/IsoCodes.cs \
	MonoDevelop.Gettext/PluralForms.cs \
	MonoDevelop.Gettext/Translation.cs \
	MonoDevelop.Gettext/TranslationProject.cs \
	MonoDevelop.Gettext/TranslationProjectOptionsDialog.cs \
	MonoDevelop.Gettext/TranslationService.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/locale.svg \
	icons/locale_16x16.png \
	icons/locale_22x22.png \
	icons/locale_32x32.png \
	icons/md-gettext-project_16.png \
	icons/md-gettext-project_22.png \
	icons/md-gettext-project_32.png \
	templates/TranslationProject.xpt.xml


ADDIN = MonoDevelop.Gettext.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)/$(ADDIN)

$(ADDIN_BUILD)/$(ADDIN): $(srcdir)/$(ADDIN)
	mkdir -p $(ADDIN_BUILD)
	cp $(srcdir)/$(ADDIN) $(ADDIN_BUILD)/.

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Gettext
assembly_DATA = $(ASSEMBLY) $(ADDIN)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(ADDIN) $(RES)

include $(top_srcdir)/Makefile.include
