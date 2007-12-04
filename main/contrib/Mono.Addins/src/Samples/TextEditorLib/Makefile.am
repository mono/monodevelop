ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ../bin/TextEditorLib.dll
COMPILE_TARGET = library
PROJECT_REFERENCES =  \
	../../bin/Mono.Addins.dll \
	../../bin/Mono.Addins.Gui.dll
BUILD_DIR = ../bin

	
if ENABLE_GUI
all: $(ASSEMBLY)
endif

FILES =  \
	AssemblyInfo.cs \
	Commands/CopyCommand.cs \
	Commands/CutCommand.cs \
	Commands/ExitCommand.cs \
	Commands/NewCommand.cs \
	Commands/OpenCommand.cs \
	Commands/PasteCommand.cs \
	Commands/SaveCommand.cs \
	Commands/SetupCommand.cs \
	ExtensionNodes/FileTemplateNode.cs \
	ExtensionNodes/MenuItemNode.cs \
	ExtensionNodes/MenuNode.cs \
	ExtensionNodes/MenuSeparatorNode.cs \
	ExtensionNodes/SubmenuNode.cs \
	ExtensionNodes/TemplateCategoryNode.cs \
	ExtensionNodes/ToolbarNode.cs \
	ExtensionNodes/ToolButtonNode.cs \
	ExtensionNodes/ToolSeparatorNode.cs \
	gtk-gui/generated.cs \
	gtk-gui/MainWindow.cs \
	ICommand.cs \
	MainWindow.cs \
	OpenFileCondition.cs \
	TextEditorApp.cs 

DATA_FILES = 

RESOURCES =  \
	gtk-gui/gui.stetic \
	Templates/ChangeLogTemplate.txt \
	Templates/DotConfigTemplate.txt \
	Templates/TextEditor.addin.xml \
	Templates/WorkReport.txt 

EXTRAS = 

REFERENCES =  \
	$(GTK_SHARP_20_LIBS) \
	-pkg:gtk-sharp-2.0 \
	-r:Mono.Posix \
	-r:System

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

if ENABLE_GUI
include $(top_srcdir)/makefile-sample.include
endif
