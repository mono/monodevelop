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

(require 'namespaces)

(namespace fsharp-mode-completion
  :export
  [load-project
   start-process
   show-tooltip-at-point
   show-typesig-at-point]
  :use
  [(popup popup-tip)
   (pos-tip pos-tip-show)
   s
   (fsharp-doc fsharp-doc/format-for-minibuffer)
   (fsharp-mode-indent fsharp-in-literal-p)])

;;; User-configurable variables

(defvar ac-fsharp-executable "fsautocomplete.exe")

(defvar ac-fsharp-complete-command
  (let ((exe (or (executable-find ac-fsharp-executable)
                 (concat (file-name-directory (or load-file-name buffer-file-name))
                         "bin/" ac-fsharp-executable))))
    (case system-type
      (windows-nt exe)
      (otherwise (list "mono" exe)))))

(defvar ac-fsharp-use-popup t
  "Display tooltips using a popup at point. If set to nil,
display in a help buffer instead.")

; Both in seconds. Note that background process uses ms.
(defvar ac-fsharp-blocking-timeout 1)
(defvar ac-fsharp-idle-timeout 1)

;;; ----------------------------------------------------------------------------

(defvar ac-fsharp-status 'idle)
(defvar ac-fsharp-completion-process nil)
(defvar ac-fsharp-partial-data "")
(defvar ac-fsharp-completion-data "")
(defvar ac-fsharp-completion-cache nil)
(defvar ac-fsharp-project-files nil)
(defvar ac-fsharp-idle-timer nil)
(defvar ac-fsharp-verbose nil)
(defvar ac-fsharp-waiting nil)

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
    (process-send-string
     ac-fsharp-completion-process
     (format "parse \"%s\" full\n%s\n<<EOF>>\n"
             (buffer-file-name)
             (buffer-substring-no-properties (point-min) (point-max))))))

(defun ac-fsharp-parse-file (file)
  (with-current-buffer (find-file-noselect file)
    (ac-fsharp-parse-current-buffer)))

;;;###autoload
(defn load-project (file)
  "Load the specified F# file as a project"
  (assert (equal "fsproj" (file-name-extension file))  ()
          "The given file was not an F# project.")

  ;; Prompt user for an fsproj, searching for a default.
  (interactive
   (list (read-file-name
          "Path to project: "
          (fsharp-mode/find-fsproj buffer-file-name)
          (fsharp-mode/find-fsproj buffer-file-name))))

  ;; Reset state.
  (setq ac-fsharp-completion-cache nil
        ac-fsharp-partial-data nil
        ac-fsharp-project-files nil)

  ;; Launch the completion process and update the current project.
  (let ((f (expand-file-name file)))
    (unless ac-fsharp-completion-process
      (_ start-process))
    (log-psendstr ac-fsharp-completion-process
                  (format "project \"%s\"\n" (expand-file-name file)))))

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
  (setq ac-fsharp-status 'idle
        ac-fsharp-completion-process nil
        ac-fsharp-partial-data ""
        ac-fsharp-completion-data ""
        ac-fsharp-completion-cache nil
        ac-fsharp-project-files nil
        ac-fsharp-idle-timer nil
        ac-fsharp-verbose nil
        ac-fsharp-waiting nil)
  (ac-fsharp-clear-errors))

;;;###autoload
(defn start-process ()
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
          (set-process-filter ac-fsharp-completion-process (~ filter-output))
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

(defmutable awaiting-tooltip nil)

;;;###autoload
(defn show-tooltip-at-point ()
  "Display a tooltip for the F# symbol at POINT."
  (interactive)
  (@set awaiting-tooltip t)
  (_ show-typesig-at-point))

;;;###autoload
(defn show-typesig-at-point ()
  "Display the type signature for the F# symbol at POINT."
  (interactive)
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
       (line-column-to-pos (+ (string-to-number (match-string 1 errors)) 1)
                           (string-to-number (match-string 2 errors)))
       (line-column-to-pos (+ (string-to-number (match-string 3 errors)) 1)
                           (string-to-number (match-string 4 errors)))
       (if (string= "ERROR" (match-string 5 errors))
           'fsharp-error-face
         'fsharp-warning-face)
       (match-string 6 errors))
      (setq errors (substring errors (match-end 0))))))

(defface fsharp-error-face
  '(
    (((class color) (background dark))
     :weight bold
     :underline "Red"
     )
    (((class color) (background light))
     :weight bold
     :underline "Red"
     ))
  "Face used for marking an error in F#"
  :group 'fsharp)

(defface fsharp-warning-face
  '(
    (((class color) (background dark))
     :weight bold
     :underline "LightBlue1"
     )
    (((class color) (background light))
     :weight bold
     :underline "Blue"
     ))
  "Face used for marking a warning in F#"
  :group 'fsharp)

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

(defun ac-fsharp-electric-dot ()
  (interactive)
  (insert ".")
  (unless (fsharp-in-literal-p)
    (completion-at-point)))

;;; ----------------------------------------------------------------------------
;;; Process handling
;;; Handle output from the completion process.

(def eom "\n<<EOF>>\n")

(defn filter-output (proc str)
  "Filter output from the completion process and handle appropriately."
  (log-to-proc-buf proc str)
  (ac-fsharp-stash-partial str)

  (let ((eofloc (string-match-p (@ eom) ac-fsharp-partial-data)))
    (while eofloc
      (let ((msg  (substring ac-fsharp-partial-data 0 eofloc))
            (part (substring ac-fsharp-partial-data (+ eofloc (length (@ eom))))))
        (cond
         ((s-starts-with? "DATA: completion" msg) (_ set-completion-data msg))
         ((s-starts-with? "DATA: finddecl" msg)   (_ visit-definition msg))
         ((s-starts-with? "DATA: tooltip" msg)    (_ handle-tooltip msg))
         ((s-starts-with? "DATA: errors" msg)     (_ display-parse-errors msg))
         ((s-starts-with? "DATA: project" msg)    (_ handle-project msg))
         ((s-starts-with? "ERROR: " msg)          (_ handle-process-error msg))
         ((s-starts-with? "INFO: " msg) (when ac-fsharp-verbose (message msg)))
         (t
          (message "Error: unrecognised message: '%s'" msg)))

        (setq ac-fsharp-partial-data part))
      (setq eofloc (string-match-p (@ eom) ac-fsharp-partial-data)))))

(defn set-completion-data (str)
  (setq ac-fsharp-completion-data (s-split "\n" (s-replace "DATA: completion" "" str) t)
        ac-fsharp-waiting nil))

(defn visit-definition (str)
  (if (string-match "\n\\(.*\\):\\([0-9]+\\):\\([0-9]+\\)" str)
      (let ((file (match-string 1 str))
            (line (+ 1 (string-to-number (match-string 2 str))))
            (col (string-to-number (match-string 3 str))))
        (find-file (match-string 1 str))
        (goto-char (line-column-to-pos line col)))
    (message "Unable to find definition.")))

(defn display-parse-errors (str)
  (ac-fsharp-show-errors
   (concat (replace-regexp-in-string "DATA: errors\n" "" str) "\n")))

(defn handle-tooltip (str)
  "Display information from the background process. If the user
has requested a popup tooltip, display a popup. Otherwise,
display a short summary in the minibuffer."
  ;; Do not display if the current buffer is not an fsharp buffer.
  (when (equal major-mode 'fsharp-mode)
    (let ((cleaned (replace-regexp-in-string "DATA: tooltip\n" "" str)))

      (if (@ awaiting-tooltip)
          (progn
            (@set awaiting-tooltip nil)
            (if ac-fsharp-use-popup
                (_ show-popup cleaned)
              (_ show-info-window cleaned)))
        (message (fsharp-doc/format-for-minibuffer cleaned))))))

(defn show-popup (str)
  (if (display-graphic-p)
      (pos-tip-show str)
    ;; Use unoptimized calculation for popup, making it less likely to
    ;; wrap lines.
    (let ((popup-use-optimized-column-computation nil) )
      (popup-tip str))))

(def info-buffer-name "*fsharp info*")

(defn show-info-window (str)
  (save-excursion
    (let ((help-window-select t))
      (with-help-window (@ info-buffer-name)
        (princ str)))))

(defn handle-project (str)
  (setq ac-fsharp-project-files (cdr (split-string str "\n")))
  (ac-fsharp-parse-file (car (last ac-fsharp-project-files))))

(defn handle-process-error (str)
  (unless (s-matches? "Could not get type information" str)
    (message str))
  (when ac-fsharp-waiting
    (setq ac-fsharp-completion-data nil)
    (setq ac-fsharp-waiting nil)))

;;; fsharp-mode-completion.el ends here
