# Directories

base_d = $(abspath ..)
test_d = $(abspath test)
tmp_d  = $(abspath tmp)
bin_d  = $(abspath bin)

# Elisp files required for tests.
src_files         = $(wildcard ./*.el)
obj_files         = $(patsubst %.el,%.elc,$(src_files))
integration_tests = $(test_d)/integration-tests.el
unit_tests        = $(filter-out $(integration_tests), $(wildcard $(test_d)/*tests.el))
utils             = $(test_d)/test-common.el

# Emacs command format.
emacs            = emacs
load_files       = $(patsubst %,-l %, $(utils))
load_unit_tests  = $(patsubst %,-l %, $(unit_tests))
load_integration_tests = $(patsubst %,-l %, $(integration_tests))

# HACK: Vars for manually building the ac binary.
# We should be really able to use the top-level makefile for this...
ac_exe    = $(bin_d)/fsautocomplete.exe
ac_fsproj = $(base_d)/FSharp.AutoComplete/FSharp.AutoComplete.fsproj
ac_out    = $(base_d)/FSharp.AutoComplete/bin/Debug/

# Installation paths.
dest_root = $(HOME)/.emacs.d/fsharp-mode/
dest_bin  = $(HOME)/.emacs.d/fsharp-mode/bin/

# ----------------------------------------------------------------------------

.PHONY : test unit-test integration-test packages clean-elc install byte-compile check-compile check-compile run

# Building

$(ac_exe) : $(bin_d) ~/.config/.mono/certs
	xbuild $(ac_fsproj) /property:OutputPath="$(bin_d)"

~/.config/.mono/certs:
	mozroots --import --sync --quiet

install : $(ac_exe) $(dest_root) $(dest_bin)
# Install elisp packages
	$(emacs) $(load_files) --batch -f load-packages
# Copy files
	for f in $(src_files); do \
		cp $$f $(dest_root) ;\
	done
# Copy bin folder.
	cp -R $(bin_d) $(dest_root)


$(dest_root) :; mkdir -p $(dest_root)
$(dest_bin)  :; mkdir -p $(dest_bin)
$(bin_d)     :; mkdir -p $(bin_d)

# Cleaning

clean : clean-elc
	rm -rf $(bin_d)
	rm -rf $(tmp_d)

clean-elc :
	rm -f  *.elc
	rm -f  $(test_d)/*.elc

# Testing

test unit-test :
	HOME=$(tmp_d) ;\
	$(emacs) $(load_files) --batch -f run-fsharp-unit-tests

integration-test : $(ac_exe) packages
	HOME=$(tmp_d) ;\
	$(emacs) $(load_files) --batch -f run-fsharp-integration-tests

test-all : unit-test integration-test check-compile check-declares

packages :
	HOME=$(tmp_d) ;\
	$(emacs) $(load_files) --batch -f load-packages

byte-compile : packages
	HOME=$(tmp_d) ;\
	$(emacs) -batch --eval "(package-initialize)"\
          --eval "(add-to-list 'load-path \"$(base_d)/emacs\")" \
          -f batch-byte-compile $(src_files)

check-declares : packages
	HOME=$(tmp_d) ;\
	$(emacs) -batch --eval "(package-initialize)"\
          --eval '(when (check-declare-directory "$(base_d)/emacs") (kill-emacs 1)))'

check-compile : packages $(obj_files)

.el.elc:
	HOME=$(tmp_d) ;\
	$(emacs) -batch --eval "(package-initialize)"\
          --eval "(add-to-list 'load-path \"$(base_d)/emacs\")" \
	  --eval '(setq byte-compile-error-on-warn t)' \
          -f batch-byte-compile $<

run : $(ac_exe) packages
	HOME=$(tmp_d) ;\
	$(emacs) $(load_files) -f configure-fsharp-tests
