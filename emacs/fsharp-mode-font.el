;;; fsharp-mode-font.el --- Syntax highlighting for F#

;; Copyright (C) 1997 INRIA

;; Author: 1993-1997 Xavier Leroy, Jacques Garrigue and Ian T Zimmerman
;;         2010-2011 Laurent Le Brun <laurent@le-brun.eu>
;; Maintainer: Robin Neatherway <robin.neatherway@gmail.com>
;; Keywords: languages

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

;; useful colors

(cond
 ((x-display-color-p)
  (require 'font-lock)
  (cond
   ((not (boundp 'font-lock-type-face))
    ;; make the necessary faces
    (make-face 'Firebrick)
    (set-face-foreground 'Firebrick "firebrick")
    (make-face 'RosyBrown)
    (set-face-foreground 'RosyBrown "RosyBrown")
    (make-face 'Purple)
    (set-face-foreground 'Purple "Purple")
    (make-face 'MidnightBlue)
    (set-face-foreground 'MidnightBlue "MidnightBlue")
    (make-face 'DarkGoldenRod)
    (set-face-foreground 'DarkGoldenRod "DarkGoldenRod")
    (make-face 'DarkOliveGreen)
    (set-face-foreground 'DarkOliveGreen "DarkOliveGreen4")
    (make-face 'CadetBlue)
    (set-face-foreground 'CadetBlue "CadetBlue")
    ; assign them as standard faces
    (setq font-lock-comment-face 'Firebrick)
    (setq font-lock-string-face 'RosyBrown)
    (setq font-lock-keyword-face 'Purple)
    (setq font-lock-function-name-face 'MidnightBlue)
    (setq font-lock-variable-name-face 'DarkGoldenRod)
    (setq font-lock-type-face 'DarkOliveGreen)
    (setq font-lock-constant-face 'CadetBlue)))
  ; extra faces for documention
  (make-face 'Stop)
  (set-face-foreground 'Stop "White")
  (set-face-background 'Stop "Red")
  (make-face 'Doc)
  (set-face-foreground 'Doc "Red")
  (setq font-lock-stop-face 'Stop)
  (setq font-lock-doccomment-face 'Doc)
))


(defconst fsharp-font-lock-keywords
  (list
;stop special comments
   '("\\(^\\|[^\"]\\)\\((\\*\\*/\\*\\*)\\)"
     2 font-lock-stop-face)
;doccomments
   '("\\(^\\|[^\"]\\)\\((\\*\\*[^*]*\\([^)*][^*]*\\*+\\)*)\\)"
     2 font-lock-doccomment-face)
;comments
   '("\\(^\\|[^\"]\\)\\((\\*[^*]*\\*+\\([^)*][^*]*\\*+\\)*)\\)"
     2 font-lock-comment-face)

;;  '("(\\*IF-OCAML\\([^)*][^*]*\\*+\\)+ENDIF-OCAML\\*)"
;;    2 font-lock-comment-face)

;;   '("\\(^\\|[^\"]\\)\\((\\*[^F]\\([^)*][^*]*\\*+\\)+)\\)"
;;     . font-lock-comment-face)
;  '("(\\*.*\\*)\\|(\\*.*\n.*\\*)"
;    . font-lock-comment-face)


;character literals
   (cons (concat "'\\(\\\\\\([ntbr'\\]\\|"
                 "[0-9][0-9][0-9]\\)\\|.\\)'"
                 "\\|\"[^\"\\]*\\(\\\\\\(.\\|\n\\)[^\"\\]*\\)*\"")
         'font-lock-string-face)

  '("//.*" . font-lock-comment-face)

;modules and constructors
   ;; '("`?\\<[A-Z][A-Za-z0-9_']*\\>" . font-lock-function-name-face)
;definition

  ;; functions
  '("\\<\\(?:let\\|and\\)\s+\\(?:\\(?:inline\\|rec\\)\s+\\)?\\([A-Za-z0-9_']+\\)\\(?:\s+[A-Za-z_]\\|\s*(\\)"
    1 font-lock-function-name-face)

  ;; pattern functions
  '("\\<\\(?:let\\|and\\)\s+\\(?:\\(?:inline\\|rec\\)\s+\\)?\\([A-Za-z0-9_']+\\)\s*=\s*function"
    1 font-lock-function-name-face)

  ;; active patterns
  '("\\<\\(?:let\\|and\\)\s+\\(?:\\(?:inline\\|rec\\)\s+\\)?(\\(|[A-Za-z0-9_'|]+|\\))\s+[A-Za-z_(]"
    1 font-lock-function-name-face)

  ;; member functions
  '("\\<\\(?:override\\|member\\|abstract\\)\s+\\(?:\\(?:inline\\|rec\\)\s+\\)?\\(?:[A-Za-z0-9_']+\\.\\)?\\([A-Za-z0-9_']+\\)"
    1 font-lock-function-name-face)

  ;; operator overload (!, %, &, *, +, -, ., /, <, =, >, ?, @, ^, |, and ~)
  '("\\<\\(?:override\\|member\\|abstract\\)\s+\\(?:\\(?:inline\\|rec\\)\s+\\)?\\(([!%&*+-./<=>?@^|~]+)\\)"
    1 font-lock-function-name-face)

  ;; constructor
  '("^\s*\\<\\(new\\) *(.*)[^=]*=" 1 font-lock-function-name-face)

  ;; open namespace
  '("\\<open\s\\([A-Za-z0-9_.]+\\)" 1 font-lock-type-face)

  ;; module/namespace
  '("\\<\\(?:module\\|namespace\\)\s\\([A-Za-z0-9_.]+\\)" 1 font-lock-type-face)

  ;; type defines
  '("^\s*\\<\\(?:type\\|and\\)\s+\\(?:private\\|internal\\|public\\)*\\([A-Za-z0-9_'.]+\\)" 1 font-lock-type-face)

  ;; attributes
  '("\\[<[A-Za-z0-9_]+>\\]" . font-lock-preprocessor-face)

   (cons (concat "\\(\\<"
                 (mapconcat 'identity
                            '(
                              ;; F# keywords
                              "abstract" "and" "as" "assert" "base" "begin"
                              "class" "default" "delegate" "do" "done" "downcast"
                              "downto" "elif" "else" "end" "exception" "extern"
                              "false" "finally" "for" "fun" "function" "global"
                              "if" "in" "inherit" "inline" "interface" "internal"
                              "lazy" "let" "match" "member" "module" "mutable"
                              "namespace" "new" "null" "of" "open" "or" "override"
                              "private" "public" "rec" "return" "sig" "static"
                              "struct" "then" "to" "true" "try" "type" "upcast"
                              "use" "val" "void" "when" "while" "with" "yield"

                              ;; F# reserved words for future use
                              "atomic" "break" "checked" "component" "const"
                              "constraint" "constructor" "continue" "eager"
                              "fixed" "fori" "functor" "include" "measure"
                              "method" "mixin" "object" "parallel" "params"
                              "process" "protected" "pure" "recursive" "sealed"
                              "tailcall" "trait" "virtual" "volatile"
                              )
                            "\\>\\|\\<")
                 "\\>\\)")
         'font-lock-keyword-face)

;blocking
;;    '("\\<\\(begin\\|end\\|module\\|namespace\\|object\\|sig\\|struct\\)\\>"
;;      . font-lock-keyword-face)
;control
   (cons (concat
          "\\<\\(asr\\|false\\|land\\|lor\\|lsl\\|lsr\\|lxor"
          "\\|mod\\|new\\|null\\|object\\|or\\|sig\\|true\\)\\>"
          "\\|\|\\|->\\|&\\|#")
         'font-lock-constant-face)
;labels (and open)
   '("\\<\\(assert\\|open\\|include\\|module\\|namespace\\|extern\\|void\\)\\>\\|[~?][ (]*[a-z][a-zA-Z0-9_']*"
     . font-lock-variable-name-face)))

(defconst inferior-fsharp-font-lock-keywords
  (append
   (list
;inferior
    '("^[#-]" . font-lock-comment-face)
   '("^>" . font-lock-variable-name-face))
   fsharp-font-lock-keywords))

;; font-lock commands are similar for fsharp-mode and inferior-fsharp-mode
(add-hook 'fsharp-mode-hook
      '(lambda ()
         (cond
          ((fboundp 'global-font-lock-mode)
           (make-local-variable 'font-lock-defaults)
           (setq font-lock-defaults
                 '(fsharp-font-lock-keywords nil nil ((?' . "w") (?_ . "w")))))
          (t
           (setq font-lock-keywords fsharp-font-lock-keywords)))
         (make-local-variable 'font-lock-keywords-only)
         (setq font-lock-keywords-only t)
         (font-lock-mode 1)))

(defun inferior-fsharp-mode-font-hook ()
  (cond
   ((fboundp 'global-font-lock-mode)
    (make-local-variable 'font-lock-defaults)
    (setq font-lock-defaults
          '(inferior-fsharp-font-lock-keywords
            nil nil ((?' . "w") (?_ . "w")))))
   (t
    (setq font-lock-keywords inferior-fsharp-font-lock-keywords)))
  (make-local-variable 'font-lock-keywords-only)
  (setq font-lock-keywords-only t)
  (font-lock-mode 1))

(add-hook 'inferior-fsharp-mode-hooks 'inferior-fsharp-mode-font-hook)

(provide 'fsharp-mode-font)

;;; fsharp-mode-font.el ends here
