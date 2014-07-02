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
   ;; Preprocessor directives
   (cons (regexp-opt
          '(;; Preprocessor directives
            "#if" "#else" "#endif"

            ;; FSI directives
            "#load" "#r" "#I" "#quit" "#time" "#help"

            ;; F# keywords
            "abstract" "and" "as" "assert" "base" "begin"
            "class" "default" "delegate" "do" "done"
            "downcast" "downto" "elif" "else" "end"
            "exception" "extern" "false" "finally" "for" "fun"
            "function" "global" "if" "in" "inherit" "inline"
            "interface" "internal" "lazy" "let" "let!"
            "match" "member" "module" "mutable" "namespace"
            "new" "not" "null" "of" "open" "or" "override"
            "private" "public" "rec" "return" "return!"
            "select" "static" "struct" "then" "to" "true"
            "try" "type" "upcast" "use" "use!"  "val" "void"
            "when" "while" "with" "yield" "yield!"

            ;; "Reserved because they are reserved in OCaml"
            "asr" "land" "lor" "lsl" "lsr" "lxor" "mod" "sig"

            ;; F# reserved words for future use
            "atomic" "break" "checked" "component" "const"
            "constraint" "constructor" "continue" "eager"
            "event" "external" "fixed" "functor" "include"
            "method" "mixin" "object" "parallel" "process"
            "protected" "pure" "sealed" "tailcall" "trait"
            "virtual" "volatile"

            ;; Workflows not yet handled by fsautocomplete
            ;; but async always present
            "async"
            ) 'symbols)
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
   '("\\<\\(assert\\|open\\|include\\|module\\|namespace\\|extern\\|void\\)\\>\\|[~][ (]*[a-z][a-zA-Z0-9_']*"
     . font-lock-variable-name-face)
   ;; (cons (concat
   ;;        "\\<\\(asr\\|false\\|land\\|lor\\|lsl\\|lsr\\|lxor"
   ;;        "\\|mod\\|new\\|null\\|object\\|or\\|sig\\|true\\)\\>"
   ;;        "\\|\|\\|->\\|&\\|#")
   ;;       'font-lock-constant-face)
   ))


(defun fsharp--syntax-propertize-function (start end)
;  (message "Called with (%d,%d)" start end)
  (goto-char start)
  (fsharp--syntax-string end)
  (funcall (syntax-propertize-rules
            ("\\(@\\)\"" (1 (prog1 "|" (fsharp--syntax-string end)))) ; verbatim string
            ("\\(\"\\)\"\"" (1 (prog1 "|" (fsharp--syntax-string end)))) ; triple-quoted string
            ("\\('\\)\\(?:[^\n\t\r\b\a\f\v\\\\]\\|\\\\[\"'ntrbafv\\\\]\\|\\\\u[0-9A-Fa-f]\\{4\\}\\|\\\\[0-9]\\{3\\}\\)\\('\\)"
             (1 "|") (2 "|")) ; character literal
            ("\\((\\)/" (1 "()"))
            ("\\(/\\)\\*" (1 ".")))
           start end))

(defun fsharp--syntax-string (end)
  (let* ((pst (syntax-ppss))
         (instr (nth 3 pst))
         (start (nth 8 pst)))
    (when (eq t instr) ; Then we are in a custom string
      ;(message "In custom string")
      (cond
       ((eq ?@ (char-after start)) ; Then we are in a verbatim string
        ;(message "verbatim")
        (while
            (when (re-search-forward "\"\"?" end 'move)
              (if (> (- (match-end 0) (match-beginning 0)) 1)
                  t ;; Skip this "" and keep looking further.
                (put-text-property (- (match-beginning 0) 1) (- (match-end 0) 1)
                                   'syntax-table (string-to-syntax "."))
                (put-text-property (match-beginning 0) (match-end 0)
                                   'syntax-table (string-to-syntax "|"))
                nil)))
        )
       
       (t ; Then we are in a triple-quoted string
        ;(message "triple-quoted")
        (when (re-search-forward "\"\"\"" end 'move)
          (put-text-property (- (match-beginning 0) 1) (match-beginning 0)
                             'syntax-table (string-to-syntax "."))
          (put-text-property (match-beginning 0) (match-end 0)
                             'syntax-table (string-to-syntax "|")))
          )))))


(defconst inferior-fsharp-font-lock-keywords
  (append
   (list
;inferior
    '("^[#-]" . font-lock-comment-face)
   '("^>" . font-lock-variable-name-face))
   fsharp-font-lock-keywords))

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
