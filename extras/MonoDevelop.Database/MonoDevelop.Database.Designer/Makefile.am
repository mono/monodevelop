ADDIN_BUILD = $(top_builddir)/build
ASSEMBLY = $(ADDIN_BUILD)/MonoDevelop.Database.Designer.dll
REFS =  \
	$(GTK_SHARP_LIBS) \
	$(GTKSOURCEVIEW_SHARP_LIBS) \
	$(MONODEVELOP_LIBS) \
	-r:../build/MonoDevelop.Database.Components.dll \
	-r:../build/MonoDevelop.Database.Sql.dll \
	-r:Mono.Posix \
	-r:System

FILES =  \
	Dialogs/CreateDatabaseDialog.cs \
	Dialogs/PreviewDialog.cs \
	Dialogs/ProcedureEditorDialog.cs \
	Dialogs/TableEditorDialog.cs \
	Dialogs/UserEditorDialog.cs \
	Dialogs/ViewEditorDialog.cs \
	gtk-gui/generated.cs \
	gtk-gui/MonoDevelop.Database.Designer.CheckConstraintEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Designer.ColumnsEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Designer.CommentEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Designer.ConstraintsEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Designer.CreateDatabaseDialog.cs \
	gtk-gui/MonoDevelop.Database.Designer.ForeignKeyConstraintEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Designer.IndicesEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Designer.PreviewDialog.cs \
	gtk-gui/MonoDevelop.Database.Designer.PrimaryKeyConstraintEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Designer.ProcedureEditorDialog.cs \
	gtk-gui/MonoDevelop.Database.Designer.TableEditorDialog.cs \
	gtk-gui/MonoDevelop.Database.Designer.TriggersEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Designer.UniqueConstraintEditorWidget.cs \
	gtk-gui/MonoDevelop.Database.Designer.UserEditorDialog.cs \
	gtk-gui/MonoDevelop.Database.Designer.ViewEditorDialog.cs \
	Widgets/CheckConstraintEditorWidget.cs \
	Widgets/ColumnsEditorWidget.cs \
	Widgets/CommentEditorWidget.cs \
	Widgets/ConstraintsEditorWidget.cs \
	Widgets/ForeignKeyConstraintEditorWidget.cs \
	Widgets/IndicesEditorWidget.cs \
	Widgets/PrimaryKeyConstraintEditorWidget.cs \
	Widgets/TriggersEditorWidget.cs \
	Widgets/UniqueConstraintEditorWidget.cs

RES =  \
	gtk-gui/gui.stetic \
	gtk-gui/objects.xml \
	MonoDevelop.Database.Designer.addin.xml

all: $(ASSEMBLY) $(ADDIN_BUILD)

$(ASSEMBLY): $(FILES) $(build_resources)
	mkdir -p $(ADDIN_BUILD)
	$(CSC) $(CSC_FLAGS) $(REFS) $(build_resources:%=/resource:%) $(build_sources) -out:$@ -target:library


assemblydir = $(MD_ADDIN_DIR)/MonoDevelop.Database
assembly_DATA = $(ASSEMBLY) $(ASSEMBLY).mdb

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include
