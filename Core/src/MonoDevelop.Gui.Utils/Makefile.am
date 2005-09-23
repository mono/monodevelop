DLLNAME = MonoDevelop.Gui.Utils.dll
DLL = $(top_builddir)/build/bin/$(DLLNAME)

REFERENCES = /r:System.Drawing \
	/r:ICSharpCode.SharpZipLib \
	$(GTK_SHARP_LIBS) \
	$(GNOME_SHARP_LIBS)

FILES = \
FileIcons/FileIconLoader.cs \
ReportingStream/ReportingStream.cs \
DirectoryArchive/Decompressor.cs \
DirectoryArchive/TarDecompressor.cs \
DirectoryArchive/ZipDecompressor.cs \
DirectoryArchive/Bz2Support.cs \
DirectoryArchive/GzSupport.cs

RES = \
icons/gnome-fs-regular.png

GENERATED_FILES = \
AssemblyInfo.cs

all: $(DLL)

$(DLL): $(build_sources) $(RES)
	$(CSC) $(CSC_FLAGS) -out:$@ /target:library $(REFERENCES) $(RES:%=/resource:%) $(build_sources)

assemblydir = $(MD_ASSEMBLY_DIR)
assembly_DATA = $(DLL)

CLEANFILES = $(DLL) $(DLL).mdb

EXTRA_DIST = $(FILES) $(RES)

include $(top_srcdir)/Makefile.include

