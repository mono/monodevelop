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
  
