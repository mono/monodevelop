;;; inf-fsharp-mode.el --- Support for F# interactive

;; Copyright (C) 1997 INRIA

;; Author: 1993-1997 Xavier Leroy, Jacques Garrigue
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

(require 'comint)
(with-no-warnings (require 'cl))

;; User modifiable variables

;; Whether you want the output buffer to be diplayed when you send a phrase

(defvar fsharp-display-when-eval t
  "*If true, display the inferior fsharp buffer when evaluating expressions.")

(defvar inferior-fsharp-program
  (case system-type
    (windows-nt "fsi.exe")
    (otherwise "fsharpi --readline-"))
  "*Program name for invoking an inferior fsharp from Emacs.")


;; End of User modifiable variables


(defvar inferior-fsharp-mode-map nil)
(if inferior-fsharp-mode-map nil
  (setq inferior-fsharp-mode-map
        (copy-keymap comint-mode-map)))

;; Augment fsharp mode, so you can process fsharp code in the source files.

(defun inferior-fsharp-mode ()
  "Major mode for interacting with an inferior fsharp process.
Runs a fsharp toplevel as a subprocess of Emacs, with I/O through an
Emacs buffer. A history of input phrases is maintained. Phrases can
be sent from another buffer in fsharp mode.

\\{inferior-fsharp-mode-map}"
  (interactive)
  (comint-mode)
  (setq comint-prompt-regexp "^# ?")
  (setq comint-prompt-read-only t)
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
  (setq minor-mode-map-alist (assq-delete-all 'compilation-minor-mode (copy-seq minor-mode-map-alist)))
)


(defconst inferior-fsharp-buffer-subname "inferior-fsharp")
(defconst inferior-fsharp-buffer-name
  (concat "*" inferior-fsharp-buffer-subname "*"))

(defun fsharp-run-process-if-needed (&optional cmd)
  "Launch fsi if needed, using CMD if supplied."
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
    ))

;;;###autoload
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
        (goto-char (point-max)))
    (while
        (> count 0)
      (previous-multiframe-window)
      (setq count (- count 1)))
    )
)

(defun inferior-fsharp-eval-region (start end)
  "Send the current region to the inferior fsharp process."
  (interactive "r")
  (save-excursion (fsharp-run-process-if-needed))
  (save-excursion
    ;; send location to fsi
    (let* (
          (name (file-truename (buffer-name (current-buffer))))
          (dir (file-name-directory name))
          (line (number-to-string (line-number-at-pos start)))
          (loc (concat "# " line " \"" name "\"\n"))
          (movedir (concat "#silentCd @\"" dir "\";;\n")))
      (comint-send-string inferior-fsharp-buffer-name movedir)
      (comint-send-string inferior-fsharp-buffer-name loc))
    (goto-char end)
    (comint-send-region inferior-fsharp-buffer-name start (point))
    ;; normally, ";;" are part of the region
    (if (and (>= (point) 2)
             (prog2 (backward-char 2) (looking-at ";;")))
        (comint-send-string inferior-fsharp-buffer-name "\n")
      (comint-send-string inferior-fsharp-buffer-name "\n;;\n"))
    ;; the user may not want to see the output buffer
    (if fsharp-display-when-eval
        (display-buffer inferior-fsharp-buffer-name t))))

(defvar fsharp-previous-output nil
  "tells the beginning of output in the shell-output buffer, so that the
output can be retreived later, asynchronously.")

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

(provide 'inf-fsharp-mode)

;;; inf-sharp-mode.el ends here
