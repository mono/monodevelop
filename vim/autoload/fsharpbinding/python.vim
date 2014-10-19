" Vim autoload functions
" Language:     F#
" Last Change:  Mon 20 Oct 2014 12:20:30 AM CEST
" Maintainer:   Gregor Uhlenheuer <kongo2002@googlemail.com>

if exists('g:loaded_autoload_fsharpbinding_python')
    finish
endif
let g:loaded_autoload_fsharpbinding_python = 1

let s:cpo_save = &cpo
set cpo&vim


function! fsharpbinding#python#LoadLogFile()
python << EOF
print fsautocomplete.logfiledir
EOF
endfunction


function! fsharpbinding#python#ParseProject(...)
    if a:0 > 0
    python << EOF
fsautocomplete.project(vim.eval("a:1"))
EOF
    else
    python << EOF
if proj_file:
    fsautocomplete.project(proj_file)
EOF
    endif
endfunction


function! fsharpbinding#python#BuildProject(...)
    try
        if a:0 > 0
            execute '!xbuild ' . a:1
        else
            let pn = pyeval('proj_file')
            execute '!xbuild ' . pn
        endif
    catch
        echo "failed to execute build"
    endtry
endfunction


function! fsharpbinding#python#GetInfo()
    let line = line('.')
    let c = col('.')
    let err = s:findErrorByPos(line, c)
    if empty(err) == 0
        echo err['text']
    else
        call fsharpbinding#python#TypeCheck()
    endif
endfunction


function! fsharpbinding#python#TypeCheck()
    python << EOF
b = vim.current.buffer
fsautocomplete.parse(b.name, True, b)
row, col = vim.current.window.cursor
res = fsautocomplete.tooltip(b.name, row, col)
lines = res.splitlines()
first = ""
if len(lines):
    first = lines[0]
if first == 'Multiple items' or first.startswith('type'):
    vim.command('echo "%s"' % res)
else:
    vim.command('echo "%s"' % first)
EOF
endfunction


" probable loclist format
" {'lnum': 2, 'bufnr': 1, 'col': 1, 'valid': 1, 'vcol': 1, 'nr': -1, 'type': 'W', 'pattern': '', 'text': 'Expected an assignment or functi on call and instead saw an expression.'}

" fsautocomplete format
" {"StartLine":4,"StartLineAlternate":5,"EndLine":4,"EndLineAlternate":5,"StartColumn":0,"EndColumn":4,"Severity":"Error","Message":"The value or constructor 'asdf' is not defined","Subcategory":"typecheck","FileName":"/Users/karlnilsson/code/kjnilsson/fsharp-vim/test.fsx"}
function! s:findErrorByPos(line, col)
    for e in b:errs
        if e['lnum'] == a:line
            if e['col'] < a:col && e['ecol'] >= a:col
                return e
            endif
        endif
    endfor
    return {}
endfunction


function! fsharpbinding#python#FindErrors()
    let result = []
    let buf = bufnr('%')
    try
        let errs = pyeval('fsautocomplete.errors(vim.current.buffer.name, True, vim.current.buffer)')
        for e in errs
            call add(result,
                \{'lnum': e['StartLineAlternate'],
                \'col': e['StartColumn'],
                \'ecol': e['EndColumn'],
                \'type': e['Severity'][0],
                \'text': e['Message'],
                \'pattern': '\%' . e['StartLineAlternate'] . 'l\%>' . e['StartColumn'] .  'c\%<' . (e['EndColumn'] + 1) . 'c',
                \'bufnr': buf,
                \'valid': 1 })
        endfor
    catch
        echohl WarningMsg "failed to parse file"
    endtry
    return result
endfunction


function! fsharpbinding#python#Complete(findstart, base)
    let line = getline('.')
    let idx = col('.') - 1 "1-indexed

    " if there are trailing characters move one further back
    if len(line) >= idx
        let idx -= 1
    endif

    while idx > 0
        let c = line[idx]
        if c == ' ' || c == '.'
            let idx += 1
            break
        endif
        let idx -= 1
    endwhile

    if a:findstart == 1
        return idx
    else

    python << EOF
b = vim.current.buffer
row, col = vim.current.window.cursor
line = b[row - 1]
if col > len(line):
    col = len(line)
fsautocomplete.parse(b.name, True, b)
vim.command('return %s' % fsautocomplete.complete(b.name, row, col, vim.eval('a:base')))
EOF
    endif
endfunction


function! fsharpbinding#python#GoBackFromDecl()
    python << EOF
b = vim.current.buffer
w = vim.current.window
try:
    f, cur = Statics.locations.pop()
    # declared within same file
    if b.name == f:
        w.cursor = cur
    else:
        pyvim.jump(f, cur)
except:
    print "no more locations"
EOF
endfunction


function! fsharpbinding#python#GotoDecl()
    python << EOF
b = vim.current.buffer
w = vim.current.window
fsautocomplete.parse(b.name, True, b)
row, col = vim.current.window.cursor
res = fsautocomplete.finddecl(b.name, row, col)
Statics.locations.append((b.name, w.cursor))
if res == None:
    vim.command('echo "declaration not found"')
else:
    f, cur = res
    # declared within same file
    if b.name == f:
        w.cursor = cur
    else:
        pyvim.jump(f, cur)
EOF
endfunction


let &cpo = s:cpo_save
unlet s:cpo_save

" vim: sw=4 et sts=4
