# Directories

base_d = $(abspath ..)
bin_d  = $(abspath ftplugin/bin)

# HACK: Vars for manually building the ac binary.
# We should be really able to use the top-level makefile for this...
ac_exe    = $(bin_d)/fsautocomplete.exe
ac_fsproj = $(base_d)/FSharp.AutoComplete/FSharp.AutoComplete.fsproj
ac_out    = $(base_d)/FSharp.AutoComplete/bin/Debug/

# Installation paths.
dest_root = $(HOME)/.vim/bundle/fsharpbinding-vim/
dest_bin  = $(dest_root)/ftplugin/bin/

# ----------------------------------------------------------------------------

# Building

$(ac_exe) : $(bin_d) ~/.config/.mono/certs
	xbuild $(ac_fsproj) /property:OutputPath="$(bin_d)"

~/.config/.mono/certs:
	mozroots --import --sync --quiet

install : $(ac_exe) $(dest_root) $(dest_bin)
	rm -r $(dest_root)
	mkdir -p $(dest_root)
	mkdir -p $(dest_root)/ftplugin
	mkdir -p $(dest_root)/ftplugin/bin
	mkdir -p $(dest_root)/ftdetect
	mkdir -p $(dest_root)/syntax
	
	cp syntax/fsharp.vim $(dest_root)/syntax
	cp ftdetect/fsharp.vim $(dest_root)/ftdetect/fsharp.vim
	cp ftplugin/fsharp.vim $(dest_root)/ftplugin/fsharp.vim
	cp ftplugin/pyvim.py $(dest_root)/ftplugin/pyvim.py
	cp ftplugin/fsharpvim.py $(dest_root)/ftplugin/fsharpvim.py
	cp -R ftplugin/bin $(dest_root)/ftplugin

$(dest_root) :; mkdir -p $(dest_root)
$(dest_bin)  :; mkdir -p $(dest_bin)
$(bin_d)     :; mkdir -p $(bin_d)

# Cleaning

clean : 
	rm -rf $(bin_d)
