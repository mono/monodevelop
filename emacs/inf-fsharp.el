;(***********************************************************************)
;(*                                                                     *)
;(*                           Objective fsharp                            *)
;(*                                                                     *)
;(*                   Xavier Leroy and Jacques Garrigue                 *)
;(*                                                                     *)
;(*  Copyright 1997 Institut National de Recherche en Informatique et   *)
;(*  en Automatique.  All rights reserved.  This file is distributed    *)
;(*  under the terms of the GNU General Public License.                 *)
;(*                                                                     *)
;(***********************************************************************)

;(* $Id: inf-fsharp.el,v 1.11 2004/08/20 17:04:35 doligez Exp $ *)

;;; inf-fsharp.el --- run the fsharp toplevel in an Emacs buffer

;; Xavier Leroy, july 1993.

;; modified by Jacques Garrigue, july 1997.

;; modified by Laurent Le Brun for F#, 2010

(require 'comint)
(require 'fsharp)

;; User modifiable variables

;; Whether you want the output buffer to be diplayed when you send a phrase

(defvar fsharp-display-when-eval t
  "*If true, display the inferior fsharp buffer when evaluating expressions.")


;; End of User modifiable variables


(defvar inferior-fsharp-mode-map nil)
(if inferior-fsharp-mode-map nil
  (setq inferior-fsharp-mode-map
        (copy-keymap comint-mode-map)))

;; Augment fsharp mode, so you can process fsharp code in the source files.

(defvar inferior-fsharp-program "fsharp"
  "*Program name for invoking an inferior fsharp from Emacs.")

(defun inferior-fsharp-mode ()
  "Major mode for interacting with an inferior fsharp process.
Runs a fsharp toplevel as a subprocess of Emacs, with I/O through an
Emacs buffer. A history of input phrases is maintained. Phrases can
be sent from another buffer in fsharp mode.

\\{inferior-fsharp-mode-map}"
  (interactive)
  (comint-mode)
  (setq comint-prompt-regexp "^# ?")
  (setq major-mode 'inferior-fsharp-mode)

  (setq mode-name "Inferior fsharp")
  (make-local-variable 'paragraph-start)
  (setq paragraph-start (concat "^$\\|" page-delimiter))
  (make-local-variable 'paragraph-separate)
  (setq paragraph-separate paragraph-start)
  (make-local-variable 'paragraph-ignore-fill-prefix)
  (setq paragraph-ignore-fill-prefix t)
  (make-local-variable 'require-final-newline)
  (setq require-final-newline t)
  (make-local-variable 'comment-start)
  (setq comment-start "(*")
  (make-local-variable 'comment-end)
  (setq comment-end "*)")
  (make-local-variable 'comment-column)
  (setq comment-column 40)
  (make-local-variable 'comment-start-skip)
  (setq comment-start-skip "(\\*+ *")
  (make-local-variable 'parse-sexp-ignore-comments)
  (setq parse-sexp-ignore-comments nil)
  (use-local-map inferior-fsharp-mode-map)
  (run-hooks 'inferior-fsharp-mode-hooks)

  (define-key inferior-fsharp-mode-map [M-return] 'fsharp-comint-send)

  ;; use compilation mode to parse errors, but RET and C-cC-c should still be from comint-mode
  (compilation-minor-mode)
  (make-local-variable 'minor-mode-map-alist)
  (setq minor-mode-map-alist (assq-delete-all 'compilation-minor-mode (copy-list minor-mode-map-alist)))
)


(defconst inferior-fsharp-buffer-subname "inferior-fsharp")
(defconst inferior-fsharp-buffer-name
  (concat "*" inferior-fsharp-buffer-subname "*"))

;; for compatibility with xemacs 

(defun fsharp-sit-for (second &optional mili redisplay)
   (if (and (boundp 'running-xemacs) running-xemacs)
       (sit-for (if mili (+ second (* mili 0.001)) second) redisplay)
     (sit-for second mili redisplay)))

;; To show result of evaluation at toplevel

(defvar inferior-fsharp-output nil)
(defun inferior-fsharp-signal-output (s)
  (if (string-match "[^ ]" s) (setq inferior-fsharp-output t)))

(defun inferior-fsharp-mode-output-hook ()
  (setq comint-output-filter-functions
        (list (function inferior-fsharp-signal-output))))
(add-hook 'inferior-fsharp-mode-hooks 'inferior-fsharp-mode-output-hook)

;; To launch fsharp whenever needed

(defun fsharp-run-process-if-needed (&optional cmd)
  (if (comint-check-proc inferior-fsharp-buffer-name) nil
    (if (not cmd)
        (if (comint-check-proc inferior-fsharp-buffer-name)
            (setq cmd inferior-fsharp-program)
          (setq cmd (read-from-minibuffer "fsharp toplevel to run: "
                                          inferior-fsharp-program))))
    (setq inferior-fsharp-program cmd)
    (let ((cmdlist (inferior-fsharp-args-to-list cmd))
          (process-connection-type nil))
      (set-buffer (apply (function make-comint)
                         inferior-fsharp-buffer-subname
                         (car cmdlist) nil (cdr cmdlist)))
      (inferior-fsharp-mode)
      (display-buffer inferior-fsharp-buffer-name)
      t)
    (setq fsharp-shell-active t)
    ))

;; patched to from original run-fsharp sharing code with
;;  fsharp-run-process-when-needed

(defun run-fsharp (&optional cmd)
  "Run an inferior fsharp process.
Input and output via buffer `*inferior-fsharp*'."
  (interactive
   (list (if (not (comint-check-proc inferior-fsharp-buffer-name))
             (read-from-minibuffer "fsharp toplevel to run: "
                                   inferior-fsharp-program))))
  (fsharp-run-process-if-needed cmd)
  (switch-to-buffer-other-window inferior-fsharp-buffer-name))

;; split the command line (e.g. "mono fsi" -> ("mono" "fsi"))
;; we double the \ before unquoting, so that the user doesn't have to
(defun inferior-fsharp-args-to-list (string)
  (split-string-and-unquote (replace-regexp-in-string "\\\\" "\\\\\\\\" string)))

(defun inferior-fsharp-show-subshell ()
  (interactive)
  (fsharp-run-process-if-needed)
  (display-buffer inferior-fsharp-buffer-name)
  ; Added by Didier to move the point of inferior-fsharp to end of buffer
  (let ((buf (current-buffer))
        (fsharp-buf  (get-buffer inferior-fsharp-buffer-name))
        (count 0))
    (while
        (and (< count 10)
             (not (equal (buffer-name (current-buffer))
                         inferior-fsharp-buffer-name)))
      (next-multiframe-window)
      (setq count (+ count 1)))
    (if  (equal (buffer-name (current-buffer))
                inferior-fsharp-buffer-name)
        (end-of-buffer))
    (while
        (> count 0)
      (previous-multiframe-window)
      (setq count (- count 1)))
    )
)

;; patched by Didier to move cursor after evaluation 

(defun inferior-fsharp-eval-region (start end)
  "Send the current region to the inferior fsharp process."
  (interactive "r")
  (save-excursion (fsharp-run-process-if-needed))
  (save-excursion
    ;; send location to fsi
    (let* (
          (name (buffer-name (current-buffer)))
          (line (number-to-string (line-number-at-pos start)))
          (loc (concat "# " line " \"" name "\"\n")))
      (comint-send-string inferior-fsharp-buffer-name loc))
    (goto-char end)
;    (fsharp-skip-comments-backward)
    (comint-send-region inferior-fsharp-buffer-name start (point))
    ;; normally, ";;" are part of the region
    (if (and (>= (point) 2)
             (prog2 (backward-char 2) (looking-at ";;")))
        (comint-send-string inferior-fsharp-buffer-name "\n")
      (comint-send-string inferior-fsharp-buffer-name "\n;;\n"))
    ;; the user may not want to see the output buffer
    (if fsharp-display-when-eval
        (display-buffer inferior-fsharp-buffer-name t))))

;; jump to errors produced by fsharp compiler

(defun inferior-fsharp-goto-error (start end)
  "Jump to the location of the last error as indicated by inferior toplevel."
  (interactive "r")
  (let ((loc (+ start
                (save-excursion
                  (set-buffer (get-buffer inferior-fsharp-buffer-name))
                  (re-search-backward
                   (concat ;; comint-prompt-regexp
                           "(\\([0-9]+\\),\\([0-9]+\\)): error"))
;;                           "[ \t]*Characters[ \t]+\\([0-9]+\\)-[0-9]+:$"))
                  (string-to-int (match-string 1))))))
    (goto-line (- loc 1))))
;;    (goto-char loc)))


;;; orgininal inf-fsharp.el ended here

;; as eval-phrase, but ignores errors.

(defun inferior-fsharp-just-eval-phrase (arg &optional min max)
  "Send the phrase containing the point to the fsharp process.
With prefix-arg send as many phrases as its numeric value,
ignoring possible errors during evaluation.

Optional arguments min max defines a region within which the phrase
should lies."
  (interactive "p")
  (let ((beg))
    (while (> arg 0)
      (setq arg (- arg 1))
      (setq beg  (fsharp-find-phrase min max))
      (fsharp-eval-region beg (point)))
    beg))

(defvar fsharp-previous-output nil
  "tells the beginning of output in the shell-output buffer, so that the
output can be retreived later, asynchronously.")

;; enriched version of eval-phrase, to repport errors.

(defun inferior-fsharp-eval-phrase (arg &optional min max)
  "Send the phrase containing the point to the fsharp process.
With prefix-arg send as many phrases as its numeric value, 
If an error occurs during evalutaion, stop at this phrase and
repport the error. 

Return nil if noerror and position of error if any.

If arg's numeric value is zero or negative, evaluate the current phrase
or as many as prefix arg, ignoring evaluation errors. 
This allows to jump other erroneous phrases. 

Optional arguments min max defines a region within which the phrase
should lies."
  (interactive "p")
  (if (save-excursion (fsharp-run-process-if-needed))
      (progn
        (setq inferior-fsharp-output nil)
        (fsharp-wait-output 10 1)))
  (if (< arg 1) (inferior-fsharp-just-eval-phrase (max 1 (- 0 arg)) min max)
    (let ((proc (get-buffer-process inferior-fsharp-buffer-name))
          (buf (current-buffer))
          previous-output orig beg end err)
      (save-window-excursion
        (while (and (> arg 0) (not err))
          (setq previous-output (marker-position (process-mark proc)))
          (setq fsharp-previous-output previous-output)
          (setq inferior-fsharp-output nil)
          (setq orig (inferior-fsharp-just-eval-phrase 1 min max))
          (fsharp-wait-output)
          (switch-to-buffer inferior-fsharp-buffer-name  nil)
          (goto-char previous-output)
          (cond ((re-search-forward
                  " *Characters \\([01-9][01-9]*\\)-\\([1-9][01-9]*\\):\n[^W]"
                  (point-max) t)
                 (setq beg (string-to-int (fsharp-match-string 1)))
                 (setq end (string-to-int (fsharp-match-string 2)))
                 (switch-to-buffer buf)
                 (goto-char orig)
                 (forward-byte end)
                 (setq end (point))
                 (goto-char orig)
                 (forward-byte beg)
                 (setq beg (point))
                 (setq err beg)
                 )
                ((looking-at
                  "Toplevel input:\n[>]\\([^\n]*\\)\n[>]\\(\\( *\\)^*\\)\n")
                 (let ((expr (fsharp-match-string 1))
                       (column (-   (match-end 3) (match-beginning 3)))
                       (width (-   (match-end 2) (match-end 3))))
                   (if (string-match  "^\\(.*\\)[<]EOF[>]$" expr)
                       (setq expr (substring expr (match-beginning 1) (match-end 1))))
                   (switch-to-buffer buf)
                   (re-search-backward
                    (concat "^" (regexp-quote expr) "$")
                    (- orig 10))
                   (goto-char (+ (match-beginning 0) column))
                   (setq end (+ (point) width)))
                 (setq err beg))
                ((looking-at
                  "Toplevel input:\n>[.]*\\([^.].*\n\\)\\([>].*\n\\)*[>]\\(.*[^.]\\)[.]*\n")
                 (let* ((e1 (fsharp-match-string 1))
                        (e2 (fsharp-match-string 3))
                        (expr
                         (concat
                          (regexp-quote e1) "\\(.*\n\\)*" (regexp-quote e2))))
                   (switch-to-buffer buf)
                   (re-search-backward expr orig 'move)
                   (setq end (match-end 0)))
                 (setq err beg))
                (t
                 (switch-to-buffer buf)))
          (setq arg (- arg 1))
          )
        (pop-to-buffer inferior-fsharp-buffer-name)
        (if err
            (goto-char (point-max))
          (goto-char previous-output)
          (goto-char (point-max)))
        (pop-to-buffer buf))
      (if err (progn (beep) (fsharp-overlay-region (point) end))
        (if inferior-fsharp-output
            (message "No error")
          (message "No output yet...")
          ))
      err)))

(defun fsharp-overlay-region (beg end &optional wait)
  (interactive "%r")
  (cond ((fboundp 'make-overlay)
         (if fsharp-error-overlay ()
           (setq fsharp-error-overlay (make-overlay 1 1))
           (overlay-put fsharp-error-overlay 'face 'region))
         (unwind-protect
             (progn
               (move-overlay fsharp-error-overlay beg end (current-buffer))
               (beep) (if wait (read-event) (fsharp-sit-for 60)))
           (delete-overlay fsharp-error-overlay)))))  

;; wait some amount for ouput, that is, until inferior-fsharp-output is set
;; to true. Hence, interleaves sitting for shorts delays and checking the
;; flag. Give up after some time. Typing into the source buffer will cancel 
;; waiting, i.e. may report 'No result yet' 

(defun fsharp-wait-output (&optional before after)
  (let ((c 1))
    (fsharp-sit-for 0 (or before 1))
    (let ((c 1))
      (while (and (not inferior-fsharp-output) (< c 99) (fsharp-sit-for 0 c t))
        (setq c (+ c 1))))
    (fsharp-sit-for (or after 0) 1)))

;; To insert the last output from fsharp at point
(defun fsharp-insert-last-output ()
  "Insert the result of the evaluation of previous phrase"
  (interactive)
  (let ((pos (process-mark (get-buffer-process inferior-fsharp-buffer-name))))
  (insert-buffer-substring inferior-fsharp-buffer-name
                           fsharp-previous-output (- pos 2))))


(defun fsharp-simple-send (proc string)
  (comint-simple-send proc (concat string ";;")))

(defun fsharp-comint-send ()
  (interactive)
  (let ((comint-input-sender 'fsharp-simple-send))
    (comint-send-input)))

(provide 'inf-fsharp)
