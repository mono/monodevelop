;(***********************************************************************)
;(*                                                                     *)
;(*                           Objective Caml                            *)
;(*                                                                     *)
;(*                Jacques Garrigue and Ian T Zimmerman                 *)
;(*                                                                     *)
;(*  Copyright 1997 Institut National de Recherche en Informatique et   *)
;(*  en Automatique.  All rights reserved.  This file is distributed    *)
;(*  under the terms of the GNU General Public License.                 *)
;(*                                                                     *)
;(***********************************************************************)

;(* $Id: fsharp-font.el,v 1.19 2004/08/20 17:04:35 doligez Exp $ *)

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
    (setq font-lock-reference-face 'CadetBlue)))
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
   '("`?\\<[A-Z][A-Za-z0-9_']*\\>" . font-lock-function-name-face)
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
         'font-lock-type-face)

;blocking
;;    '("\\<\\(begin\\|end\\|module\\|namespace\\|object\\|sig\\|struct\\)\\>"
;;      . font-lock-keyword-face)
;control
   (cons (concat
          "\\<\\(asr\\|false\\|land\\|lor\\|lsl\\|lsr\\|lxor"
          "\\|mod\\|new\\|null\\|object\\|or\\|sig\\|true\\)\\>"
          "\\|\|\\|->\\|&\\|#")
         'font-lock-reference-face)
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

(provide 'fsharp-font)
