" Vim filetype plugin
" Language:     F#
" Last Change:  Thu 23 Oct 2014 08:39:04 PM CEST
" Maintainer:   Gregor Uhlenheuer <kongo2002@googlemail.com>

if exists('b:did_ftplugin')
    finish
endif
let b:did_ftplugin = 1

let s:cpo_save = &cpo
set cpo&vim

" check for python support
if has('python')
    python <<EOF
import vim
import os
fsharp_dir = vim.eval("expand('<sfile>:p:h')")
sys.path.append(fsharp_dir)

from fsharpvim import FSAutoComplete,Statics
import pyvim

if Statics.fsac == None:
    Statics.fsac = FSAutoComplete(fsharp_dir)
fsautocomplete = Statics.fsac
b = vim.current.buffer
fsautocomplete.parse(b.name, True, b)
proj_file = None
#find project file if any - assumes fsproj file will be in the same directory as the fs or fsi file
file_name = vim.current.buffer.name
x,ext = os.path.splitext(file_name)
if '.fs' == ext or '.fsi' == ext:
    dir = os.path.dirname(os.path.realpath(file_name))
    projs = filter(lambda f: '.fsproj' == os.path.splitext(f)[1], os.listdir(dir))
    if len(projs):
        proj_file = os.path.join(dir, projs[0])
        fsautocomplete.project(proj_file)
EOF

    nnoremap <buffer> <leader>i :call fsharpbinding#python#TypeCheck()<cr>
    nnoremap <buffer> <leader>d :call fsharpbinding#python#GotoDecl()<cr>
    nnoremap <buffer> <leader>s :call fsharpbinding#python#GoBackFromDecl()<cr>

    com! -buffer LogFile call fsharpbinding#python#LoadLogFile()
    com! -buffer -nargs=* -complete=file ParseProject call fsharpbinding#python#ParseProject(<f-args>)
    com! -buffer -nargs=* -complete=file BuildProject call fsharpbinding#python#BuildProject(<f-args>)

    " omnicomplete
    setlocal omnifunc=fsharpbinding#python#Complete
endif

" enable syntax based folding
setl fdm=syntax

" comment settings
setl formatoptions=croql
setl commentstring=(*%s*)
setl comments=s0:*\ -,m0:*\ \ ,ex0:*),s1:(*,mb:*,ex:*),:\/\/\/,:\/\/

" make ftplugin undo-able
let b:undo_ftplugin = 'setl fo< cms< com< fdm<'

let &cpo = s:cpo_save

" vim: sw=4 et sts=4
