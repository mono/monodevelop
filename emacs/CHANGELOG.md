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
  
