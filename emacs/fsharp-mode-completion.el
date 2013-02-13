;;; fsharp-mode-completion.el --- Autocompletion support for F#

;; Copyright (C) 2012-2013 Robin Neatherway

;; Author: Robin Neatherway <robin.neatherway@gmail.com>
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

(require 'cl)

(defvar ac-fsharp-executable "fsautocomplete.exe")

(defvar ac-fsharp-complete-command
  (let ((exe
         (if (executable-find ac-fsharp-executable)
             (executable-find ac-fsharp-executable)
         (concat (file-name-directory (or load-file-name buffer-file-name))
                 "/bin/" ac-fsharp-executable))))
    (case system-type
      (windows-nt exe)
      (otherwise (list "mono" exe)))))

; Both in seconds. Note that background process uses ms.
(defvar ac-fsharp-blocking-timeout 1)
(defvar ac-fsharp-idle-timeout 1)

(defvar ac-fsharp-status 'idle)
(defvar ac-fsharp-completion-process nil)
(defvar ac-fsharp-partial-data "")
(defvar ac-fsharp-completion-data "")
(defvar ac-fsharp-completion-cache nil)
(defvar ac-fsharp-project-files nil)
(defvar ac-fsharp-idle-timer nil)
(defvar ac-fsharp-verbose nil)
(defvar ac-fsharp-waiting nil)

(defconst eom "\n<<EOF>>\n"
  "End of message marker")

(defun log-to-proc-buf (proc str)
  (when (processp proc)
    (let ((buf (process-buffer proc))
          (atend (with-current-buffer (process-buffer proc)
                   (eq (marker-position (process-mark proc)) (point)))))
      (when (buffer-live-p buf)
        (with-current-buffer buf
          (goto-char (process-mark proc))
          (insert-before-markers str))
        (if atend
            (with-current-buffer buf
              (goto-char (process-mark proc))))))))

(defun log-psendstr (proc str)
  (log-to-proc-buf proc str)
  (process-send-string proc str))

(defun ac-fsharp-parse-current-buffer ()
  (save-restriction
    (widen)
    (log-psendstr
     ac-fsharp-completion-process
     (format "parse \"%s\" full\n%s\n<<EOF>>\n"
             (buffer-file-name)
             (buffer-substring-no-properties (point-min) (point-max))))))

(defun ac-fsharp-parse-file (file)
  (with-current-buffer (find-file-noselect file)
    (ac-fsharp-parse-current-buffer)))

;;;###autoload
(defun ac-fsharp-load-project (file)
  "Load the specified F# file as a project"
  (interactive "f")
  (setq ac-fsharp-completion-cache nil)
  (setq ac-fsharp-partial-data nil)
  (setq ac-fsharp-project-files nil)
  (unless ac-fsharp-completion-process
    (ac-fsharp-launch-completion-process))
  (log-psendstr ac-fsharp-completion-process
                (format "project \"%s\"\n" (expand-file-name file))))



(defun ac-fsharp-send-pos-request (cmd file line col)
  (let ((request (format "%s \"%s\" %d %d %d\n" cmd file line col
                         (* 1000 ac-fsharp-blocking-timeout))))
      (log-psendstr ac-fsharp-completion-process request)))

(defun ac-fsharp-send-error-request ()
  (log-psendstr ac-fsharp-completion-process "errors\n"))

;;;###autoload
(defun ac-fsharp-quit-completion-process ()
  (interactive)
  (message "Quitting fsharp completion process")
  (when 
      (and ac-fsharp-completion-process
	   (process-live-p ac-fsharp-completion-process))
    (log-psendstr ac-fsharp-completion-process "quit\n")
    (sleep-for 1)
    (when (process-live-p ac-fsharp-completion-process)
      (kill-process ac-fsharp-completion-process)))
  (when ac-fsharp-idle-timer
    (cancel-timer ac-fsharp-idle-timer))
  (setq ac-fsharp-completion-process nil)
  (setq ac-fsharp-project-files nil)
  (setq ac-fsharp-partial-data "")
  (ac-fsharp-clear-errors))

;;;###autoload
(defun ac-fsharp-launch-completion-process ()
  "Launch the F# completion process in the background"
  (interactive)
  (if ac-fsharp-completion-process
      (message "Completion process already running. Shutdown existing process first.")
    (message (format "Launching completion process: '%s'"
                     (mapconcat 'identity ac-fsharp-complete-command " ")))
    (setq ac-fsharp-completion-process
          (let ((process-connection-type nil))
            (apply 'start-process
                   "fsharp-complete"
                   "*fsharp-complete*"
                   ac-fsharp-complete-command)))

    (if (process-live-p ac-fsharp-completion-process)
        (progn
          (set-process-filter ac-fsharp-completion-process 'ac-fsharp-filter-output)
          (set-process-query-on-exit-flag ac-fsharp-completion-process nil)
          (setq ac-fsharp-status 'idle)
          (setq ac-fsharp-partial-data "")
          (setq ac-fsharp-project-files))
      (setq ac-fsharp-completion-process nil))

    (setq ac-fsharp-idle-timer
          (run-with-idle-timer
           ac-fsharp-idle-timeout
           t
           (lambda () (ac-fsharp-get-errors)))))

  ;(add-hook 'before-save-hook 'ac-fsharp-reparse-buffer)
  ;(local-set-key (kbd ".") 'completion-at-point)
  )


; Consider using 'text' for filtering
; TODO: This caching is a bit optimistic. It might not always be correct
;       to use the cached values if the line and col just happen to line up.
;       Could dirty cache on idle, or include timestamps and ignore values
;       older than a few seconds. On the other hand it only caches the most
;       recent position, so it's very unlikely to try that position again
;       without the completions being the same unless another completion has
;       been tried in between.
(defun ac-fsharp-completions (file line col text)
  (setq ac-fsharp-waiting t)
  (let ((cache (assoc file ac-fsharp-completion-cache)))
    (if (and cache (equal (cddr cache) (list line col)))
        (cadr cache)
      (ac-fsharp-parse-current-buffer)
      (ac-fsharp-send-pos-request "completion" file line col)
      (while ac-fsharp-waiting
        (accept-process-output ac-fsharp-completion-process))
      (when ac-fsharp-completion-data
        (push (list file ac-fsharp-completion-data line col) ac-fsharp-completion-cache))
      ac-fsharp-completion-data)))

(defun ac-fsharp-completion-at-point ()
  "Return a function ready to interrogate the F# compiler service for completions at point."
  (if ac-fsharp-completion-process
      (let ((end (point))
            (start
             (save-excursion
               (skip-chars-backward "^ ." (line-beginning-position))
               (point))))
        (list start end
              (completion-table-dynamic
               (apply-partially #'ac-fsharp-completions
                                (buffer-file-name)
                                (- (line-number-at-pos) 1)
                                (current-column)))))
  ; else
    nil))

(defun ac-fsharp-can-make-request ()
  (and ac-fsharp-completion-process
       (or
        (member (expand-file-name (buffer-file-name)) ac-fsharp-project-files)
        (string-match-p "\\(fsx\\|fsscript\\)" (file-name-extension (buffer-file-name))))))

;;;###autoload
(defun ac-fsharp-tooltip-at-point ()
  "Fetch and display F# tooltips at point"
  (interactive)
  (require 'pos-tip)
  (when (ac-fsharp-can-make-request)
    (ac-fsharp-parse-current-buffer)
    (ac-fsharp-send-pos-request "tooltip"
                                (buffer-file-name)
                                (- (line-number-at-pos) 1)
                                (current-column))))

;;;###autoload
(defun ac-fsharp-gotodefn-at-point ()
  "Find the point of declaration of the symbol at point and goto it"
  (interactive)
  (when (ac-fsharp-can-make-request)
    (ac-fsharp-parse-current-buffer)
    (ac-fsharp-send-pos-request "finddecl"
                                (buffer-file-name)
                                (- (line-number-at-pos) 1)
                                (current-column))))

(defun ac-fsharp-get-errors ()
  (when (ac-fsharp-can-make-request)
    (ac-fsharp-parse-current-buffer)
    (ac-fsharp-send-error-request)))

(defun line-column-to-pos (line col)
  (save-excursion
    (goto-char (point-min))
    (forward-line (- line 1))
    (if (< (point-max) (+ (point) col))
        (point-max)
      (forward-char col)
      (point))))

(defconst ac-fsharp-error-regexp
  "\\[\\([0-9]+\\):\\([0-9]+\\)-\\([0-9]+\\):\\([0-9]+\\)\\] \\(ERROR\\|WARNING\\) \\(.*\\(?:\n[^[].*\\)*\\)"
  "Regexp to match errors that come from fsautocomplete. Each
starts with a character range for position and is followed by
possibly many lines of description.")



(defun ac-fsharp-show-errors (errors)
  (ac-fsharp-clear-errors)
  (save-match-data
    (while (string-match ac-fsharp-error-regexp errors)
      (ac-fsharp-show-error-overlay
       (line-column-to-pos (+ (string-to-int (match-string 1 errors)) 1)
                           (string-to-int (match-string 2 errors)))
       (line-column-to-pos (+ (string-to-int (match-string 3 errors)) 1)
                           (string-to-int (match-string 4 errors)))
       (if (string= "ERROR" (match-string 5 errors))
           'fsharp-error-face
         'fsharp-warning-face)
       (match-string 6 errors))
      (setq errors (substring errors (match-end 0))))))

(defface fsharp-error-face
  '(
    (((class color) (background dark))
     :underline "Red"
     )
    (((class color) (background light))
     :underline "Red"
     ))
  "Face used for marking an error in F#")

(defface fsharp-warning-face
  '(
    (((class color) (background dark))
     :underline "LightBlue1"
     )
    (((class color) (background light))
     :underline "Blue"
    ))
  "Face used for marking a warning in F#")

(defun ac-fsharp-show-error-overlay (p1 p2 face txt)
  "Overlay the text from p1 to p2 to indicate an error is present here.
   The error is described by txt."
  ; Three cases
  ; 1. No overlays here yet: make it
  ; 2. new warning, exists error: do nothing
  ; 3. new error exists warning: rm warning and make it
  (let ((ofaces (mapcar (lambda (o) (overlay-get o 'face)) (overlays-in p1 p2))))
    (if (and (eq face 'fsharp-warning-face)
           (memq 'fsharp-error-face ofaces))
        nil
      (when (and (eq face 'fsharp-error-face)
                 (memq 'fsharp-warning-face ofaces))
        (remove-overlays p1 p2 'face 'fsharp-warning-face))
      (let ((over (make-overlay p1 p2)))
        (overlay-put over 'face face)
        (overlay-put over 'help-echo txt)))))

(defun ac-fsharp-clear-errors ()
  (interactive)
  (remove-overlays nil nil 'face 'fsharp-error-face)
  (remove-overlays nil nil 'face 'fsharp-warning-face))

(defun ac-fsharp-stash-partial (str)
  (setq ac-fsharp-partial-data (concat ac-fsharp-partial-data str)))

(defun string/starts-with (s arg)
  "returns non-nil if string S starts with ARG.  Else nil."
  (cond ((>= (length s) (length arg))
         (string-equal (substring s 0 (length arg)) arg))
        (t nil)))

(defun string/ends-with (s ending)
  "return non-nil if string S ends with ENDING."
  (let ((elength (length ending)))
    (string= (substring s (- 0 elength)) ending)))

(defun ac-fsharp-filter-output (proc str)

  (log-to-proc-buf proc str)
  (ac-fsharp-stash-partial str)

  (let ((eofloc (string-match-p eom ac-fsharp-partial-data)))
    (when eofloc
      (let ((msg (substring ac-fsharp-partial-data 0 eofloc)))
        (cond
         ((string/starts-with msg "DATA: completion")
          (setq ac-fsharp-completion-data
                (split-string
                 (replace-regexp-in-string "DATA: completion" "" msg)
                 "\n"
                 t))
          (setq ac-fsharp-waiting nil))

         ((string/starts-with msg "DATA: finddecl")
          (if (string-match "\n\\(.*\\):\\([0-9]+\\):\\([0-9]+\\)" msg)
              (let ((file (match-string 1 msg))
                    (line (+ 1 (string-to-int (match-string 2 msg))))
                    (col (string-to-int (match-string 3 msg))))
                (find-file (match-string 1 msg))
                (goto-char (line-column-to-pos line col)))
            (message "Error: unable to find definition")))

         ((string/starts-with msg "DATA: tooltip")
          (let ((data (replace-regexp-in-string "DATA: tooltip\n" ""
                                                msg)))
            (pos-tip-show data)))

         ((string/starts-with msg "DATA: errors")
          (ac-fsharp-show-errors
           (concat (replace-regexp-in-string "DATA: errors\n" "" msg) "\n")))

         ((string/starts-with msg "DATA: project")
          (setq ac-fsharp-project-files
                (cdr (split-string msg "\n")))
          (ac-fsharp-parse-file (car (last ac-fsharp-project-files))))

         ((string/starts-with msg "INFO: ")
          (when ac-fsharp-verbose
            (message msg)))

         ((string/starts-with msg "ERROR: ")
          (message msg)
          (when ac-fsharp-waiting
            (setq ac-fsharp-completion-data nil)
            (setq ac-fsharp-waiting nil)))

         (t
          (message (format "Error unrecognised message: '%s'" msg))))

        (setq ac-fsharp-partial-data (substring ac-fsharp-partial-data
                                                (+ eofloc (length eom))))
        (ac-fsharp-filter-output proc "")))))

(provide 'fsharp-mode-completion)

;;; fsharp-mode-completion.el ends here
