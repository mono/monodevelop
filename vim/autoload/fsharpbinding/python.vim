" Vim autoload functions
" Language:     F#
" Last Change:  Mon 20 Oct 2014 08:21:43 PM CEST
" Maintainer:   Gregor Uhlenheuer <kongo2002@googlemail.com>

if exists('g:loaded_autoload_fsharpbinding_python')
    finish
endif
let g:loaded_autoload_fsharpbinding_python = 1

let s:cpo_save = &cpo
set cpo&vim

" taken from: http://stackoverflow.com/questions/1533565/how-to-get-visually-selected-text-in-vimscript
function! s:get_visual_selection()
  " Why is this not a built-in Vim script function?!
  let [lnum1, col1] = getpos("'<")[1:2]
  let [lnum2, col2] = getpos("'>")[1:2]
  let lines = getline(lnum1, lnum2)
  let lines[-1] = lines[-1][: col2 - (&selection == 'inclusive' ? 1 : 2)]
  let lines[0] = lines[0][col1 - 1:]
  return join(lines, "\n")
endfunction


function! fsharpbinding#python#LoadLogFile()
python << EOF
print fsautocomplete.logfiledir
EOF
endfunction


function! fsharpbinding#python#ParseProject(...)
    execute 'wa'
    if a:0 > 0
    python << EOF
fsautocomplete.project(vim.eval("a:1"))
EOF
    else
    python << EOF
v = vim.current.buffer.vars
if "proj_file" in v:
    fsautocomplete.project(v["proj_file"])
EOF
    endif
endfunction


function! fsharpbinding#python#BuildProject(...)
    try
        execute 'wa'
        if a:0 > 0
            execute '!xbuild ' . fnameescape(a:1)
        elseif exists('b:proj_file')
            execute '!xbuild ' . fnameescape(b:proj_file)
        else
            echoe "no project file could be found"
        endif
    catch
        echoe "failed to execute build. ex: " v:exception
    endtry
endfunction


function! fsharpbinding#python#RunProject(...)
    try
        execute 'wa'
        if a:0 > 0
            execute '!mono ' . fnameescape(a:1)
        elseif exists('b:proj_file')
            let cmd = 'Statics.projects["' . b:proj_file . '"]["Output"]'
            echom "runproj pre pyeval " cmd
            let target = pyeval(cmd)
            echom "target" target
            execute '!mono ' . fnameescape(target)
        else
            echoe "no project file could be found"
        endif
    catch
        echoe "failed to execute build. ex: " v:exception
    endtry
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
if first.startswith('Multiple') or first.startswith('type'):
    vim.command('echo "%s"' % res)
else:
    vim.command('echo "%s"' % first)
EOF
endfunction


" probable loclist format
" {'lnum': 2, 'bufnr': 1, 'col': 1, 'valid': 1, 'vcol': 1, 'nr': -1, 'type': 'W', 'pattern': '', 'text': 'Expected an assignment or function call and instead saw an expression.'}

" fsautocomplete format
" {"StartLine":4,"StartLineAlternate":5,"EndLine":4,"EndLineAlternate":5,"StartColumn":0,"EndColumn":4,"Severity":"Error","Message":"The value or constructor 'asdf' is not defined","Subcategory":"typecheck","FileName":"/Users/karlnilsson/code/kjnilsson/fsharp-vim/test.fsx"}
function! fsharpbinding#python#FindErrors()
    let result = []
    let buf = bufnr('%')
    try
        let errs = pyeval('fsautocomplete.errors(vim.current.buffer.name, True, vim.current.buffer)')
        for e in errs
            call add(result,
                \{'lnum': e['StartLineAlternate'],
                \ 'col': e['StartColumn'],
                \ 'type': e['Severity'][0],
                \ 'text': e['Message'],
                \ 'hl': '\%' . e['StartLineAlternate'] . 'l\%>' . e['StartColumn'] .  'c\%<' . (e['EndColumn'] + 1) . 'c',
                \ 'bufnr': buf,
                \ 'valid': 1 })
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

function! fsharpbinding#python#OnBufEnter()
python << EOF
file_dir = vim.eval("expand('%:p:h')")
fsi.cd(file_dir)
v = vim.current.buffer.vars
if "proj_file" in v:
    fsautocomplete.project(v["proj_file"])
EOF
    "set makeprg
    if !filereadable(expand("%:p:h")."/Makefile")
        if exists('b:proj_file')
            let &l:makeprg='xbuild ' . b:proj_file . ' /verbosity:quiet /nologo /p:Configuration=Debug'
            setlocal errorformat=\ %#%f(%l\\\,%c):\ %m
        endif
    endif
endfunction

function! fsharpbinding#python#FsiPurge()
    let prelude = pyeval('fsi.purge()')
    for l in l:prelude
        echom l
    endfor
endfunction

function! fsharpbinding#python#FsiReset(fsi_path)
    python << EOF
fsi.shutdown()
Statics.fsi = FSharpInteractive(vim.eval('a:fsi_path'))
fsi = Statics.fsi
fsi.cd(vim.eval("expand('%:p:h')"))
EOF
    echo "fsi reset"
endfunction

function! fsharpbinding#python#FsiSend(text)
python << EOF
#file_dir = vim.eval("expand('%:p:h')")
path = vim.current.buffer.name
(row, col) = vim.current.window.cursor
#fsi.cd(file_dir)
fsi.set_loc(path, row)
fsi.send(vim.eval('a:text'))
EOF
endfunction

function! fsharpbinding#python#FsiEval(text)
    try
    "clear anything in the buffer
        call fsharpbinding#python#FsiPurge()
        call fsharpbinding#python#FsiSend(a:text)
        let lines = pyeval('fsi.read_until_prompt()')
        for l in lines
            echom l
        endfor
    catch
        echohl WarningMsg "fsi eval failure" 
    endtry
endfunction

function! fsharpbinding#python#FsiSendLine()
    let text = getline('.')
    call fsharpbinding#python#FsiEval(text)
    exec "normal" "j"
endfunction

function! fsharpbinding#python#FsiSendLineSilent()
    let text = getline('.')
    call fsharpbinding#python#FsiSend(text)
    exec "normal" "j"
endfunction

function! fsharpbinding#python#FsiSendSel()
    let text = s:get_visual_selection()
    call fsharpbinding#python#FsiEval(text)
endfunction

function! fsharpbinding#python#FsiSendSelSilent()
    let text = s:get_visual_selection()
    call fsharpbinding#python#FsiSend(text)
endfunction

let &cpo = s:cpo_save
unlet s:cpo_save

" vim: sw=4 et sts=4
