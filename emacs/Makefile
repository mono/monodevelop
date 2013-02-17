# Elisp files required for tests.
src   = $(filter-out fsharp-mode-pkg.el, $(wildcard *.el))
tests = $(filter-out ./test/integration-tests.el, $(wildcard ./test/*tests.el))
utils = test/test-common.el test/pos-tip-mock.el

# Dependencies to be loaded.
ns_url    = https://raw.github.com/chrisbarrett/elisp-namespaces/master/namespaces.el
ns_script = deps/namespaces.el
deps      = $(ns_script)

# Emacs command format.
emacs     = emacs
files     = $(patsubst %,-l %, $(deps) $(src) $(utils) $(tests))
test_opts = $(files) --batch -f ert-run-tests-batch-and-exit

.PHONY: test

test: deps
	$(emacs) $(test_opts)

deps:
	mkdir -p deps
	curl -# $(ns_url) > $(ns_script)

clean:
	rm -f  *.elc
	rm -f  tests/*.elc
	rm -fr deps
