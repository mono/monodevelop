;;; fsharp-mode.el --- Support for the F# programming language

;; Copyright (C) 1997 INRIA

;; Author: 1993-1997 Xavier Leroy, Jacques Garrigue and Ian T Zimmerman
;;         2010-2011 Laurent Le Brun <laurent@le-brun.eu>
;;         2012-2014 Robin Neatherway <robin.neatherway@gmail.com>
;; Maintainer: Robin Neatherway
;; Keywords: languages
;; Version: 1.3.0

;; This file is not part of GNU Emacs.

;; This file is free software; you can redistribute it and/or modify
;; it under the terms of the GNU General Public License as published by
;; the Free Software Foundation; either version 3, or (at your option)
;; any later version.

;; This file is distributed in the hope that it will be useful,
;; but WITHOUT ANY WARRANTY; without even the implied warranty of
;; MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;; GNU General Public License for more details.

;; You should have received a copy of the GNU General Public License
;; along with GNU Emacs; see the file COPYING.  If not, write to
;; the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
;; Boston, MA 02110-1301, USA.

(require 'fsharp-mode-completion)
(require 'fsharp-doc)
(require 'inf-fsharp-mode)

;;; Compilation

(defvar fsharp-compile-command
  (or (executable-find "fsharpc")
      (executable-find "fsc"))
  "The program used to compile F# source files.")

(defvar fsharp-build-command
  (or (executable-find "xbuild")
      (executable-find "msbuild"))
  "The command used to build F# projects and solutions.")

(defvar fsharp-compiler nil
  "The command used to compile an individual F# buffer.
This will be set to a sane default, depending the type of file
and whether it is in a project directory.")
(make-variable-buffer-local 'fsharp-compiler)

;;; ----------------------------------------------------------------------------

(defvar fsharp-shell-active nil
  "Non nil when a subshell is running.")

(defvar running-xemacs  (string-match "XEmacs" emacs-version)
  "Non-nil if we are running in the XEmacs environment.")

(defvar fsharp-mode-map nil
  "Keymap used in fsharp mode.")

(unless fsharp-mode-map
  (setq fsharp-mode-map (make-sparse-keymap))
  (if running-xemacs
      (define-key fsharp-mode-map 'backspace 'backward-delete-char-untabify)
    (define-key fsharp-mode-map "\177" 'backward-delete-char-untabify))

  ;; F# bindings
  (define-key fsharp-mode-map "\C-c\C-a" 'fsharp-find-alternate-file)
  (define-key fsharp-mode-map "\C-c\C-c" 'compile)
  (define-key fsharp-mode-map "\C-cx" 'fsharp-run-executable-file)
  (define-key fsharp-mode-map "\M-\C-x" 'fsharp-eval-phrase)
  (define-key fsharp-mode-map "\C-c\C-e" 'fsharp-eval-phrase)
  (define-key fsharp-mode-map "\C-c\C-r" 'fsharp-eval-region)
  (define-key fsharp-mode-map "\C-c\C-f" 'fsharp-load-buffer-file)
  (define-key fsharp-mode-map "\C-c\C-s" 'fsharp-show-subshell)
  (define-key fsharp-mode-map "\M-\C-h" 'fsharp-mark-phrase)

  (define-key fsharp-mode-map (kbd "M-n") 'next-error)
  (define-key fsharp-mode-map (kbd "M-p") 'previous-error)

  (define-key fsharp-mode-map "\C-cl" 'fsharp-shift-region-left)
  (define-key fsharp-mode-map "\C-cr" 'fsharp-shift-region-right)

  (define-key fsharp-mode-map "\C-m"      'fsharp-newline-and-indent)
  (define-key fsharp-mode-map "\C-c:"     'fsharp-guess-indent-offset)
  (define-key fsharp-mode-map [delete]    'fsharp-electric-delete)
  (define-key fsharp-mode-map [backspace] 'fsharp-electric-backspace)
  (define-key fsharp-mode-map (kbd ".") 'fsharp-ac/electric-dot)

  (define-key fsharp-mode-map (kbd "C-c <up>") 'fsharp-goto-block-up)

  (define-key fsharp-mode-map (kbd "C-c C-p") 'fsharp-ac/load-project)
  (define-key fsharp-mode-map (kbd "C-c C-t") 'fsharp-ac/show-tooltip-at-point)
  (define-key fsharp-mode-map (kbd "C-c C-d") 'fsharp-ac/gotodefn-at-point)
  (define-key fsharp-mode-map (kbd "C-c C-q") 'fsharp-ac/stop-process)
  (define-key fsharp-mode-map (kbd "C-c C-.") 'fsharp-ac/complete-at-point)

  (unless running-xemacs
    (let ((map (make-sparse-keymap "fsharp"))
          (forms (make-sparse-keymap "Forms")))
      (define-key fsharp-mode-map [menu-bar] (make-sparse-keymap))
      (define-key fsharp-mode-map [menu-bar fsharp] (cons "F#" map))

      (define-key map [goto-block-up] '("Goto block up" . fsharp-goto-block-up))
      (define-key map [mark-phrase] '("Mark phrase" . fsharp-mark-phrase))
      (define-key map [shift-left] '("Shift region to right" . fsharp-shift-region-right))
      (define-key map [shift-right] '("Shift region to left" . fsharp-shift-region-left))
      (define-key map [separator-2] '("---"))

      ;; others
      (define-key map [run] '("Run..." . fsharp-run-executable-file))
      (define-key map [compile] '("Compile..." . compile))
      (define-key map [switch-view] '("Switch view" . fsharp-find-alternate-file))
      (define-key map [separator-1] '("--"))
      (define-key map [show-subshell] '("Show subshell" . fsharp-show-subshell))
      (define-key map [eval-region] '("Eval region" . fsharp-eval-region))
      (define-key map [eval-phrase] '("Eval phrase" . fsharp-eval-phrase)))))

;;;###autoload
(add-to-list 'auto-mode-alist '("\\.fs[iylx]?$" . fsharp-mode))

(defvar fsharp-mode-syntax-table nil
  "Syntax table in use in fsharp mode buffers.")
(unless fsharp-mode-syntax-table
  (setq fsharp-mode-syntax-table (make-syntax-table))
  ; backslash is an escape sequence
  (modify-syntax-entry ?\\ "\\" fsharp-mode-syntax-table)

  ; ( is first character of comment start
  (modify-syntax-entry ?\( "()1n" fsharp-mode-syntax-table)
  ; * is second character of comment start,
  ; and first character of comment end
  (modify-syntax-entry ?*  ". 23n" fsharp-mode-syntax-table)
  ; ) is last character of comment end
  (modify-syntax-entry ?\) ")(4n" fsharp-mode-syntax-table)

  ; // is the beginning of a comment "b"
  (modify-syntax-entry ?/ ". 12b" fsharp-mode-syntax-table)
  ; // \n is the end of a comment "b"
  (modify-syntax-entry ?\n "> b" fsharp-mode-syntax-table)

  ; quote and underscore are part of symbols
  ; so are # and ! as they can form part of types/preprocessor
  ; directives and also keywords
  (modify-syntax-entry ?' "_" fsharp-mode-syntax-table)
  (modify-syntax-entry ?_ "_" fsharp-mode-syntax-table)
  (modify-syntax-entry ?# "_" fsharp-mode-syntax-table)
  (modify-syntax-entry ?! "_" fsharp-mode-syntax-table)

  ; ISO-latin accented letters and EUC kanjis are part of words
  (let ((i 160))
    (while (< i 256)
      (modify-syntax-entry i "w" fsharp-mode-syntax-table)
      (setq i (1+ i)))))

;; Other internal variables

(defvar fsharp-last-noncomment-pos nil
  "Caches last buffer position determined not inside a fsharp comment.")
(make-variable-buffer-local 'fsharp-last-noncomment-pos)

;; last-noncomment-pos can be a simple position, because we nil it
;; anyway whenever buffer changes upstream. last-comment-start and -end
;; have to be markers, because we preserve them when the changes' end
;; doesn't overlap with the comment's start.

(defvar fsharp-last-comment-start nil
  "A marker caching last determined fsharp comment start.")
(make-variable-buffer-local 'fsharp-last-comment-start)

(defvar fsharp-last-comment-end nil
  "A marker caching last determined fsharp comment end.")
(make-variable-buffer-local 'fsharp-last-comment-end)

(defvar fsharp-mode-hook nil
  "Hook for fsharp-mode")

;;;###autoload
(define-derived-mode fsharp-mode prog-mode "fsharp"
  :syntax-table fsharp-mode-syntax-table
  "Major mode for editing fsharp code.

\\{fsharp-mode-map}"

  (require 'fsharp-mode-indent)
  (require 'fsharp-mode-font)
  (require 'fsharp-doc)
  (require 'fsharp-mode-completion)

  ;(kill-all-local-variables)
  (use-local-map fsharp-mode-map)
  ;(set-syntax-table fsharp-mode-syntax-table)

  (mapc 'make-local-variable
        '(paragraph-start
          require-final-newline
          paragraph-separate
          paragraph-ignore-fill-prefix
          comment-start
          comment-end
          comment-column
          comment-start-skip
          parse-sexp-ignore-comments
          indent-line-function
          add-log-current-defun-function
          underline-minimum-offset
          compile-command
          syntax-propertize-function

          ac-sources
          ac-auto-start
          ac-use-comphist
          ac-auto-show-menu
          popup-tip-max-width
	  fsharp-ac-last-parsed-ticks
	  fsharp-ac-errors))

  (setq major-mode               'fsharp-mode
        mode-name                "fsharp"
        local-abbrev-table       fsharp-mode-abbrev-table
        paragraph-start          (concat "^$\\|" page-delimiter)
        paragraph-separate       paragraph-start
        require-final-newline    t
        indent-tabs-mode         nil
        comment-start            "//"
        comment-end              ""
        comment-column           40
        comment-start-skip       "///* *"
        comment-indent-function  'fsharp-comment-indent-function
        indent-region-function   'fsharp-indent-region
        indent-line-function     'fsharp-indent-line
        underline-minimum-offset  2

        paragraph-ignore-fill-prefix   t
        add-log-current-defun-function 'fsharp-current-defun
        fsharp-last-noncomment-pos     nil
        fsharp-last-comment-start      (make-marker)
        fsharp-last-comment-end        (make-marker)

        )

  ; Syntax highlighting
  (setq font-lock-defaults '(fsharp-font-lock-keywords))
  (setq syntax-propertize-function 'fsharp--syntax-propertize-function)
  ;; Error navigation
  (setq next-error-function 'fsharp-ac/next-error)
  (add-hook 'next-error-hook 'fsharp-ac/show-error-at-point nil t)
  (add-hook 'post-command-hook 'fsharp-ac/show-error-at-point nil t)

  ;; make a local copy of the menubar, so our modes don't
  ;; change the global menubar
  (when (and running-xemacs
             (featurep 'menubar)
             current-menubar)
    (set-buffer-menubar current-menubar)
    (add-submenu nil fsharp-mode-xemacs-menu))

  (setq compile-command (fsharp-mode-choose-compile-command
                         (buffer-file-name)))

  (fsharp-mode--load-with-binding (buffer-file-name))
  (turn-on-fsharp-doc-mode)
  (run-hooks 'fsharp-mode-hook))

(defun fsharp-mode--load-with-binding (file)
  "Attempt to load FILE using the F# compiler binding.
If FILE is part of an F# project, load the project.
Otherwise, treat as a stand-alone file."
  (when fsharp-ac-intellisense-enabled
    (or (when (not (fsharp-ac--process-live-p))
          (fsharp-ac/load-project (fsharp-mode/find-fsproj file)))
        (fsharp-ac/load-file file))
    (auto-complete-mode 1)
    (setq ac-auto-start nil
          ac-use-comphist nil)
    (when (and (display-graphic-p)
               (featurep 'pos-tip))
      (setq popup-tip-max-width 240))))

(defun fsharp-mode-choose-compile-command (file)
  "Format an appropriate compilation command, depending on several factors:
1. The presence of a makefile
2. The presence of a .sln or .fsproj
3. The file's type.
"
  (let* ((fname    (file-name-nondirectory file))
         (ext      (file-name-extension file))
         (proj     (fsharp-mode/find-sln-or-fsproj file))
         (makefile (or (file-exists-p "Makefile") (file-exists-p "makefile"))))
    (cond
     (makefile          compile-command)
     (proj              (concat fsharp-build-command " /nologo " proj))
     ((equal ext "fs")  (concat fsharp-compile-command " --nologo " file))
     ((equal ext "fsl") (concat "fslex "  file))
     ((equal ext "fsy") (concat "fsyacc " file))
     (t                 compile-command))))

(defun fsharp-find-alternate-file ()
  (interactive)
  (let ((name (buffer-file-name)))
    (if (string-match "^\\(.*\\)\\.\\(fs\\|fsi\\)$" name)
        (find-file
         (concat
          (fsharp-match-string 1 name)
          (if (string= "fs" (fsharp-match-string 2 name)) ".fsi" ".fs"))))))

;;; Subshell support

(defun fsharp-eval-region (start end)
  "Send the current region to the inferior fsharp process."
  (interactive"r")
  (require 'inf-fsharp-mode)
  (inferior-fsharp-eval-region start end))

(defun fsharp-load-buffer-file ()
  "Load the filename corresponding to the present buffer in F# with #load"
  (interactive)
  (require 'inf-fsharp-mode)
  (let* ((name buffer-file-name)
         (command (concat "#load \"" name "\"")))
    (when (buffer-modified-p)
      (when (y-or-n-p (concat "Do you want to save \"" name "\" before
loading it? "))
        (save-buffer)))
    (save-excursion (fsharp-run-process-if-needed))
    (save-excursion (fsharp-simple-send inferior-fsharp-buffer-name command))))

(defun fsharp-show-subshell ()
  (interactive)
  (require 'inf-fsharp-mode)
  (inferior-fsharp-show-subshell))

(defconst fsharp-error-regexp-fs
  "^\\([^(\n]+\\)(\\([0-9]+\\),\\([0-9]+\\)):"
  "Regular expression matching the error messages produced by fsc.")

(if (boundp 'compilation-error-regexp-alist)
    (or (memq 'fsharp
              compilation-error-regexp-alist)
        (progn
          (add-to-list 'compilation-error-regexp-alist 'fsharp)
          (add-to-list 'compilation-error-regexp-alist-alist
                       `(fsharp ,fsharp-error-regexp-fs 1 2 3)))))

;; Usual match-string doesn't work properly with font-lock-mode
;; on some emacs.

(defun fsharp-match-string (num &optional string)

  "Return string of text matched by last search, without properties.

NUM specifies which parenthesized expression in the last regexp.
Value is nil if NUMth pair didn't match, or there were less than NUM
pairs.  Zero means the entire text matched by the whole regexp or
whole string."

  (let* ((data (match-data))
         (begin (nth (* 2 num) data))
         (end (nth (1+ (* 2 num)) data)))
    (if string (substring string begin end)
      (buffer-substring-no-properties begin end))))

(defun fsharp-run-executable-file ()
  (interactive)
  (let ((name (buffer-file-name)))
    (if (string-match "^\\(.*\\)\\.\\(fs\\|fsi\\)$" name)
        (shell-command (concat (match-string 1 name) ".exe")))))

;;; Project

(defun fsharp-mode/find-sln-or-fsproj (dir-or-file)
  "Search for a solution or F# project file in any enclosing
folders relative to DIR-OR-FILE."
  (or (fsharp-mode/find-sln dir-or-file)
      (fsharp-mode/find-fsproj dir-or-file)))

(defun fsharp-mode/find-sln (dir-or-file)
  (fsharp-mode-search-upwards (rx (0+ nonl) ".sln" eol)
     (file-name-directory dir-or-file)))

(defun fsharp-mode/find-fsproj (dir-or-file)
  (fsharp-mode-search-upwards (rx (0+ nonl) ".fsproj" eol)
     (file-name-directory dir-or-file)))

(defun fsharp-mode-search-upwards (regex dir)
  (when dir
    (or (car-safe (directory-files dir 'full regex))
        (fsharp-mode-search-upwards regex (fsharp-mode-parent-dir dir)))))

(defun fsharp-mode-parent-dir (dir)
  (let ((p (file-name-directory (directory-file-name dir))))
    (unless (equal p dir)
      p)))

(provide 'fsharp-mode)

;;; fsharp-mode.el ends here
