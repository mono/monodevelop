BINDIR=$(top_builddir)/build/bin

if ENABLE_GIT
all: $(GITSHARP_CORE) $(GITSHARP)
else
all:
endif

GITSHARP_CORE_DLL=GitSharp.Core.dll
GITSHARP_CORE=$(BINDIR)/$(GITSHARP_CORE_DLL)
GITSHARP_CORE_DEBUG=$(BINDIR)/$(GITSHARP_CORE_DLL).mdb
GITSHARP_CORE_DIR=$(srcdir)/GitSharp.Core

$(GITSHARP_CORE): $(GITSHARP_CORE_CSFILES)
	mkdir -p $(BINDIR)
	xbuild $(GITSHARP_CORE_DIR)/GitSharp.Core.csproj

GITSHARP_DLL=GitSharp.dll
GITSHARP=$(BINDIR)/$(GITSHARP_DLL)
GITSHARP_DEBUG=$(BINDIR)/$(GITSHARP_DLL).mdb
GITSHARP_DIR=$(srcdir)/GitSharp

$(GITSHARP): $(GITSHARP_CSFILES) $(GITSHARP_CORE)
	mkdir -p $(BINDIR)
	xbuild $(GITSHARP_DIR)/GitSharp.csproj

CLEANFILES = $(GITSHARP_CORE) $(GITSHARP_CORE_MDB) $(GITSHARP) $(GITSHARP_MDB)

## Helpers for re-importing the latest source
## Clone git://github.com/sinesignal/GitSharp.git to the
## same root directory MonoDevelop is located in.  For 
## example, if you cloned MonoDevelop to /home/user/sourcecode
## you will want to clone GitSharp to the same directory.
## git clone git://github.com/sinesignal/GitSharp.git
## Then pull the development branch and check it out.
## git pull origin development
## git checkout development
## I'm sure this can be done better, but I'm not quite sure how
## After running make update-source, you will need to manually edit the copied 
## csproj files before compiling and checking in
## Change the output path and XML comments path
## Remove the hintpath for the SharpZipLib reference
## Remove the SharedAssemblyInfo.cs file also

GITSHARP_ROOT_SRC_DIR=../../../../GitSharp

update-source-GitSharp.Core:
	rm -rf ./GitSharp.Core
	cp -r $(GITSHARP_ROOT_SRC_DIR)/GitSharp.Core .

update-source-GitSharp:
	rm -rf ./GitSharp
	cp -r $(GITSHARP_ROOT_SRC_DIR)/GitSharp .

update-source: update-source-GitSharp.Core update-source-GitSharp

