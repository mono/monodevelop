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

(defvar ac-fsharp-status 'idle)
(defvar ac-fsharp-completion-process nil)
(defvar ac-fsharp-partial-data "")
(defvar ac-fsharp-data "")
(defvar ac-fsharp-completion-cache nil)

(defun log-to-proc-buf (proc str)
  (let ((buf (process-buffer proc))
        (atend (with-current-buffer (process-buffer proc)
                 (eq (marker-position (process-mark proc)) (point)))))
    (when (buffer-live-p buf)
      (with-current-buffer buf
        (goto-char (process-mark proc))
        (insert-before-markers str))
      (if atend
          (with-current-buffer buf
            (goto-char (process-mark proc)))))))

(defun log-psendstr (proc str)
  (log-to-proc-buf proc str)
  (process-send-string proc str))

(defun ac-fsharp-parse-file (file)
  (save-restriction
    (widen)
    (log-psendstr
     ac-fsharp-completion-process
     (format "parse \"%s\" full\n%s\n<<EOF>>\n"
             file
             (buffer-substring-no-properties (point-min) (point-max))))))

;;;###autoload
(defun ac-fsharp-load-project (file)
  "Load the specified F# file as a project"
  (interactive "f")
  (setq ac-fsharp-completion-cache nil)
  (unless ac-fsharp-completion-process
    (ac-fsharp-launch-completion-process))
  (log-psendstr ac-fsharp-completion-process
                (format "project \"%s\"\n" (expand-file-name file))))

(defun ac-fsharp-send-completion-request (file line col)
  (let ((request (format "completion \"%s\" %d %d\n" file line col)))
      (log-psendstr ac-fsharp-completion-process request)))

(defun ac-fsharp-send-tooltip-request (file line col)
  (let ((request (format "tip \"%s\" %d %d\n" file line col)))
      (log-psendstr ac-fsharp-completion-process request)))

;;;###autoload
(defun ac-fsharp-quit-completion-process ()
  (interactive)
  (log-psendstr ac-fsharp-completion-process "quit\n")
  (setq ac-fsharp-completion-process nil))

;;;###autoload
(defun ac-fsharp-launch-completion-process ()
  "Launch the F# completion process in the background"
  (interactive)
  (message "Launching completion process")
  (setq ac-fsharp-completion-process
        (let ((process-connection-type nil))
          (apply 'start-process
                 "fsharp-complete"
                 "*fsharp-complete*"
                 ac-fsharp-complete-command)))

  (set-process-filter ac-fsharp-completion-process 'ac-fsharp-filter-output)
  (set-process-query-on-exit-flag ac-fsharp-completion-process nil)

  (setq ac-fsharp-status 'idle)

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
  (setq ac-fsharp-status 'fetch-in-progress)
  (setq ac-fsharp-data nil)
  (let ((cache (assoc file ac-fsharp-completion-cache)))
    (if (and cache (equal (cddr cache) (list line col)))
        (cadr cache)
      (ac-fsharp-parse-file file)
      (ac-fsharp-send-completion-request file line col)
      (while (eq ac-fsharp-status 'fetch-in-progress)
        (accept-process-output ac-fsharp-completion-process))
      (push (list file ac-fsharp-data line col) ac-fsharp-completion-cache)
      ac-fsharp-data)
    ))

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
                                (current-column))))
        )
    nil))

;;;###autoload
(defun ac-fsharp-tooltip-at-point ()
  "Fetch and display F# tooltips at point"
  (interactive)
  (require 'pos-tip)
  (if ac-fsharp-completion-process
      (progn
        (setq ac-fsharp-status 'fetch-in-progress)
        (setq ac-fsharp-data nil)
        (ac-fsharp-parse-file (buffer-file-name))
        (ac-fsharp-send-tooltip-request (buffer-file-name) (- (line-number-at-pos) 1) (current-column))
        (while (eq ac-fsharp-status 'fetch-in-progress)
          (accept-process-output ac-fsharp-completion-process))
        (if (eq 0 (length ac-fsharp-data))
            (setq ac-fsharp-data '("No tooltip data available")))
        (pos-tip-show (mapconcat 'identity ac-fsharp-data "\n")))))

(defun ac-fsharp-stash-partial (str)
  (setq ac-fsharp-partial-data (concat ac-fsharp-partial-data str)))

(defun ac-fsharp-filter-output (proc str)

  (log-to-proc-buf proc str)

  (case ac-fsharp-status
    (fetch-in-progress
     (ac-fsharp-stash-partial str)
     (if (and
          (>= (length str) 8)
          (string= (substring str -8 nil) "<<EOF>>\n"))
         (progn
           (setq str ac-fsharp-partial-data)
           (setq ac-fsharp-partial-data "")
           (setq str (replace-regexp-in-string "<<EOF>>" "" str))
           (setq str (replace-regexp-in-string "DONE: Background parsing started" "" str))
           (setq str (replace-regexp-in-string "\n\n" "\n" str))

           (let ((help (split-string str "[\n]+" t)))
             (setq ac-fsharp-data help)
             (setq ac-fsharp-status 'idle)
             ))))
    (otherwise
     ;(message "filter output called and found <<EOF>> while not waiting")
    )))

(provide 'fsharp-mode-completion)

;;; fsharp-mode-completion.el ends here
