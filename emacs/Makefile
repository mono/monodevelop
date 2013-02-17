# Makefile variables,
makefile_path = $(CURDIR)/$(word $(words $(MAKEFILE_LIST)),$(MAKEFILE_LIST))
makefile_dir  = $(dir $(makefile_path))

# Elisp files required for tests.
src   = $(filter-out fsharp-mode-pkg.el, $(wildcard *.el))
tests = $(filter-out ./test/integration-tests.el, $(wildcard ./test/*tests.el))
utils = test/test-common.el test/pos-tip-mock.el

# Dependencies to be loaded.
deps   = ./deps/
ns_url = https://raw.github.com/chrisbarrett/elisp-namespaces/master/namespaces.el
ns_script = $(deps)namespaces.el
depends   = $(ns_script)

# Emacs command format.
emacs      = emacs
load_files = $(patsubst %,-l %, $(depends) $(src) $(utils))
load_tests = $(patsubst %,-l %, $(tests))
emacs_opts = --batch -f ert-run-tests-batch-and-exit

# ----------------------------------------------------------------------------

.PHONY : env depends test unit-test integration-test

env :
	export HOME=$(makefile_dir)test/

clean :
	rm -f  *.elc
	rm -fr deps
	rm -f  tests/*.elc
	rm -fr tests/.emacs.d

# Dependencies

depends      : $(deps) $(ns_script)
$(deps)      :; mkdir -p $(deps)
$(ns_script) :; curl -# $(ns_url) > $(ns_script)

# Tests

test : unit-test integration-test

unit-test : depends env
	$(emacs) $(load_files) $(load_tests) $(emacs_opts)

integration-test : depends env
	$(emacs) $(load_files) -l test/integration-tests.el $(emacs_opts)
