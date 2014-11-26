# fsharp-mode

Provides support for the F# language in Emacs. Includes the following features:

- Support for F# Interactive
- Displays type signatures and tooltips
- Provides syntax highlighting and indentation.
- Intellisense support.

The following features are under development:

- Intelligent indentation

Requires Emacs 24+ and F# 3.0. Without F# 3.0 the background compiler
process will not function correctly.

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
    make test-all # optional
    make install
    ```

2. Add the following to your init.el:
    ```lisp
    (add-to-list 'load-path "~/.emacs.d/fsharp-mode/")
    (autoload 'fsharp-mode "fsharp-mode"     "Major mode for editing F# code." t)
    (add-to-list 'auto-mode-alist '("\\.fs[iylx]?$" . fsharp-mode))
    ```

Note that if you do not use `make install`, which attempts to download
the dependencies from MELPA for you, then you must ensure that you have
installed them yourself. A list can be found in `fsharp-mode-pkg.el`.

If you run into any problems with installation, please check that you
have Emacs 24 on your PATH using `emacs --version`.
Note that OSX comes with Emacs 22 by default and installing a .app of
Emacs 24 will not add it to your PATH. One option is:

`alias emacs='/Applications/Emacs.app/Contents/MacOS/Emacs'`

## Usage

fsharp-mode should launch automatically whenever you open an F#
buffer. When the intellisense process is running, the following features will be available:

1. Type information for symbol at point will be displayed in the minibuffer.
2. Errors and warnings will be automatically highlighted, with mouseover
   text. Jump to the next and previous error using <kbd>M-n</kbd> and <kbd>M-p</kbd>.
3. To display a tooltip, move the cursor to a symbol and press
   <kbd>C-c C-t</kbd> (default).
4. To jump to the definition of a symbol at point, use <kbd>C-c C-d</kbd>.
5. Completion will be invoked automatically on dot, as in Visual Studio.
   It may be invoked manually using `fsharp-ac/complete-at-point`,
   bound by default to <kbd>C-c C-.</kbd>.
6. To stop the intellisense process for any reason, use <kbd>C-c C-q</kbd>.

### Projects

fsharp-mode offers intellisense for projects using the MSBuild/`.fsproj`
format. This allows project files to be shared with other developers using
Visual Studio and Xamarin Studio/Monodevelop. To create a new project file,
it is recommended that you take an existing project file and modify the list
of source files. One such project file can be found in the fsharp-mode repository [here](https://github.com/fsharp/fsharpbinding/blob/master/emacs/test/Test1/Test1.fsproj).

If, on loading a new `.fs` file, the intellisense process is not running and
a `.fsproj` file is found in the current or an enclosing directory, the
intellisense process will be launched, and the project loaded.

Currently intellisense features can be offered for just one project at
a time. To load a new F# project, use <kbd>C-c C-p</kbd>.

### Scripts

F# scripts (`.fsx` files) are standalone, and require no project file. As a
result, intellisense can be offered for many script files concurrently with a project. If you wish open a script file and the intellisense process is not yet running, it will be launched automatically.

## Configuration

### Compiler and REPL paths

The F# compiler and interpreter should be set to good defaults for your
OS as long as the relevant executables can be found on your PATH. If you
have a non-standard setup you may need to configure these paths manually.

On Windows:

```lisp
(setq inferior-fsharp-program "\"c:\\Path\To\Fsi.exe\"")
(setq fsharp-compiler "\"c:\\Path\To\Fsc.exe\"")
```

On Unix-like systems, you must use the *--readline-* flag to ensure F#
Interactive will work correctly with Emacs. Typically `fsi` and `fsc` are
invoked through the shell scripts `fsharpi` and `fsharpc`.

```lisp
(setq inferior-fsharp-program "path/to/fsharpi --readline-")
(setq fsharp-compiler "path/to/fsharpc")
```

### Behavior

There are a few variables you can adjust to change how fsharp-mode behaves:

- `fsharp-ac-use-popup`: Show tooltips using a popup at the cursor
  position. If set to nil, display the tooltip in a split window.

- `fsharp-doc-idle-delay`: Set the time (in seconds) to wait before
  showing type information in the minibuffer.
  
- `fsharp-ac-intellisense-enabled`: This mode overrides some aspects of
  auto-complete configuration and runs the background process automatically.
  Set to nil to prevent this. Note that this will only prevent the background
  process from being launched in the *future*. If it is already running you
  will also need to quit it using <kbd>C-c C-q</kbd>.

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
- <kbd>M-n</kbd>:           Go to next error
- <kbd>M-p</kbd>:           Go to previous error

To interrupt the interactive mode, use <kbd>C-c C-c</kbd>. This is useful if your
code does an infinite loop or a very long computation.

If you want to shift the region by 2 spaces, use: <kbd>M-2 C-c r</kbd>

In the interactive buffer, use <kbd>M-RET</kbd> to send the code without
explicitly adding the `;;` thing.

For key bindings that will be more familiar to users of Visual Studio, adding
the following to your `init.el` may be a good start:

```lisp
(add-hook 'fsharp-mode-hook
 (lambda ()
   (define-key fsharp-mode-map (kbd "M-RET") 'fsharp-eval-region)
   (define-key fsharp-mode-map (kbd "C-SPC") 'fsharp-ac/complete-at-point)))
```


## Troubleshooting

`fsharp-mode` is still under development, so you may encounter some issues. Please report them so we can improve things! Either open an issue on [Github](https://github.com/fsharp/fsharpbinding/) with the label `Emacs`, or email the [mailing list](http://groups.google.com/group/fsharp-opensource).

### `fsharp-ac-debug`

If you set the variable `fsharp-ac-debug` to a non-`nil` value, e.g. `(setq fsharp-ac-debug 0)`, then some debug output will be seen in the buffer `*fsharp-debug*`. Setting `fsharp-ac-debug` to an 1 or 2 will cause a truncated or complete copy of communication between Emacs and the background intellisense process to be logged in `*fsharp-debug*`. This can make things rather slow, but would be useful for bug reports.

### `Error: F# completion process produced malformed JSON.`

This is probably the result of the background intellisense process crashing and printing a stacktrace in plain text. Please report the crash, preferably with how to reproduce, and the contents of the `*fsharp-complete*` buffer.

### `Error: background intellisense process not running.`

You have requested some intellisense information (such as completions or a tooltip), but the background process is not running. The most common cause of this is that a standard `.fs` file is being visited in the current buffer, but a `.fsproj` project file was not found in the same directory. Try loading one with <kbd>C-c C-p</kbd>.

### `Error: this file is not part of the loaded project.`

In this case you have requested intellisense for the visited file, which is a standard `.fs` file *not* included in the current loaded project. This mode can currently only provide intellisense for one project at a time. Try loading the appropriate project with <kbd>C-c C-p</kbd>.

### Windows completion menu performance

There are some issues with the `pos-tip` library used to display the documentation tooltips for completions. This can cause sluggish performance when scrolling through the list if you try to move up or down just before the tooltip is displayed. We are looking into proper solutions for this with the `pos-tip` maintainer. For now you can work around the issue with `(setq ac-quick-help-prefer-pos-tip nil)`. This will use an alternative method for displaying these tooltips that is faster but uglier.

### Installing from Git

If you installed by cloning the git repository and you are having problem, please sanity check by running `make test-all` in the `emacs` folder.

## Contributing

This project is maintained by the
[F# Software Foundation](http://fsharp.org/), with the repository hosted
on [GitHub](https://github.com/fsharp/fsharpbinding).

Pull requests are welcome. Please run the test-suite with `make
test-all` before submitting a pull request.
