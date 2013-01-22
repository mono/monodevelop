# F# Language Support for Emacs

Features:

- Interactive F# buffer
- Indentation
- Syntax highlighter
- Experimental support for tooltips and [autocompletion](README-intellisense.md)


### Installation

`fsharp-mode` is available on [MELPA](http://melpa.milkbox.net).

To download it,`package.el` is the built-in package manager in Emacs 24+. On Emacs 23
you will need to get [package.el](http://bit.ly/pkg-el23) yourself if you wish to use it.

If you're not already using MELPA, add this to your
`~/.emacs.d/init.el` (or equivalent) and load it with <kbd>M-x eval-buffer</kbd>.

```lisp
(require 'package)
(add-to-list 'package-archives
             '("melpa" . "http://melpa.milkbox.net/packages/") t)
(package-initialize)
```

And then you can install fsharp-mode with the following command:

<kbd>M-x package-install [RET] fsharp-mode [RET]</kbd>

or by adding this bit of Emacs Lisp code to your Emacs initialization file(`.emacs` or `init.el`):

```lisp
(when (not (package-installed-p 'fsharp-mode))
  (package-install 'fsharp-mode))
```

If the installation doesn't work try refreshing the package list:

<kbd>M-x package-refresh-contents [RET]</kbd>

### Manual installation

In order to install the F# mode, you have to tell Emacs where to find it. This is done by
adding some commands to the init file.

Copy the `fsharpbinding/emacs/` directory in your `~/.emacs.d`
directory (or a place of your choice) and rename it to `fsharp`
Assuming you now have a `~/.emacs.d/fsharp` directory, copy the following lines to your
init file (usually `~/.emacs` or `init.el`).

```lisp
(setq load-path (cons "~/.emacs.d/fsharp" load-path))
(setq auto-mode-alist (cons '("\\.fs[iylx]?$" . fsharp-mode) auto-mode-alist))
(autoload 'fsharp-mode "fsharp" "Major mode for editing F# code." t)
(autoload 'run-fsharp "inf-fsharp" "Run an inferior F# process." t)
```

### Configuration

If `fsc` and `fsi` are in your path, that's all you need. Otherwise,
you can add these two following lines to set the path to the compiler
and interactive F#.

On Windows (adapt the path if needed):

```lisp
(setq inferior-fsharp-program "\"c:\\Program Files\\Microsoft F#\\v4.0\\Fsi.exe\"")
(setq fsharp-compiler "\"c:\\Program Files\\Microsoft F#\\v4.0\\Fsc.exe\"")
```

On Unix the interactive defaults to `fsharpi --readline-`, which should work with the open source release. Otherwise (adapt the path if needed):

```lisp
(setq inferior-fsharp-program "mono ~/fsi.exe --readline-")
(setq fsharp-compiler "mono ~/fsc.exe")
```

### Bindings

If you are new to Emacs, you might want to use the menu (call
menu-bar-mode if you don't see it). However, it's usually faster to learn
a few useful bindings:

- <kbd>C-c C-r</kbd>:       Evaluate region
- <kbd>C-c C-f</kbd>:       Load current buffer into toplevel
- <kbd>C-c C-e</kbd>:       Evaluate current toplevel phrase
- <kbd>C-M-x</kbd>:         Evaluate current toplevel phrase
- <kbd>C-M-h</kbd>:         Mark current toplevel phrase
- <kbd>C-c C-s</kbd>:       Show interactive buffer
- <kbd>C-c C-c</kbd>:       Compile with fsc
- <kbd>C-c x</kbd>:         Run the executable
- <kbd>C-c C-a</kbd>:       Open alternate file (.fsi or .fs)
- <kbd>C-c l</kbd>:         Shift region to left
- <kbd>C-c r</kbd>:         Shift region to right
- <kbd>C-c <up></kbd>:      Move cursor to the beginning of the block
- <kbd>C-c C-p</kbd>:       Load a project for autocompletion and tooltips
- <kbd>C-c C-t</kbd>:       Request a tooltip for symbol at point

To interrupt the interactive mode, use <kbd>C-c C-c</kbd>. This is useful if your
code does an infinite loop or a very long computation.

If you want to shift the region by 2 spaces, use: <kbd>M-2 C-c r</kbd>

In the interactive buffer, use <kbd>M-RET</kbd> to send the code without
explicitly adding the `;;` thing.

