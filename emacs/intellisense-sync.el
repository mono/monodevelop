;;; intellisense-sync.el --- Autocompletion support for F#

;; Copyright (C) 2012 Robin Neatherway

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

(setq ac-fsharp-complete-executable
      (concat (file-name-directory (or load-file-name buffer-file-name))
              "/bin/fsautocomplete.exe"))

(defvar ac-fsharp-status 'idle)
(defvar ac-fsharp-completion-process nil)
(defvar ac-fsharp-partial-data "")
(defvar ac-fsharp-data "")

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

;; (process-buffer ac-fsharp-completion-process)
;; (process-mark ac-fsharp-completion-process)
;; (with-current-buffer
;;     (process-buffer ac-fsharp-completion-process)
;;   (point))

;; (let ((proc ac-fsharp-completion-process)
;;       (buf (process-buffer ac-fsharp-completion-process)))
;;   (save-selected-window
;;     (select-window (get-buffer-window buf))
;;     (goto-char (process-mark proc))))

(defun log-psendstr (proc str)
  (log-to-proc-buf proc str)
  (process-send-string proc str))

(defun ac-fsharp-parse-file ()
  (interactive)
  (message "Parsing file")
  (save-restriction
    (widen)
    (log-psendstr
     ac-fsharp-completion-process
     (format "parse \"%s\" full\n%s\n<<EOF>>\n"
             (buffer-file-name)
             (buffer-substring-no-properties (point-min) (point-max))))))

(defun ac-fsharp-load-project ()
  (interactive)
  (message (format "Loading project %s" buffer-file-name))
  (log-psendstr ac-fsharp-completion-process
                (format "project \"%s\"\n" buffer-file-name)))

(defun ac-fsharp-send-completion-request (line col)
  (let ((request (format "completion \"%s\" %d %d\n"
                               (buffer-file-name) line col)))
    (message (format "Sending completion request for: '%s' of '%s'" ac-prefix request))
      (log-psendstr ac-fsharp-completion-process request)))


(defun ac-fsharp-send-shutdown-command ()
  (interactive)
  (message "sending shut down")
  (log-psendstr ac-fsharp-completion-process "quit\n"))

(defun ac-fsharp-launch-completion-process ()
  (interactive)
  (message "Launching completion process")
  (setq ac-fsharp-completion-process
        (let ((process-connection-type nil))
          (case system-type
            (windows-nt
             (start-process "fsharp-complete"
                            "*fsharp-complete*"
                            ac-fsharp-complete-executable))
            (otherwise
             (start-process "fsharp-complete"
                            "*fsharp-complete*"
                            "mono" ac-fsharp-complete-executable)))))

  (set-process-filter ac-fsharp-completion-process 'ac-fsharp-filter-output)
  (set-process-query-on-exit-flag ac-fsharp-completion-process nil)

  (setq ac-fsharp-status 'idle)

  ;(add-hook 'kill-buffer-hook 'ac-fsharp-shutdown-process nil t)
  ;(add-hook 'before-save-hook 'ac-fsharp-reparse-buffer)

  ;(local-set-key (kbd ".") 'ac-fsharp-async-preemptive))
  )


; Consider using 'text' for filtering
(defun fsharp-completions (line col text)
  (message (format "fsharp-completions called for (%d,%d) and '%s'" line col text))
  (progn
    (setq ac-fsharp-status 'fetch-in-progress)
    (setq ac-fsharp-data nil)
    (ac-fsharp-parse-file)
    (ac-fsharp-send-completion-request line col)
    (while (eq ac-fsharp-status 'fetch-in-progress)
      (accept-process-output ac-fsharp-completion-process))
    ac-fsharp-data))

(defun fsharp-completion-at-point ()
  "Return a function ready to interrogate the F# compiler service for completions at point."
  (let ((end (point))
        (start
         (save-excursion
           (skip-chars-backward "^ ." (line-beginning-position))
           (point))))
    (message (format "completion-at-point called for (%d,%d)" start end))
    (list start end
          (completion-table-dynamic
           (apply-partially #'fsharp-completions
                            (- (line-number-at-pos) 1)
                            (current-column))))
            ))

(add-hook 'completion-at-point-functions #'fsharp-completion-at-point)
;(set (make-local-variable 'gud-gdb-completion-function) 'gud-gdb-completions)

;(local-set-key "\C-i" 'completion-at-point)
;(local-set-key "\C-i" 'indent-for-tab-command)

(defun ac-fsharp-stash-partial (str)
  (setq ac-fsharp-partial-data (concat ac-fsharp-partial-data str)))


; str function is called whenever fsintellisense.exe writes something on stdout
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
             (message "ac-fsharp-filter-output setting current candidate")
             (setq ac-fsharp-data help)
             (setq ac-fsharp-status 'idle)
             ))))
    (otherwise
     (message "filter output called and found <<EOF>> while not waiting")
    )))

;;; intellisense-sync.el ends here
