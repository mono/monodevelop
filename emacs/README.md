# fsharp-mode

Provides support for the F# language in Emacs. Includes the following features:

- Support for F# Interactive
- Displays type signatures and tooltips
- Provides syntax highlighting and indentation.

The following features are under development:

- Intelligent indentation
- Experimental [intellisense support](README-intellisense.md).

Requires Emacs 24+.

## Installation

### Package

`fsharp-mode` is available on [MELPA](http://melpa.milkbox.net) and can
be installed using the built-in package manager.

If you're not already using MELPA, add the following to your init.el:

```lisp
;;; Initialize MELPA
(require 'package)
(add-to-list 'package-archives '("melpa" . "http://melpa.milkbox.net/packages/"))
(unless package-archive-contents (package-refresh-contents))
(package-initialize)

;;; Install fsharp-mode
(unless (package-installed-p 'fsharp-mode)
  (package-install 'fsharp-mode))

(require 'fsharp-mode)
```

### Manual installation

1. Clone this repo and run `make install`:
    ```
    git clone git://github.com/fsharp/fsharpbinding.git
    cd fsharpbinding/emacs
    make install
    ```

2. Add the following to your init.el:
    ```lisp
    (add-to-list 'load-path "~/.emacs.d/fsharp-mode/")

    (autoload 'fsharp-mode "fsharp-mode"     "Major mode for editing F# code." t)
    (autoload 'run-fsharp  "inf-fsharp-mode" "Run an inferior F# process." t)
    (add-to-list 'auto-mode-alist '("\\.fs[iylx]?$" . fsharp-mode))
    ```

## Usage

fsharp-mode should launch automatically whenever you open an F#
buffer. It will automatically display type information and provide
tooltips if the current file is part of an F# project.

To display a tooltip, move the cursor to a symbol and press
<kbd>C-c C-t</kbd> (default).

## Configuration

### Compiler and REPL paths

The F# compiler and interpreter should be set to good defaults for your
OS. If you have a non-standard setup you may need to configure these
paths manually.

On Windows:

```lisp
(setq inferior-fsharp-program "\"c:\\Path\To\Fsi.exe\"")
(setq fsharp-compiler "\"c:\\Path\To\Fsc.exe\"")
```

On Unix-like systems, you must use the *--readline-* flag to ensure F#
Interactive will work correctly with Emacs.

```lisp
(setq inferior-fsharp-program "mono path/to/fsharpi.exe --readline-")
(setq fsharp-compiler "mono path/to/fsharpc.exe")
```

### Behavior

There are a few variables you can adjust to change how fsharp-mode behaves:

- `ac-fsharp-use-popup`: Show tooltips using a popup at the cursor
  position. If set to nil, display the tooltip in a split window.

- `fsharp-doc-idle-delay`: Set the time (in seconds) to wait before
  showing type information in the minibuffer.

### Key Bindings

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
- <kbd>C-c C-d</kbd>:       Jump to definition of symbol at point
- <kbd>C-c C-t</kbd>:       Request a tooltip for symbol at point
- <kbd>C-c C-q</kbd>:       Quit current background compiler process

To interrupt the interactive mode, use <kbd>C-c C-c</kbd>. This is useful if your
code does an infinite loop or a very long computation.

If you want to shift the region by 2 spaces, use: <kbd>M-2 C-c r</kbd>

In the interactive buffer, use <kbd>M-RET</kbd> to send the code without
explicitly adding the `;;` thing.

## Contributing

This project is maintained by the
[F# Software Foundation](http://fsharp.org/), with the repository hosted
on [GitHub](https://github.com/fsharp/fsharpbinding).

Pull requests are welcome. Please run the test-suite with `make
test-all` before submitting a pull request.
