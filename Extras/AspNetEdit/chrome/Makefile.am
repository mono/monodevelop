packagename = aspdesigner
extension_guid="{d7d824a2-030d-11db-9804-b622a1ef5492}"
packagedir=$(MD_ADDIN_DIR)/AspNetEdit
chromedir = $(MOZILLA_HOME)/chrome
installed_chrome_txt = $(chromedir)/installed-chrome.txt


### Build the JAR ###

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

content/aspdesigner/JSCall.js: $(JSCALL_SCRIPT)
	cp $^ $@

$(packagename).jar: $(jar_files)
	$(ZIP) -q9 $@ $^

all: $(packagename).jar

install-files:
	mkdir -p $(packagedir); \
	cp $(packagename).jar $(packagedir)/

uninstall-files:
	rm -rf $(packagedir)/$(packagename).jar



### Handle the different chrome installation mechanisms ###

if MOZILLA_EXTENSION_TYPE_TEXTFILE
install-data-hook: install-files install-chrome-text
uninstall-hook: uninstall-files uninstall-chrome-text
else
install-data-hook: install-files install-manifest
uninstall-hook: uninstall-files uninstall-manifest
endif

### Install manifest ###

install-manifest:
	cp $(packagename).manifest $(chromedir)/

uninstall-manifest:
	rm -rf $(chromedir)/$(packagename).manifest


### OLD MOZILLA STUFF - NOT TESTED RECENTLY
## TODO: on debian, call update-mozilla-chrome or update-firefox-chrome

old_chrome_install_text = "content,install,url,jar:$(packagedir)/aspdesigner.jar!/content/aspdesigner/"		\
                          "locale,install,url,jar:$(packagedir)/aspdesigner.jar/locale/en-US/aspdesigner/"

install-chrome-text:
	if test -f $(installed_chrome_txt); then \
		cp $(installed_chrome_txt) /tmp/chrome.old; \
		sudo bash -c "sed -e'/aspdesigner/d' /tmp/chrome.old > $(installed_chrome_txt)"; \
		for CHRM in $(chrome_install_text); do \
			cp $(installed_chrome_txt) /tmp/chrome.old; \
			sudo bash -c "sed -e'\$$a$$CHRM' /tmp/chrome.old > $(installed_chrome_txt)"; \
			rm /tmp/chrome.old; \
		done; \
	fi;

uninstall-chrome-text:
	if test -f $(installed_chrome_txt); then \
		cp $(installed_chrome_txt) /tmp/chrome.old; \
		sudo bash -c "sed -e'/aspdesigner/d' /tmp/chrome.old > $(installed_chrome_txt)"; \
		rm /tmp/chrome.old; \
	fi;


EXTRA_DIST = $(jar_files) aspdesigner.manifest.in

CLEANFILES =			\
	$(packagename).jar	\
	content/JSCall.js
