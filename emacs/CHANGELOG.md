## 1.5.0 (2014-11-25)

Incorporate FSharp.AutoComplete version 0.13.3, which has corrected help text for the parse command and uses FCS 0.0.79.

Features:
  - #235: Support multiple projects simultaneously

Bugfixes:
  - #824: Emacs should give a better error message if fsautocomplete not found
  - #808: C-c C-p gives an error if no project file above current file's directory
  - #790: Can't make fsac requests in indirect buffers
  - #754: Compiler warnings when installing fsharp-mode from MELPA

## 1.4.2 (2014-10-30)

Incorporate FSharp.AutoComplete version 0.13.2, which returns more information if the project parsing fails.

Features:
  - #811: Return exception message on project parsing fail

## 1.4.1 (2014-10-30)

Incorporate FSharp.AutoComplete version 0.13.1, which contains a fix for goto definition.

Bugfixes:
  - #787: Correct off-by-one error in fsac goto definition

## 1.4.0 (2014-10-26)

The main feature of this release is that the project parsing logic has
been moved to FSharp.Compiler.Service as part of fixing #728.

Features:
  - #319: Better error feedback when no completion data available
  - #720: Rationalise emacs testing, also fixed #453

Bugfixes:
  - #765: Do not offer completions in irrelevant locations (strings/comments)
  - #721: Tests for Emacs syntax highlighting, and resultant fixes
  - #248: Run executable file now uses output from FSharp.AutoComplete
  - #728: Fix project support on Windows

## 1.3.0 (2014-08-28)

Changes by @rneatherway unless otherwise noted.

Major changes in this release are performance improvements thanks to @juergenhoetzel (avoiding parsing the current buffer unless necessary), and
fixes for syntax highlighting.


Features:
  - #481: Only parse the current buffer if it is was modified (@juergenhoetzel)

Bugfixes:
  - #619: Disable FSI syntax highlighting
  - #670: Prevent double dots appearing during completion
  - #485: Fetch SSL certs before building exe in emacs dir
  - #496: Corrections to emacs syntax highlighting
  - #597: Highlight preprocessor and async
  - #605: Add FSI directives to syntax highlighting of emacs
  - #571: Correct range-check for emacs support
  - #572: Ensure fsi prompt is readonly
  - #452: Fetch SSL certs before building exe in emacs dir
  
