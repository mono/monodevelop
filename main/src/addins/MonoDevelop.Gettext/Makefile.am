
ADDIN_BUILD = $(top_builddir)/build/AddIns/MonoDevelop.Gettext
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Gettext.dll

DEPS =  \
	$(top_builddir)/build/AddIns/MonoDevelop.Autotools/MonoDevelop.Autotools.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.Deployment/MonoDevelop.Deployment.dll \
	$(top_builddir)/build/AddIns/MonoDevelop.DesignerSupport/MonoDevelop.DesignerSupport.dll \
	$(top_builddir)/build/bin/Mono.TextEditor.dll \
	$(top_builddir)/build/bin/MonoDevelop.Components.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.dll \
	$(top_builddir)/build/bin/MonoDevelop.Core.Gui.dll \
	$(top_builddir)/build/bin/MonoDevelop.Ide.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.dll \
	$(top_builddir)/build/bin/MonoDevelop.Projects.Gui.dll

REFS =  \
	$(GLIB_SHARP_LIBS) \
	$(GTK_SHARP_LIBS) \
	$(MONO_ADDINS_LIBS) \
	-r:Mono.Posix \
	-r:System \
	-r:System.Core \
	-r:System.Xml

FILES =  \
	AssemblyInfo.cs \
	Commands.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Gettext.Editor.CatalogHeadersWidget.cs \
	gtk-gui/MonoDevelop.Gettext.GettextFeatureWidget.cs \
	gtk-gui/MonoDevelop.Gettext.POEditorWidget.cs \
	gtk-gui/MonoDevelop.Gettext.TranslationProjectOptionsDialog.cs \
	gtk-gui/MonoDevelop.Gettext.Translator.LanguageChooserDialog.cs \
	MonoDevelop.Gettext.Editor/CatalogEditorView.cs \
	MonoDevelop.Gettext.Editor/CatalogHeadersWidget.cs \
	MonoDevelop.Gettext.Editor/GettextEditorDisplayBinding.cs \
	MonoDevelop.Gettext.Editor/GtkSpell.cs \
	MonoDevelop.Gettext.Editor/POEditorWidget.cs \
	MonoDevelop.Gettext.ExtensionNodes/ExcludeRegex.cs \
	MonoDevelop.Gettext.ExtensionNodes/IncludeExtensionNode.cs \
	MonoDevelop.Gettext.ExtensionNodes/RegexScannerExtensionNode.cs \
	MonoDevelop.Gettext.ExtensionNodes/TransformExtensionNode.cs \
	MonoDevelop.Gettext.ExtensionNodes/XmlRegexScannerExtensionNode.cs \
	MonoDevelop.Gettext.NodeBuilders/ProjectFileNodeBuilderExtension.cs \
	MonoDevelop.Gettext.NodeBuilders/TranslationNodeBuilder.cs \
	MonoDevelop.Gettext.NodeBuilders/TranslationProjectNodeBuilder.cs \
	MonoDevelop.Gettext.Translator/LanguageChooserDialog.cs \
	MonoDevelop.Gettext/Catalog.cs \
	MonoDevelop.Gettext/CatalogDeletedEntry.cs \
	MonoDevelop.Gettext/CatalogEntry.cs \
	MonoDevelop.Gettext/CatalogParser.cs \
	MonoDevelop.Gettext/GettextFeature.cs \
	MonoDevelop.Gettext/GettextFeatureWidget.cs \
	MonoDevelop.Gettext/GettextTool.cs \
	MonoDevelop.Gettext/IFileScanner.cs \
	MonoDevelop.Gettext/IsoCodes.cs \
	MonoDevelop.Gettext/MakefileHandler.cs \
	MonoDevelop.Gettext/PluralForms.cs \
	MonoDevelop.Gettext/PropertyProvider.cs \
	MonoDevelop.Gettext/RegexFileScanner.cs \
	MonoDevelop.Gettext/StringEscaping.cs \
	MonoDevelop.Gettext/Translation.cs \
	MonoDevelop.Gettext/TranslationCollection.cs \
	MonoDevelop.Gettext/TranslationProject.cs \
	MonoDevelop.Gettext/TranslationProjectOptionsDialog.cs \
	MonoDevelop.Gettext/TranslationService.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	icons/gettext-overlay-16.png \
	icons/gettext-overlay-32.png \
	icons/locale_16x16.png \
	icons/locale_22x22.png \
	icons/locale_32x32.png \
	icons/translation-fuzzy-16.png \
	icons/translation-missing-16.png \
	icons/translation-valid-16.png \
	MonoDevelop.Gettext.addin.xml \
	templates/Makefile.am.template \
	templates/Makefile.template \
	templates/TranslationProject.xpt.xml

all: $(ASSEMBLY) $(ASSEMBLY).mdb

$(ADDIN_BUILD)/%: $(srcdir)/lib/%
	mkdir -p $(ADDIN_BUILD)
	cp $< $@

$(ASSEMBLY): $(build_sources) $(build_resources) $(DEPS)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_deps) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library

$(ASSEMBLY).mdb: $(ASSEMBLY)

assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Gettext
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
