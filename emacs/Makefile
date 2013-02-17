# Elisp files required for tests.
src   = $(filter-out fsharp-mode-pkg.el, $(wildcard *.el))
tests = test/basic-tests.el test/fsharp-doc-tests.el test/unit-tests.el
utils = test/test-utilities.el test/pos-tip-mock.el

# Dependencies to be loaded.
ns_url    = https://raw.github.com/chrisbarrett/elisp-namespaces/master/namespaces.el
ns_script = deps/namespaces.el
deps      = $(ns_script)

# Emacs command format.
emacs     = emacs
command   = '(ert-run-tests-batch-and-exit)'
files     = $(patsubst %,-l %, $(deps) $(src) $(utils) $(tests))
test_opts = $(files) --batch --eval $(command)

.PHONY: test unittest integrationtest

test: deps
	$(emacs) $(test_opts)

deps:
	mkdir -p deps
	curl $(ns_url) > $(ns_script)

clean:
	rm -f  *.elc
	rm -f  tests/*.elc
	rm -fr deps
