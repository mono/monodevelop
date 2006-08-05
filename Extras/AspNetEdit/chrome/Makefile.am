packagename = aspdesigner
extension_guid="{d7d824a2-030d-11db-9804-b622a1ef5492}"
chromedir = $(EXTENSION_DIR)
chrome_install_file = $(MOZILLA_HOME)/chrome/installed-chrome.txt
chrome_install_text =	"content,install,url,jar:resource:/chrome/aspdesigner.jar!/content/aspdesigner/"		\
			"locale,install,url,jar:resource:/chrome/aspdesigner.jar!/locale/en-US/aspdesigner/"

jar_files =	\
		content/aspdesigner/JSCall.js	\
		content/aspdesigner/contents.rdf \
		content/aspdesigner/aspdesigner.xul \
		content/aspdesigner/editorContent.css \
		content/aspdesigner/editor.js \
		content/aspdesigner/clipboard.js \
		content/aspdesigner/constants.js \
		content/aspdesigner/xpcom.js \
		locale/en-US/aspdesigner/contents.rdf

xpi_files = \
		chrome/$(packagename).jar \
		chrome.manifest \
		install.rdf

all: $(packagename).xpi

$(packagename).xpi: $(xpi_files)
	$(ZIP) -q9 $@ $^

content/aspdesigner/JSCall.js: $(JSCALL_SCRIPT)
	cp $^ $@

chrome/$(packagename).jar: $(jar_files)
	-@mkdir -p chrome
	$(ZIP) -q9 $@ $^

install-data-hook: install-files install-text

uninstall-hook: uninstall-files uninstall-text

install-files:
	mkdir -p $(chromedir); \
	cp chrome/$(packagename).jar $(chromedir)
	
#	for file in $(xpi_files); do \
#		cp $$file $(chromedir)/$$file; \
#	done;

uninstall-files:
	rm -rf $(chromedir)/$(packagename).jar

install-text:
	if test -f $(chrome_install_file); then \
		cp $(chrome_install_file) /tmp/chrome.old; \
		sudo bash -c "sed -e'/aspdesigner/d' /tmp/chrome.old > $(chrome_install_file)"; \
		for CHRM in $(chrome_install_text); do \
			cp $(chrome_install_file) /tmp/chrome.old; \
			sudo bash -c "sed -e'\$$a$$CHRM' /tmp/chrome.old > $(chrome_install_file)"; \
			rm /tmp/chrome.old; \
		done; \
	fi;

uninstall-text:
	if test -f $(chrome_install_file); then \
		cp $(chrome_install_file) /tmp/chrome.old; \
		sudo bash -c "sed -e'/aspdesigner/d' /tmp/chrome.old > $(chrome_install_file)"; \
		rm /tmp/chrome.old; \
	fi;

EXTRA_DIST = $(jar_files) $(xpi_files)

CLEANFILES =			\
	chrome/$(packagename).jar	\
	$(packagename).xpi \
	content/JSCall.js
