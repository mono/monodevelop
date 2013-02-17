# Directories
test_d = test/
deps_d = deps/
temp_d = tmp/
HOME  := $(temp_d)
export $(HOME)

# Elisp files required for tests.
src   = $(filter-out fsharp-mode-pkg.el, $(wildcard *.el))
tests = $(filter-out $(test_d)integration-tests.el, $(wildcard $(test_d)*tests.el))
utils = test/test-common.el test/pos-tip-mock.el

# Dependencies to be loaded.
ns_url = https://raw.github.com/chrisbarrett/elisp-namespaces/master/namespaces.el
ns_script = $(deps_d)namespaces.el
depends   = $(ns_script)

# Emacs command format.
emacs      = emacs
load_files = $(patsubst %,-l %, $(depends) $(src) $(utils))
load_tests = $(patsubst %,-l %, $(tests))
emacs_opts = --batch -f ert-run-tests-batch-and-exit

# ----------------------------------------------------------------------------

.PHONY : env depends test unit-test integration-test

clean :
	rm -f  *.elc
	rm -fr $(deps_d)
	rm -f  $(test_d)*.elc
	rm -fr $(test_d).emacs.d

# Dependencies

depends      : $(deps_d) $(ns_script)
$(deps_d)    :; mkdir -p $(deps_d)
$(ns_script) :; curl -# $(ns_url) > $(ns_script)

# Tests

test : unit-test integration-test

unit-test : depends
	$(emacs) $(load_files) $(load_tests) $(emacs_opts)

integration-test : depends
	cd $(test_d)
	$(emacs) $(load_files) -l $(test_d)integration-tests.el $(emacs_opts)
