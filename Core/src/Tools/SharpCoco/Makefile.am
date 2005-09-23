
ASSEMBLY = SharpCoco.exe
ATG = src/Coco.ATG

FILES = src/AssemblyInfo.cs \
src/Parser.cs \
src/Trace.cs \
src/Coco.cs \
src/Tab.cs \
src/ParserGen.cs \
src/Scanner.cs \
src/DFA.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(build_sources)
	$(CSC) $(CSC_FLAGS) $(build_sources) /out:$(ASSEMBLY)

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
EXTRA_DIST = $(FILES)
include $(top_srcdir)/Makefile.include

