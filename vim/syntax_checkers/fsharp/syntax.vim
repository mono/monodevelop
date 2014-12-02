if exists('g:loaded_syntastic_fsharp_syntax_checker')
    finish
endif
let g:loaded_syntastic_fsharp_syntax_checker = 1
let g:fsharp_only_check_errors_on_write = 0
let g:fsharp_xbuild_path = "xbuild"

let s:save_cpo = &cpo
set cpo&vim

function! SyntaxCheckers_fsharp_syntax_IsAvailable() dict
    return has('python')
endfunction

function! SyntaxCheckers_fsharp_syntax_GetLocList() dict
    return fsharpbinding#python#CurrentErrors()
endfunction

call g:SyntasticRegistry.CreateAndRegisterChecker({
    \ 'filetype': 'fsharp',
    \ 'name': 'syntax'})

let &cpo = s:save_cpo
unlet s:save_cpo

" vim: set et sts=4 sw=4:
