ADDIN_VERSION=2.10.3.23
CORE_ADDIN_VERSION=1.9.1
TARGET_VERSION=2.10
GTK_POLICY_VERSION=2.4 2.6 2.8 2.10 2.12 2.14 2.16 2.18
GNOME_POLICY_VERSION=2.4 2.6 2.8 2.16 2.18 2.20 2.22 2.24 2.26 2.28 2.30
GTK_VERSION=2.10.3
GNOME_VERSION=2.16.1

GTK_TARBALL=gtk-sharp-$(GTK_VERSION).tar.gz
GNOME_TARBALL=gnome-sharp-$(GNOME_VERSION).tar.gz
GTK_DIR=gtk-sharp-$(GTK_VERSION)
GNOME_DIR=gnome-sharp-$(GNOME_VERSION)
ADDIN_FILE=md-gtk-sharp.addin.xml
ADDIN_MPACK=MonoDevelop.GtkCore.GtkSharp.$(TARGET_VERSION)_$(GTK_VERSION).mpack
GTK_LIB=tmpinstall/lib/mono/gac/gtk-sharp2/$(GTK_VERSION)/gtk-sharp2.dll
GNOME_LIB=tmpinstall/lib/mono/gac/gnome-sharp2/$(GTK_VERSION)/gnome-sharp2.dll

GTK_LIBS = \
	atk/atk-sharp \
	gconf/GConf/gconf-sharp \
	gdk/gdk-sharp \
	glade/glade-sharp \
	glib/glib-sharp \
	gnome/gnome-sharp \
	gtkdotnet/gtk-dotnet \
	gtk/gtk-sharp \
	pango/pango-sharp

GNOME_LIBS = \
	art/art-sharp \
	gconf/GConf/gconf-sharp \
	gnome/gnome-sharp \
	gnomevfs/gnome-vfs-sharp \
	gtkhtml/gtkhtml-sharp \
	rsvg/rsvg-sharp \
	vte/vte-sharp

LIBS = $(foreach lib, $(GTK_LIBS), $(GTK_DIR)/$(lib)) $(foreach lib, $(GNOME_LIBS), $(GNOME_DIR)/$(lib))
ASSEMBLIES = $(foreach lib, $(LIBS), $(lib).dll)
GTK_POLICY_FILES = $(foreach pv, $(GTK_POLICY_VERSION), $(foreach lib, $(LIBS), $(dir $(lib))policy.$(pv).$(notdir $(lib)).dll))
GTK_POLICY_CONFIGS = $(foreach pv, $(GTK_POLICY_VERSION), $(foreach lib, $(LIBS), $(dir $(lib))policy.$(pv).config))
GNOME_POLICY_FILES = $(foreach pv, $(GNOME_POLICY_VERSION), $(foreach lib, $(LIBS), $(dir $(lib))policy.$(pv).$(notdir $(lib)).dll))
GNOME_POLICY_CONFIGS = $(foreach pv, $(GNOME_POLICY_VERSION), $(foreach lib, $(LIBS), $(dir $(lib))policy.$(pv).config))
LOCAL_CONFIG=PKG_CONFIG_PATH=`pwd`/../tmpinstall/lib/pkgconfig:$$PKG_CONFIG_PATH PATH=`pwd`/../tmpinstall/bin:$$PATH

FILES = $(ASSEMBLIES) $(GTK_POLICY_FILES) $(GNOME_POLICY_FILES) $(GTK_POLICY_CONFIGS) $(GNOME_POLICY_CONFIGS)
GAC_FILES = $(ASSEMBLIES) $(POLICY_FILES) $(GNOME_POLICY_FILES)

all: build_gtk build_gnome $(ADDIN_FILE)

kk:
	echo $@

clean:
	rm -rf $(FILES) $(dir $(LIBS)) $(ADDIN_FILE) $(GTK_TARBALL) $(GNOME_TARBALL) $(GTK_DIR) $(GNOME_DIR) tmpinstall gconf files lib

$(GTK_TARBALL):
	wget http://go-mono.com/sources/gtk-sharp/$(GTK_TARBALL)

$(GTK_DIR)/Makefile: $(GTK_TARBALL)
	rm -rf $(GTK_DIR) && \
	tar xvfz $(GTK_TARBALL) && \
	cd $(GTK_DIR) && \
	sed s/POLICY_VERSIONS="\(.*\)"/\POLICY_VERSIONS=\""$(GTK_POLICY_VERSION)"\"/ configure > configure.mod && \
	chmod +x configure.mod && \
	$(LOCAL_CONFIG) ./configure.mod --prefix=`pwd`/../tmpinstall

$(GNOME_TARBALL):
	wget http://go-mono.com/sources/gnome-sharp2/$(GNOME_TARBALL)

$(GNOME_DIR)/Makefile: $(GNOME_TARBALL)
	rm -rf $(GNOME_DIR) && \
	tar xvfz $(GNOME_TARBALL) && \
	cd $(GNOME_DIR) && \
	sed s/POLICY_VERSIONS="\(.*\)"/\POLICY_VERSIONS=\""$(GNOME_POLICY_VERSION)"\"/ configure > configure.mod && \
	chmod +x configure.mod && \
	$(LOCAL_CONFIG) ./configure.mod --prefix=`pwd`/../tmpinstall \

build_gtk: $(GTK_DIR)/Makefile
	cd $(GTK_DIR) && \
	$(LOCAL_CONFIG) make && \
	$(LOCAL_CONFIG) make install

build_gnome: $(GNOME_DIR)/Makefile
	cd $(GNOME_DIR) && \
	$(LOCAL_CONFIG) make && \
	$(LOCAL_CONFIG) make install

$(ADDIN_FILE): $(ADDIN_FILE).xsl
	echo "<files>"  > files
	echo "<targetversion>"$(TARGET_VERSION)"</targetversion>"  >> files
	echo "<gtkversion>"$(GTK_VERSION)"</gtkversion>"  >> files
	echo "<addinversion>"$(ADDIN_VERSION)"</addinversion>"  >> files
	echo "<coreaddinversion>"$(CORE_ADDIN_VERSION)"</coreaddinversion>"  >> files
	rm -rf lib
	cp -r tmpinstall/lib lib
	find lib -name *.dll -type f -printf "<dll>%p</dll>\n" >> files
	find lib -name *.config -type f -printf "<config>%p</config>\n" >> files
	echo "</files>" >> files
	mono-xmltool --transform $(ADDIN_FILE).xsl files $(ADDIN_FILE)

$(ADDIN_MPACK): $(ADDIN_FILE)
	mdtool setup p $(ADDIN_FILE)

pack: $(ADDIN_MPACK)

	
