" Vim filetype plugin
" Language:     F#
" Last Change:  Thu 23 Oct 2014 08:39:53 PM CEST
" Maintainer:   Gregor Uhlenheuer <kongo2002@googlemail.com>

"todo: provider solution context for project builds

if exists('b:did_ftplugin')
    finish
endif
let b:did_ftplugin = 1

let s:cpo_save = &cpo
set cpo&vim

let s:candidates = [ 'fsi',
            \ 'fsi.exe',
            \ 'fsharpi',
            \ 'fsharpi.exe' ]

if !exists('g:fsharp_interactive_bin')
    let g:fsharp_interactive_bin = ''
    for c in s:candidates
        if executable(c)
            let g:fsharp_interactive_bin = c
        endif
    endfor
endif

" check for python support
if has('python')
    python <<EOF
import vim
import os
fsharp_dir = vim.eval("expand('<sfile>:p:h')")
file_dir = vim.eval("expand('%:p:h')")
sys.path.append(fsharp_dir)

from fsharpvim import FSAutoComplete,Statics
from fsi import FSharpInteractive 
import pyvim

if Statics.fsac == None:
    debug = vim.eval("get(g:, 'fsharpbinding_debug', 0)") != '0'
    Statics.fsac = FSAutoComplete(fsharp_dir, debug)
if Statics.fsi == None:
    debug = vim.eval("get(g:, 'fsharpbinding_debug', 0)") != '0'
    Statics.fsi = FSharpInteractive(vim.eval('g:fsharp_interactive_bin'))
fsautocomplete = Statics.fsac
fsi = Statics.fsi
fsi.cd(file_dir)
proj_file = None
#find project file if any - assumes fsproj file will be in the same directory as the fs or fsi file
b = vim.current.buffer
x,ext = os.path.splitext(b.name)
if '.fs' == ext or '.fsi' == ext:
    dir = os.path.dirname(os.path.realpath(b.name))
    projs = filter(lambda f: '.fsproj' == os.path.splitext(f)[1], os.listdir(dir))
    if len(projs):
        proj_file = os.path.join(dir, projs[0])
        fsautocomplete.project(proj_file)
fsautocomplete.parse(b.name, True, b)
EOF

    nnoremap <buffer> <leader>t :call fsharpbinding#python#TypeCheck()<cr>
    nnoremap <buffer> <leader>d :call fsharpbinding#python#GotoDecl()<cr>
    nnoremap <buffer> <leader>s :call fsharpbinding#python#GoBackFromDecl()<cr>

    com! -buffer LogFile call fsharpbinding#python#LoadLogFile()
    com! -buffer -nargs=* -complete=file ParseProject call fsharpbinding#python#ParseProject(<f-args>)
    com! -buffer -nargs=* -complete=file BuildProject call fsharpbinding#python#BuildProject(<f-args>)
    
    "fsi
    com! -buffer FsiRead call fsharpbinding#python#FsiPurge()
    com! -buffer FsiReset call fsharpbinding#python#FsiReset(g:fsharp_interactive_bin)
    com! -buffer -nargs=1 FsiEval call fsharpbinding#python#FsiEval(<q-args>)

    nnoremap  :<C-u>call fsharpbinding#python#FsiSendLine()<cr>
    vnoremap  :<C-u>call fsharpbinding#python#FsiSendSel()<cr>
    nnoremap <C-> :<C-u>call fsharpbinding#python#FsiSendLineSilent()<cr>
    vnoremap <C-> :<C-u>call fsharpbinding#python#FsiSendSelSilent()<cr>

    augroup fsharpbindings_au
        au!
        " closing the scratch window after leaving insert mode
        " is common practice
        au InsertLeave *.fs? if pumvisible() == 0|pclose|endif
    augroup END

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
