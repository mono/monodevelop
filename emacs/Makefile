# Directories
test_d = test/
temp_d = tmp/

# Elisp files required for tests.
integration_tests = $(test_d)integration-tests.el
unit_tests        = $(filter-out $(integration_tests), $(wildcard $(test_d)*tests.el))
utils             = $(test_d)test-common.el $(test_d)pos-tip-mock.el

# Emacs command format.
emacs            = emacs
load_files       = $(patsubst %,-l %, $(utils))
load_unit_tests  = $(patsubst %,-l %, $(unit_tests))
load_integration_tests = $(patsubst %,-l %, $(integration_tests))
emacs_opts       = --batch -f run-fsharp-tests

# Environment
HOME     := $(temp_d)
TESTMODE := melpa
export $(HOME) $(TESTMODE)

# ----------------------------------------------------------------------------

.PHONY : env test unit-test integration-test

clean :
	rm -f  *.elc
	rm -f  $(test_d)*.elc
	rm -fr $(test_d).emacs.d

# Tests

test : unit-test integration-test

unit-test :
	$(emacs) $(load_files) $(load_unit_tests) $(emacs_opts)

integration-test :
	cd $(test_d)
	$(emacs) $(load_files) $(load_integration_tests) $(emacs_opts)
