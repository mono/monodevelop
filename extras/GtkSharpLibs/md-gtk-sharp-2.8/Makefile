ADDIN_VERSION=2.8.3.23
CORE_ADDIN_VERSION=1.9.1
TARGET_VERSION=2.8
POLICY_VERSION=2.4 2.6 2.8 2.10 2.12 2.14 2.16 2.18
GTK_VERSION=2.8.5

GTK_TARBALL=gtk-sharp-$(GTK_VERSION).tar.gz
GTK_DIR=gtk-sharp-$(GTK_VERSION)
ADDIN_FILE=md-gtk-sharp.addin.xml

LIBS = \
	art/art-sharp \
	atk/atk-sharp \
	gconf/GConf/gconf-sharp \
	gdk/gdk-sharp \
	glade/glade-sharp \
	glib/glib-sharp \
	gnome/gnome-sharp \
	gnomevfs/gnome-vfs-sharp \
	gtkdotnet/gtk-dotnet \
	gtkhtml/gtkhtml-sharp \
	gtk/gtk-sharp \
	pango/pango-sharp \
	rsvg/rsvg-sharp \
	vte/vte-sharp

ASSEMBLIES = $(foreach lib, $(LIBS), $(lib).dll)
POLICY_FILES = $(foreach pv, $(POLICY_VERSION), $(foreach lib, $(LIBS), $(dir $(lib))policy.$(pv).$(notdir $(lib)).dll))
POLICY_CONFIGS = $(foreach pv, $(POLICY_VERSION), $(foreach lib, $(LIBS), $(dir $(lib))policy.$(pv).config))

FILES = $(ASSEMBLIES) $(POLICY_FILES) $(POLICY_CONFIGS)
GAC_FILES = $(ASSEMBLIES) $(POLICY_FILES)

all: $(GAC_FILES) $(ADDIN_FILE)

clean:
	rm -rf $(FILES) $(dir $(LIBS)) $(ADDIN_FILE) $(GTK_TARBALL) $(GTK_DIR) gconf files lib

$(GTK_TARBALL):
	wget http://go-mono.com/sources/gtk-sharp-2.8/$(GTK_TARBALL)
	rm -rf $(GTK_DIR)
	tar xvfz $(GTK_TARBALL)

$(GTK_DIR)/Makefile: $(GTK_TARBALL)
	cd gtk-sharp-$(GTK_VERSION) && \
	sed s/POLICY_VERSIONS="\(.*\)"/\POLICY_VERSIONS=\""$(POLICY_VERSION)"\"/ configure > configure.mod && \
	chmod +x configure.mod && \
	./configure.mod

build: $(GTK_DIR)/Makefile
	cd gtk-sharp-$(GTK_VERSION) && \
	make

$(GAC_FILES): build
	CURD=`pwd` && \
	cd $(dir $(GTK_DIR)/$@) && \
	gacutil -i $(notdir $@) -root $$CURD/lib
	
$(ADDIN_FILE): $(ADDIN_FILE).xsl
	echo "<files>"  > files
	echo "<targetversion>"$(TARGET_VERSION)"</targetversion>"  >> files
	echo "<gtkversion>"$(GTK_VERSION)"</gtkversion>"  >> files
	echo "<addinversion>"$(ADDIN_VERSION)"</addinversion>"  >> files
	echo "<coreaddinversion>"$(CORE_ADDIN_VERSION)"</coreaddinversion>"  >> files
	find lib -name *.dll -type f -printf "<dll>%p</dll>\n" >> files
	find lib -name *.config -type f -printf "<config>%p</config>\n" >> files
	echo "</files>" >> files
	mono-xmltool --transform $(ADDIN_FILE).xsl files $(ADDIN_FILE)

pack:
	mdtool setup p $(ADDIN_FILE)
