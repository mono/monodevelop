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

(defconst fsharp-access-control-regexp
  "\\(?:private\\s-+\\|internal\\s-+\\|public\\s-+\\)*")
(defconst fsharp-function-def-regexp
  "\\<\\(?:let\\|and\\|with\\)\\s-+\\(?:\\(?:inline\\|rec\\)\\s-+\\)?\\([A-Za-z0-9_']+\\)\\(?:\\s-+[A-Za-z_]\\|\\s-*(\\)")
(defconst fsharp-pattern-function-regexp
  "\\<\\(?:let\\|and\\)\\s-+\\(?:\\(?:inline\\|rec\\)\\s-+\\)?\\([A-Za-z0-9_']+\\)\\s-*=\\s-*function")
(defconst fsharp-active-pattern-regexp
  "\\<\\(?:let\\|and\\)\\s-+\\(?:\\(?:inline\\|rec\\)\\s-+\\)?(\\(|[A-Za-z0-9_'|]+|\\))\\(?:\\s-+[A-Za-z_]\\|\\s-*(\\)")
(defconst fsharp-member-function-regexp
  "\\<\\(?:override\\|member\\|abstract\\)\\s-+\\(?:\\(?:inline\\|rec\\)\\s-+\\)?\\(?:[A-Za-z0-9_']+\\.\\)?\\([A-Za-z0-9_']+\\)")
(defconst fsharp-overload-operator-regexp
  "\\<\\(?:override\\|member\\|abstract\\)\\s-+\\(?:\\(?:inline\\|rec\\)\\s-+\\)?\\(([!%&*+-./<=>?@^|~]+)\\)")
(defconst fsharp-constructor-regexp "^\\s-*\\<\\(new\\) *(.*)[^=]*=")
(defconst fsharp-type-def-regexp 
  (format "^\\s-*\\<\\(?:type\\|inherit\\)\\s-+%s\\([A-Za-z0-9_'.]+\\)" 
		  fsharp-access-control-regexp))
(defconst fsharp-var-or-arg-regexp "\\<\\([A-Za-z_][A-Za-z0-9_']*\\)\\>")
(defconst fsharp-explicit-field-regexp
  (format "^\\s-*\\(?:val\\|abstract\\)\\s-*\\(?:mutable\\s-+\\)?%s\\([A-Za-z_][A-Za-z0-9_']*\\)\\s-*:\\s-*\\([A-Za-z_][A-Za-z0-9_'<> \t]*\\)" fsharp-access-control-regexp))

(defvar fsharp-imenu-generic-expression
  `((nil ,(concat "^\\s-*" fsharp-function-def-regexp) 1)
    (nil ,(concat "^\\s-*" fsharp-pattern-function-regexp) 1)
    (nil ,(concat "^\\s-*" fsharp-active-pattern-regexp) 1)
    (nil ,(concat "^\\s-*" fsharp-member-function-regexp) 1)
    (nil ,(concat "^\\s-*" fsharp-overload-operator-regexp) 1)
    (nil ,fsharp-constructor-regexp 1)
    (nil ,fsharp-type-def-regexp 1)
    ))

(defvar fsharp-var-pre-form
  (lambda ()
    (save-excursion
      (re-search-forward "\\(:\\s-*\\w[^)]*\\)?=")
      (match-beginning 0))))

(defvar fsharp-fun-pre-form
  (lambda ()
    (save-excursion      
      (search-forward "->"))))

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

  ;; attributes
  '("\\[<[A-Za-z0-9_]+>\\]" . font-lock-preprocessor-face)
  ;; type defines
  `(,fsharp-type-def-regexp 1 font-lock-type-face)
  `(,fsharp-function-def-regexp 1 font-lock-function-name-face)
  `(,fsharp-pattern-function-regexp 1 font-lock-function-name-face)
  `(,fsharp-active-pattern-regexp 1 font-lock-function-name-face)
  `(,fsharp-member-function-regexp 1 font-lock-function-name-face)
  `(,fsharp-overload-operator-regexp 1 font-lock-function-name-face)
  ;; `(,fsharp-constructor-regexp 1 font-lock-function-name-face)
  `("[^:]:\\s-*\\(\\<[A-Za-z_'][^,)=<-]*\\)\\s-*\\(<[^>]*>\\)?"
    (1 font-lock-type-face)             ; type annotations
    ;; HACK: font-lock-negation-char-face is usually the same as
    ;; 'default'. use this to prevent generic type arguments from
    ;; being rendered in variable face
    (2 font-lock-negation-char-face nil t))
  `(,(format "^\\s-*\\<\\(let\\|use\\|override\\|member\\|and\\|\\(?:%snew\\)\\)\\>"
			 fsharp-access-control-regexp)
    (0 font-lock-keyword-face) ; let binding and function arguments
    (,fsharp-var-or-arg-regexp
     (,fsharp-var-pre-form) nil
     (1 font-lock-variable-name-face nil t)))
  `("\\<fun\\>"
    (0 font-lock-keyword-face) ; lambda function arguments
    (,fsharp-var-or-arg-regexp
     (,fsharp-fun-pre-form) nil
     (1 font-lock-variable-name-face nil t)))
  `(,fsharp-type-def-regexp
    (0 font-lock-keyword-face) ; implicit constructor arguments
    (,fsharp-var-or-arg-regexp
     (,fsharp-var-pre-form) nil
     (1 font-lock-variable-name-face nil t)))
  `(,fsharp-explicit-field-regexp
	(1 font-lock-variable-name-face)
	(2 font-lock-type-face))

  ;; open namespace
  '("\\<open\s\\([A-Za-z0-9_.]+\\)" 1 font-lock-type-face)

  ;; module/namespace
  '("\\<\\(?:module\\|namespace\\)\s\\([A-Za-z0-9_.]+\\)" 1 font-lock-type-face)

;labels (and open)
   '("\\<\\(assert\\|open\\|include\\|module\\|namespace\\|extern\\|void\\)\\>\\|[~?][ (]*[a-z][a-zA-Z0-9_']*"
     . font-lock-variable-name-face)
   ;; (cons (concat
   ;;        "\\<\\(asr\\|false\\|land\\|lor\\|lsl\\|lsr\\|lxor"
   ;;        "\\|mod\\|new\\|null\\|object\\|or\\|sig\\|true\\)\\>"
   ;;        "\\|\|\\|->\\|&\\|#")
   ;;       'font-lock-constant-face)
   ))

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
         (font-lock-mode 1)
         (set (make-local-variable 'imenu-generic-expression)
              fsharp-imenu-generic-expression)
         ))

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
