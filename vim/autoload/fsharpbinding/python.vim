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

" Vim73-compatible version of pyeval
" taken from: http://stackoverflow.com/questions/13219111/how-to-embed-python-expression-into-s-command-in-vim
function s:pyeval(expr)
    if version > 703
        return pyeval(a:expr)
    endif
python << EOF
import json
arg = vim.eval('a:expr')
result = json.dumps(eval(arg))
vim.command('return ' + result)
EOF
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
    elseif exists('b:proj_file')
    python << EOF
fsautocomplete.project(vim.eval("b:proj_file"))
EOF
    endif
endfunction


function! fsharpbinding#python#BuildProject(...)
    try
        execute 'wa'
        if a:0 > 0
            execute '!xbuild ' . fnameescape(a:1)
        elseif exists('b:proj_file')
            execute '!xbuild ' . fnameescape(b:proj_file) "/verbosity:quiet /nologo"
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
            echom "runproj pre s:pyeval " cmd
            let target = s:pyeval(cmd)
            echom "target" target
            execute '!mono ' . fnameescape(target)
        else
            echoe "no project file could be found"
        endif
    catch
        echoe "failed to execute build. ex: " v:exception
    endtry
endfunction

function! fsharpbinding#python#RunTests(...)
    try
        execute 'wa'
        call fsharpbinding#python#BuildProject()
        if a:0 > 0 && exists('g:fsharp_test_runner')
            execute '!mono ' . g:fsharp_test_runner fnameescape(a:1)
        elseif exists('b:proj_file') && exists('g:fsharp_test_runner')
            let cmd = 'Statics.projects["' . b:proj_file . '"]["Output"]'
            let target = s:pyeval(cmd)
            echom "target" target
            execute '!mono ' . g:fsharp_test_runner fnameescape(target)
        else
            echoe "no project file or test runner could be found"
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
    let b:fsharp_buffer_changed = 0
endfunction

" probable loclist format
" {'lnum': 2, 'bufnr': 1, 'col': 1, 'valid': 1, 'vcol': 1, 'nr': -1, 'type': 'W', 'pattern': '', 'text': 'Expected an assignment or function call and instead saw an expression.'}

" fsautocomplete format
" {"StartLine":4,"StartLineAlternate":5,"EndLine":4,"EndLineAlternate":5,"StartColumn":0,"EndColumn":4,"Severity":"Error","Message":"The value or constructor 'asdf' is not defined","Subcategory":"typecheck","FileName":"/Users/karlnilsson/code/kjnilsson/fsharp-vim/test.fsx"}
function! fsharpbinding#python#CurrentErrors()
    let result = []
    let buf = bufnr('%')
    try
        if version > 703
            let errs = s:pyeval('fsautocomplete.errors_current()')
        else
            " Send a sync parse request if Vim 7.3, otherwise misses response for large files
            let errs = s:pyeval("fsautocomplete.errors(vim.current.buffer.name, True, vim.current.buffer)")
        endif
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
    let b:fsharp_buffer_changed = 0
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

function! fsharpbinding#python#OnBufWritePre()
    "ensure a parse has been requested before BufWritePost is called
    python << EOF
fsautocomplete.parse(vim.current.buffer.name, True, vim.current.buffer)
EOF
    let b:fsharp_buffer_changed = 0
endfunction

function! fsharpbinding#python#OnInsertLeave()
    if exists ("b:fsharp_buffer_changed") != 0 
        if b:fsharp_buffer_changed == 1
    python << EOF
fsautocomplete.parse(vim.current.buffer.name, True, vim.current.buffer)
EOF
        endif
    endif
endfunction

function! fsharpbinding#python#OnCursorHold()
    if exists ("g:fsharp_only_check_errors_on_write") != 0 
        if g:fsharp_only_check_errors_on_write != 1 && b:fsharp_buffer_changed == 1
            exec "SyntasticCheck"
        endif
    endif
    let b:fsharp_buffer_changed = 0
endfunction

function! fsharpbinding#python#OnTextChanged()
    let b:fsharp_buffer_changed = 1
    "TODO: make an parse_async that writes to the server on a background thread
    python << EOF
fsautocomplete.parse(vim.current.buffer.name, True, vim.current.buffer)
EOF
endfunction

function! fsharpbinding#python#OnTextChangedI()
    let b:fsharp_buffer_changed = 1
endfunction

function! fsharpbinding#python#OnBufEnter()
    let b:fsharp_buffer_changed = 1
    set updatetime=500
python << EOF
fsautocomplete.parse(vim.current.buffer.name, True, vim.current.buffer)

file_dir = vim.eval("expand('%:p:h')")
fsi.cd(file_dir)
if vim.eval("exists('b:proj_file')") == 1:
    fsautocomplete.project(vim.eval("b:proj_file"))
EOF
    "set makeprg
    if !filereadable(expand("%:p:h")."/Makefile")
        if exists('b:proj_file')
            let &l:makeprg=g:fsharp_xbuild_path . ' ' . b:proj_file . ' /verbosity:quiet /nologo /p:Configuration=Debug'
            setlocal errorformat=\ %#%f(%l\\\,%c):\ %m
        endif
    endif
endfunction

function! fsharpbinding#python#FsiReset(fsi_path)
    python << EOF
fsi.shutdown()
Statics.fsi = FSharpInteractive(vim.eval('a:fsi_path'))
fsi = Statics.fsi
fsi.cd(vim.eval("expand('%:p:h')"))
EOF
    exec 'bd fsi-out'
    echo "fsi reset"
endfunction

function! fsharpbinding#python#FsiSend(text)
    python << EOF
path = vim.current.buffer.name
(row, col) = vim.current.window.cursor
fsi.set_loc(path, row)
fsi.send(vim.eval('a:text'))
EOF
endfunction

function! fsharpbinding#python#FsiShow()
    try
        if bufnr('fsi-out') == -1
            exec 'badd fsi-out'
        else
            exec 'vsplit fsi-out'
            setlocal buftype=nofile
            setlocal bufhidden=hide
            setlocal noswapfile
            exec 'wincmd p'
        endif
    catch
        echohl WarningMsg "failed to display fsi output" 
    endtry
endfunction

function! fsharpbinding#python#FsiPurge()
python << EOF
lines = fsi.purge()
for b in vim.buffers:
    if 'fsi-out' in b.name:
        b.append(lines)
        break
EOF
endfunction

function! fsharpbinding#python#FsiRead(time_out)
python << EOF
lines = fsi.read_until_prompt(float(vim.eval('a:time_out')))
for b in vim.buffers:
    if 'fsi-out' in b.name:
        b.append(lines)
        for w in vim.current.tabpage.windows:
            if b.name in w.buffer.name:
                w.cursor = len(b) - 1, 0
                vim.command('exe %s"wincmd w"' % w.number)
                vim.command('exe "normal! G"')
                vim.command('exe "wincmd p"')
                break
        break
#echo first nonempty line
for l in lines:
    if l != "":
        vim.command('echo "%s"' % l)
        break
EOF
endfunction

function! fsharpbinding#python#FsiEval(text)
    try
    "clear anything in the buffer
        call fsharpbinding#python#FsiPurge()
        call fsharpbinding#python#FsiSend(a:text)
        if bufnr('fsi-out') == -1
            exec 'badd fsi-out'
        endif
        call fsharpbinding#python#FsiRead(5)
    catch
        echohl WarningMsg "fsi eval failure" 
    endtry
endfunction

function! fsharpbinding#python#FsiSendLine()
    let text = getline('.')
    call fsharpbinding#python#FsiEval(text)
    exec 'normal j'
endfunction

function! fsharpbinding#python#FsiSendSel()
    let text = s:get_visual_selection()
    call fsharpbinding#python#FsiEval(text)
endfunction

let &cpo = s:cpo_save
unlet s:cpo_save

" vim: sw=4 et sts=4
