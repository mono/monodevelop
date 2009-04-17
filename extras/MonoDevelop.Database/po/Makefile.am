
PACKAGE = monodevelop-database
MDTOOL = $(LOCAL_PKGCONFIG) mono `$(LOCAL_PKGCONFIG) pkg-config --variable=libdir monodevelop`/bin/mdrun.exe
BUILD_DIR = $(top_builddir)/build/locale
INSTALL_DIR = $(DESTDIR)$(MD_ADDIN_DIR)/MonoDevelop.Database/locale

FILES = \
	ca.po\
	cs.po\
	da.po\
	de.po\
	es.po\
	fr.po\
	gl.po\
	hu.po\
	id.po\
	it.po\
	ja.po\
	nl.po\
	pl.po\
	pt.po\
	pt_BR.po\
	ru.po\
	sl_SI.po\
	sv.po\
	tr.po\
	zh_CN.po\
	zh_TW.po

GMO_FILES = $(patsubst %.po,$(BUILD_DIR)/%/LC_MESSAGES/$(PACKAGE).mo,$(FILES))
MO_FILES = $(foreach po,$(FILES), $(INSTALL_DIR)/$(basename $(po))/LC_MESSAGES/$(PACKAGE).mo)

all: $(GMO_FILES)

update-po:
	cd $(top_srcdir) && $(MDTOOL) gettext-update

statistics:
	@for LANGFILE in $(FILES); do \
		echo "$$LANGFILE:"; \
		msgfmt -o /dev/null --verbose "$$LANGFILE"; \
	done

$(GMO_FILES): $(BUILD_DIR)/%/LC_MESSAGES/$(PACKAGE).mo: %.po
	mkdir -p '$(dir $@)'
	msgfmt '$<' -o '$@'
	
$(MO_FILES): $(INSTALL_DIR)/%/LC_MESSAGES/$(PACKAGE).mo : $(BUILD_DIR)/%/LC_MESSAGES/$(PACKAGE).mo
	mkdir -p '$(dir $@)'
	$(INSTALL) '$<' '$@'

install-data-local: $(MO_FILES)

uninstall-local:
	rm -f $(MO_FILES)

CLEANFILES = $(GMO_FILES)
EXTRA_DIST = $(FILES)

include $(top_srcdir)/Makefile.include
