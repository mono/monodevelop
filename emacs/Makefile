# Directories
base_d = $(abspath ..)/
test_d = $(abspath test)/
tmp_d  = $(abspath tmp)/
bin_d  = $(abspath bin)/

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

# HACK: Vars for manually building the ac binary.
# We should be really able to use the makefile for this...
ac_exe    = $(bin_d)fsautocomplete.exe
ac_fsproj = $(base_d)FSharp.AutoComplete/FSharp.AutoComplete.fsproj
ac_out    = $(base_d)FSharp.AutoComplete/bin/Debug/

# Environment
HOME     := $(tmp_d)
export HOME

# ----------------------------------------------------------------------------

.PHONY : env test unit-test integration-test

clean :
	rm -f  *.elc
	rm -f  $(test_d)*.elc
	rm -fr $(bin_d)
	rm -rf $(tmp_d)

# Tests

test unit-test :
	$(emacs) $(load_files) $(load_unit_tests) $(emacs_opts)

integration-test : $(ac_exe)
	cd $(test_d) ; $(emacs) $(load_files) $(load_integration_tests) $(emacs_opts)

test-all : unit-test integration-test

# F# Completion Binary

$(ac_exe): bin
	xbuild $(ac_fsproj) /property:OutputPath=$(bin_d)

bin :; mkdir -p $(bin_d)
