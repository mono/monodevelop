;;; fsharp-mode.el --- Support for the F# programming language

;; Copyright (C) 1997 INRIA

;; Author: 1993-1997 Xavier Leroy, Jacques Garrigue and Ian T Zimmerman
;;         2010-2011 Laurent Le Brun <laurent@le-brun.eu>
;; Maintainer: Robin Neatherway <robin.neatherway@gmail.com>
;; Keywords: languages
;; Version: 0.7

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

(require 'namespaces)

(namespace fsharp-mode
  :export
  [find-sln-or-fsproj
   find-sln
   find-fsproj])

(defvar fsharp-mode-version 0.7
  "Version of this fsharp-mode")

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

(if fsharp-mode-map
    ()
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

  (define-key fsharp-mode-map "\C-cl" 'fsharp-shift-region-left)
  (define-key fsharp-mode-map "\C-cr" 'fsharp-shift-region-right)

  (define-key fsharp-mode-map "\C-m"      'fsharp-newline-and-indent)
  (define-key fsharp-mode-map "\C-c:"     'fsharp-guess-indent-offset)
  (define-key fsharp-mode-map [delete]    'fsharp-electric-delete)
  (define-key fsharp-mode-map [backspace] 'fsharp-electric-backspace)
  (define-key fsharp-mode-map (kbd ".") 'ac-fsharp-electric-dot)

  (define-key fsharp-mode-map (kbd "C-c <up>") 'fsharp-goto-block-up)

  (define-key fsharp-mode-map (kbd "C-c C-p") 'ac-fsharp-load-project)
  (define-key fsharp-mode-map (kbd "C-c C-t") 'ac-fsharp-tooltip-at-point)
  (define-key fsharp-mode-map (kbd "C-c C-d") 'ac-fsharp-gotodefn-at-point)
  (define-key fsharp-mode-map (kbd "C-c C-q") 'ac-fsharp-quit-completion-process)

  (if running-xemacs nil ; if not running xemacs
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
      (define-key map [eval-phrase] '("Eval phrase" . fsharp-eval-phrase))
      )))

;;;###autoload
(add-to-list 'auto-mode-alist '("\\.fs[iylx]?$" . fsharp-mode))

(defvar fsharp-mode-syntax-table nil
  "Syntax table in use in fsharp mode buffers.")
(if fsharp-mode-syntax-table
    ()
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
  (modify-syntax-entry ?\/ ". 12b" fsharp-mode-syntax-table)
  ; // \nis the beginning of a comment "b"
  (modify-syntax-entry ?\n "> b" fsharp-mode-syntax-table)

  ; backquote was a string-like delimiter (for character literals)
  ; (modify-syntax-entry ?` "\"" fsharp-mode-syntax-table)
  ; quote and underscore are part of words
  (modify-syntax-entry ?' "w" fsharp-mode-syntax-table)
  (modify-syntax-entry ?_ "w" fsharp-mode-syntax-table)
  ; ISO-latin accented letters and EUC kanjis are part of words
  (let ((i 160))
    (while (< i 256)
      (modify-syntax-entry i "w" fsharp-mode-syntax-table)
      (setq i (1+ i)))))

;; Other internal variables

(defvar fsharp-last-noncomment-pos nil
  "Caches last buffer position determined not inside a fsharp comment.")
(make-variable-buffer-local 'fsharp-last-noncomment-pos)

;;last-noncomment-pos can be a simple position, because we nil it
;;anyway whenever buffer changes upstream. last-comment-start and -end
;;have to be markers, because we preserve them when the changes' end
;;doesn't overlap with the comment's start.

(defvar fsharp-last-comment-start nil
  "A marker caching last determined fsharp comment start.")
(make-variable-buffer-local 'fsharp-last-comment-start)

(defvar fsharp-last-comment-end nil
  "A marker caching last determined fsharp comment end.")
(make-variable-buffer-local 'fsharp-last-comment-end)

(make-variable-buffer-local 'before-change-function)

(defvar fsharp-mode-hook nil
  "Hook for fsharp-mode")

;;;###autoload
(defun fsharp-mode ()
  "Major mode for editing fsharp code.

\\{fsharp-mode-map}"
  (interactive)

  (require 'fsharp-mode-indent)
  (require 'fsharp-mode-font)
  (require 'fsharp-doc)
  (require 'fsharp-mode-completion)
  (kill-all-local-variables)
  (use-local-map fsharp-mode-map)
  (set-syntax-table fsharp-mode-syntax-table)
  (make-local-variable 'paragraph-start)
  (make-local-variable 'require-final-newline)
  (make-local-variable 'paragraph-separate)
  (make-local-variable 'paragraph-ignore-fill-prefix)
  (make-local-variable 'comment-start)
  (make-local-variable 'comment-end)
  (make-local-variable 'comment-column)
  (make-local-variable 'comment-start-skip)
  (make-local-variable 'parse-sexp-ignore-comments)
  (make-local-variable 'indent-line-function)
  (make-local-variable 'add-log-current-defun-function)
  (make-local-variable 'underline-minimum-offset)
  (make-local-variable 'compile-command)

  (add-hook 'completion-at-point-functions #'ac-fsharp-completion-at-point)

  (setq major-mode              'fsharp-mode
        mode-name               "fsharp"
        local-abbrev-table      fsharp-mode-abbrev-table
        paragraph-start         (concat "^$\\|" page-delimiter)
        paragraph-separate      paragraph-start
        paragraph-ignore-fill-prefix t
        require-final-newline   t
        indent-tabs-mode        nil
        comment-start           "//"
        comment-end             ""
        comment-column          40
        comment-start-skip      "///* *"
        comment-indent-function 'fsharp-comment-indent-function
        indent-region-function  'fsharp-indent-region
        indent-line-function    'fsharp-indent-line
        underline-minimum-offset 2

        add-log-current-defun-function 'fsharp-current-defun
        before-change-function 'fsharp-before-change-function
        fsharp-last-noncomment-pos nil
        fsharp-last-comment-start (make-marker)
        fsharp-last-comment-end (make-marker))

  (if running-xemacs                    ; from Xemacs lisp mode
      (if (and (featurep 'menubar)
               current-menubar)
          (progn
            ;; make a local copy of the menubar, so our modes don't
            ;; change the global menubar
            (set-buffer-menubar current-menubar)
            (add-submenu nil fsharp-mode-xemacs-menu))))

  (in-ns fsharp-mode
    (setq compile-command (_ choose-compile-command (buffer-file-name)))
    (unless ac-fsharp-completion-process
      (_ try-load-project (buffer-file-name)))

    (turn-on-fsharp-doc-mode)
    (run-hooks 'fsharp-mode-hook)

    (if fsharp-smart-indentation
        (let ((offset fsharp-indent-offset))
          ;; It's okay if this fails to guess a good value
          (if (and (fsharp-safe (fsharp-guess-indent-offset))
                   (<= fsharp-indent-offset 8)
                   (>= fsharp-indent-offset 2))
              (setq offset fsharp-indent-offset))
          (setq fsharp-indent-offset offset)
          ;; Only turn indent-tabs-mode off if tab-width !=
          ;; fsharp-indent-offset.  Never turn it on, because the user must
          ;; have explicitly turned it off.
          (if (/= tab-width fsharp-indent-offset)
              (setq indent-tabs-mode nil))))))

(defn try-load-project (file)
  (when file
    (let ((proj (_ find-fsproj file)))
      (when proj
        (ac-fsharp-load-project proj)))))

(defn choose-compile-command (file)
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
     ;; Try to load
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

;;; subshell support

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

(defun fsharp-mode-version ()
  "Echo the current version of `fsharp-mode' in the minibuffer."
  (interactive)
  (message "Using `fsharp-mode' version %s" fsharp-mode-version)
  (fsharp-keep-region-active))

;;; Project

(defn find-sln-or-fsproj (dir-or-file)
  "Search for a solution or F# project file in any enclosing
folders relative to DIR-OR-FILE."
  (or (_ find-sln dir-or-file)
      (_ find-fsproj dir-or-file)))

(defn find-sln (dir-or-file)
  (_ search-upwards (rx (0+ nonl) ".sln" eol)
     (file-name-directory dir-or-file)))

(defn find-fsproj (dir-or-file)
  (_ search-upwards (rx (0+ nonl) ".fsproj" eol)
     (file-name-directory dir-or-file)))

(defn search-upwards (regex dir)
  (when dir
    (or (car-safe (directory-files dir 'full regex))
        (_ search-upwards regex (_ parent-dir dir)))))

(defn parent-dir (dir)
  (let ((p (file-name-directory (directory-file-name dir))))
    (unless (equal p dir)
      p)))


;;; fsharp-mode.el ends here
