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

(require 's)
(require 'dash)
(require 'fsharp-mode-indent)
(require 'auto-complete)
(require 'json)

(autoload 'pos-tip-fill-string "pos-tip")
(autoload 'pos-tip-show "pos-tip")
(autoload 'popup-tip "popup")

;;; User-configurable variables

(defvar fsharp-ac-executable "fsautocomplete.exe")

(defvar fsharp-ac-complete-command
  (let ((exe (or (executable-find fsharp-ac-executable)
                 (concat (file-name-directory (or load-file-name buffer-file-name))
                         "bin/" fsharp-ac-executable))))
    (case system-type
      (windows-nt (list exe))
      (otherwise (list "mono" exe)))))

(defvar fsharp-ac-use-popup t
  "Display tooltips using a popup at point. If set to nil,
display in a help buffer instead.")

(defvar fsharp-ac-intellisense-enabled t
  "Whether autocompletion is automatically triggered on '.'")

(defface fsharp-error-face
  '((t :inherit error))
  "Face used for marking an error in F#"
  :group 'fsharp)

(defface fsharp-warning-face
  '((t :inherit warning))
  "Face used for marking a warning in F#"
  :group 'fsharp)

;;; Both in seconds. Note that background process uses ms.
(defvar fsharp-ac-blocking-timeout 0.4)
(defvar fsharp-ac-idle-timeout 1)

;;; ----------------------------------------------------------------------------

(defvar fsharp-ac-debug nil)
(defvar fsharp-ac-status 'idle)
(defvar fsharp-ac-completion-process nil)
(defvar fsharp-ac-project-files nil)
(defvar fsharp-ac-idle-timer nil)
(defvar fsharp-ac-verbose nil)
(defvar fsharp-ac-current-candidate nil)
(defvar fsharp-ac-current-helptext (make-hash-table :test 'equal))

(defconst fsharp-ac--log-buf "*fsharp-debug*")

(defun fsharp-ac--log (str)
  (when fsharp-ac-debug
    (unless (get-buffer fsharp-ac--log-buf)
      (generate-new-buffer fsharp-ac--log-buf))
    (with-current-buffer fsharp-ac--log-buf
      (let ((pt (point))
            (atend (eq (point-max) (point))))
        (goto-char (point-max))
        (insert-before-markers (format "%s: %s" (float-time) str))
        (unless atend
          (goto-char pt))))))

(defun log-psendstr (proc str)
  (fsharp-ac--log str)
  (process-send-string proc str))

(defun fsharp-ac-parse-current-buffer ()
  (if (> (buffer-modified-tick) fsharp-ac-last-parsed-ticks)
      (save-restriction
	(let ((file (expand-file-name (buffer-file-name))))
	  (widen)
	  (fsharp-ac--log (format "Parsing \"%s\"\n" file))
	  (process-send-string
	   fsharp-ac-completion-process
	   (format "parse \"%s\"\n%s\n<<EOF>>\n"
		   file
		   (buffer-substring-no-properties (point-min) (point-max)))))
	(setq fsharp-ac-last-parsed-ticks (buffer-modified-tick)))))

(defun fsharp-ac-parse-file (file)
  (with-current-buffer (find-file-noselect file)
    (fsharp-ac-parse-current-buffer)))


(defun fsharp-ac--isIdChar (c)
  (let ((gc (get-char-code-property c 'general-category)))
    (or
     (-any? (lambda (x) (string= gc x)) '("Lu" "Ll" "Lt" "Lm" "Lo" "Nl" "Nd" "Pc" "Mn" "Mc"))
     (eq c 39))))

(defun fsharp-ac--isNormalId (s)
  (-all? (lambda (x) x) (mapcar 'fsharp-ac--isIdChar s)))

;;; ----------------------------------------------------------------------------
;;; File Parsing and loading

(defun fsharp-ac/load-project (file)
  "Load the specified F# file as a project"
  (interactive
  ;; Prompt user for an fsproj, searching for a default.
   (let* ((proj (fsharp-mode/find-fsproj buffer-file-name))
          (relproj (file-relative-name proj (file-name-directory buffer-file-name)))
          (prompt (if proj (format "Path to project (default %s): " relproj)
                    "Path to project: ")))
     (list (read-file-name prompt nil (fsharp-mode/find-fsproj buffer-file-name) t))))
  (when (fsharp-ac--valid-project-p file)
    (fsharp-ac--reset)
    (when (not (fsharp-ac--process-live-p))
      (fsharp-ac/start-process))
    ;; Load given project.
    (when (fsharp-ac--process-live-p)
      (log-psendstr fsharp-ac-completion-process
                    (format "project \"%s\"\n" (expand-file-name file))))
    file))

(defun fsharp-ac/load-file (file)
  "Start the compiler binding for an individual F# script."
  (when (fsharp-ac--script-file-p file)
    (if (file-exists-p file)
        (when (not (fsharp-ac--process-live-p))
          (fsharp-ac/start-process))
      (add-hook 'after-save-hook 'fsharp-ac--load-after-save nil 'local))))

(defun fsharp-ac--load-after-save ()
  (remove-hook 'fsharp-ac--load-after-save 'local)
  (fsharp-ac/load-file (buffer-file-name)))

(defun fsharp-ac--valid-project-p (file)
  (and file
       (file-exists-p file)
       (string-match-p (rx "." "fsproj" eol) file)))

(defun fsharp-ac--script-file-p (file)
  (and file
       (string-match-p (rx (or "fsx" "fsscript"))
                       (file-name-extension file))))

(defun fsharp-ac--reset ()
  (setq fsharp-ac-project-files nil
        fsharp-ac-status 'idle
        fsharp-ac-current-candidate nil)
  (clrhash fsharp-ac-current-helptext)
  (fsharp-ac-clear-errors))

;;; ----------------------------------------------------------------------------
;;; Display Requests

(defun fsharp-ac-send-pos-request (cmd file line col)
  (log-psendstr fsharp-ac-completion-process
                (format "%s \"%s\" %d %d %d\n" cmd file line col
                        (* 1000 fsharp-ac-blocking-timeout))))

(defun fsharp-ac--process-live-p ()
  "Check whether the background process is live"
  (and fsharp-ac-completion-process
       (process-live-p fsharp-ac-completion-process)))

(defun fsharp-ac/stop-process ()
  (interactive)
  (fsharp-ac-message-safely "Quitting fsharp completion process")
  (when (fsharp-ac--process-live-p)
    (log-psendstr fsharp-ac-completion-process "quit\n")
    (sleep-for 1)
    (when (and fsharp-ac-completion-process (process-live-p fsharp-ac-completion-process))
      (kill-process fsharp-ac-completion-process))))

(defun fsharp-ac/start-process ()
  "Launch the F# completion process in the background"
  (interactive)

  (when (fsharp-ac--process-live-p)
    (kill-process fsharp-ac-completion-process))

  (condition-case nil
      (progn
        (setq fsharp-ac-completion-process (fsharp-ac--configure-proc))
        (fsharp-ac--reset-timer))
    (error
     (setq fsharp-ac-intellisense-enabled nil)
     (message "Failed to start fsautocomplete. Disabling intellisense."))))

(defun fsharp-ac--process-sentinel (process event)
  "Default sentinel used by `fsharp-ac--configure-proc"
  (when (memq (process-status process) '(exit signal))
    (when fsharp-ac-idle-timer
      (cancel-timer fsharp-ac-idle-timer))
    (mapc (lambda (buf)
	    (with-current-buffer buf
	      (when (eq major-mode 'fsharp-mode)
		(setq fsharp-ac-last-parsed-ticks 0)
		(fsharp-ac-clear-errors))))
	  (buffer-list))
    (setq fsharp-ac-status 'idle
	  fsharp-ac-completion-process nil
	  fsharp-ac-project-files nil
	  fsharp-ac-idle-timer nil
	  fsharp-ac-verbose nil)))

(defun fsharp-ac--configure-proc ()
  (let ((proc (let (process-connection-type)
                (apply 'start-process "fsharp-complete" "*fsharp-complete*"
                       fsharp-ac-complete-command))))
    (sleep-for 0.1)
    (if (process-live-p proc)
        (progn
	  (set-process-sentinel proc #'fsharp-ac--process-sentinel)
	  (set-process-coding-system proc 'utf-8-auto)
          (set-process-filter proc 'fsharp-ac-filter-output)
          (set-process-query-on-exit-flag proc nil)
          (setq fsharp-ac-status 'idle
                fsharp-ac-project-files nil)
          (with-current-buffer (process-buffer proc)
            (delete-region (point-min) (point-max)))
          (add-to-list 'ac-modes 'fsharp-mode)
          (log-psendstr proc "outputmode json\n")
          proc)
      (fsharp-ac-message-safely "Failed to launch: '%s'"
                                (s-join " " fsharp-ac-complete-command))
      nil)))

(defun fsharp-ac--reset-timer ()
  (when fsharp-ac-idle-timer
    (cancel-timer fsharp-ac-idle-timer))
  (setq fsharp-ac-idle-timer
        (run-with-idle-timer fsharp-ac-idle-timeout
                             t
                             'fsharp-ac--parse-current-file)))


(defvar fsharp-ac-source
  '((candidates . fsharp-ac-candidate)
    (prefix . fsharp-ac--residue)
    (requires . 0)
    (document . fsharp-ac-document)
    ;(action . fsharp-ac-action)
    (cache) ; this prevents multiple re-calls, critical
    ))

(defun fsharp-ac-document (item)
  (let* ((ticks (s-match "^``\\(.*\\)``$" item))
         (key (if ticks (cadr ticks) item))
         (prop (gethash key fsharp-ac-current-helptext)))
    (let ((help
           (if prop prop
             (log-psendstr fsharp-ac-completion-process
                           (format "helptext %s\n" key))
             (with-local-quit
               (accept-process-output fsharp-ac-completion-process 0 100))
             (gethash key fsharp-ac-current-helptext
                      "Loading documentation..."))))
      (pos-tip-fill-string help popup-tip-max-width))))

(defun fsharp-ac-candidate ()
  (interactive)
  (case fsharp-ac-status
    (idle
     (setq fsharp-ac-status 'wait)
     (setq fsharp-ac-current-candidate nil)
     (clrhash fsharp-ac-current-helptext)

     (fsharp-ac-parse-current-buffer)
     (fsharp-ac-send-pos-request
      "completion"
      (expand-file-name (buffer-file-name (current-buffer)))
      (line-number-at-pos)
      (current-column)))

    (wait
     fsharp-ac-current-candidate)

    (acknowledged
     (setq fsharp-ac-status 'idle)
     fsharp-ac-current-candidate)))

(defconst fsharp-ac--ident
  (rx (one-or-more (not (any ".` \t\r\n"))))
  "Regexp for normal identifiers")

; Note that this regexp is not 100% correct.
; Allowable characters are defined using unicode
; character classes, so this will match some very
; unusual strings composed of rare unicode chars.
(defconst fsharp-ac--rawIdent
  (rx (seq
       "``"
       (one-or-more
        (or
         (not (any "`\n\r\t"))
         (seq "`" (not (any "`\n\r\t")))))
       "``"))
  "Regexp for raw identifiers")

(defconst fsharp-ac--rawIdResidue
  (rx (seq
       "``"
       (one-or-more
        (or
         (not (any "`\n\r\t"))
         (seq "`" (not (any "`\n\r\t")))))
       string-end))
  "Regexp for residues starting with backticks")

(defconst fsharp-ac--dottedIdentNormalResidue
  (rx-to-string
   `(seq (zero-or-more
          (seq
           (or (regexp ,fsharp-ac--ident)
               (regexp ,fsharp-ac--rawIdent))
           "."))
         (group (zero-or-more (not (any ".` \t\r\n"))))
         string-end))
  "Regexp for a dotted ident with a standard residue")

(defconst fsharp-ac--dottedIdentRawResidue
  (rx-to-string `(seq (zero-or-more
                       (seq
                        (or (regexp ,fsharp-ac--ident)
                            (regexp ,fsharp-ac--rawIdent))
                        "."))
                      (group (regexp ,fsharp-ac--rawIdResidue))))
  "Regexp for a dotted ident with a raw residue")

(defun fsharp-ac--residue ()
  (let ((result
         (let ((line (buffer-substring-no-properties (line-beginning-position) (point))))
           (- (point)
              (cadr
                (-min-by 'car-less-than-car
                 (-map (lambda (r) (let ((e (-map 'length (s-match r line))))
                                (if e e '(0 0))))
                       (list fsharp-ac--dottedIdentRawResidue
                             fsharp-ac--dottedIdentNormalResidue))))))))
    result))

(defun fsharp-ac-can-make-request ()
  "Test whether it is possible to make a request with the compiler binding.
The current buffer must be an F# file that exists on disk."
  (let ((file (buffer-file-name)))
    (and file
         (fsharp-ac--process-live-p)
         (not ac-completing)
         (eq fsharp-ac-status 'idle)
         (or (member (file-truename file) fsharp-ac-project-files)
             (string-match-p (rx (or "fsx" "fsscript"))
                             (file-name-extension file))))))

(defvar fsharp-ac-awaiting-tooltip nil)

(defun fsharp-ac/show-tooltip-at-point ()
  "Display a tooltip for the F# symbol at POINT."
  (interactive)
  (setq fsharp-ac-awaiting-tooltip t)
  (fsharp-ac/show-typesig-at-point))

(defun fsharp-ac/show-typesig-at-point ()
  "Display the type signature for the F# symbol at POINT."
  (interactive)
  (when (fsharp-ac-can-make-request)
     (fsharp-ac-parse-current-buffer)
     (fsharp-ac-send-pos-request "tooltip"
                                 (expand-file-name (buffer-file-name))
                                 (line-number-at-pos)
                                 (current-column))))

(defun fsharp-ac/gotodefn-at-point ()
  "Find the point of declaration of the symbol at point and goto it"
  (interactive)
  (when (fsharp-ac-can-make-request)
    (fsharp-ac-parse-current-buffer)
    (fsharp-ac-send-pos-request "finddecl"
                                (expand-file-name (buffer-file-name))
                                (line-number-at-pos)
                                (current-column))))

(defun fsharp-ac--ac-start (&rest ac-start-args)
  "Start completion, using only the F# completion source for intellisense."
  (interactive)
  (let ((ac-sources '(fsharp-ac-source))
        (ac-auto-show-menu t))
    (apply 'ac-start ac-start-args)))


(defun fsharp-ac/electric-dot ()
  (interactive)
  (when ac-completing
    (ac-complete))
  (when (or (not (eq (string-to-char ".") (char-before)))
            (not ac-completing))
    (self-insert-command 1))
  (fsharp-ac/complete-at-point))


(defun fsharp-ac/electric-backspace ()
  (interactive)
  (when (eq (char-before) (string-to-char "."))
    (ac-stop))
  (delete-char -1))

(define-key ac-completing-map
  (kbd "<backspace>") 'fsharp-ac/electric-backspace)
(define-key ac-completing-map
  (kbd ".") 'self-insert-command)

(defun fsharp-ac/complete-at-point ()
  (interactive)
  (when (and (fsharp-ac-can-make-request)
           (eq fsharp-ac-status 'idle)
           fsharp-ac-intellisense-enabled)
      (fsharp-ac--ac-start)))

;;; ----------------------------------------------------------------------------
;;; Errors and Overlays

(defstruct fsharp-error start end face text file)

(defvar fsharp-ac-errors)

(defvar fsharp-ac-last-parsed-ticks 0
  "BUFFER's tick counter, when the file was parsed")

(defun fsharp-ac--parse-current-file ()
  (when (fsharp-ac-can-make-request)
    (fsharp-ac-parse-current-buffer))
  ; Perform some emergency fixup if things got out of sync
  (when (not ac-completing)
    (setq fsharp-ac-status 'idle)))

(defun fsharp-ac-line-column-to-pos (line col)
  (save-excursion
    (goto-char (point-min))
    (forward-line (- line 1))
    (if (< (point-max) (+ (point) col))
        (point-max)
      (forward-char col)
      (point))))

(defun fsharp-ac-parse-errors (data)
  "Extract the errors from the given process response. Returns a list of fsharp-error."
  (save-match-data
    (let (parsed)
      (dolist (err data parsed)
        (let ((beg (fsharp-ac-line-column-to-pos (gethash "StartLineAlternate" err)
                                                 (gethash "StartColumn" err)))
              (end (fsharp-ac-line-column-to-pos (gethash "EndLineAlternate" err)
                                                 (gethash "EndColumn" err)))
              (face (if (string= "Error" (gethash "Severity" err))
                        'fsharp-error-face
                      'fsharp-warning-face))
              (msg (gethash "Message" err))
              (file (gethash "FileName" err))
              )
          (add-to-list 'parsed (make-fsharp-error :start beg
                                                  :end   end
                                                  :face  face
                                                  :text  msg
                                                  :file  file)))))))

(defun fsharp-ac/show-error-overlay (err)
  "Draw overlays in the current buffer to represent fsharp-error ERR."
  ;; Three cases
  ;; 1. No overlays here yet: make it
  ;; 2. new warning, exists error: do nothing
  ;; 3. new error exists warning: rm warning and make it
  (let* ((beg  (fsharp-error-start err))
         (end  (fsharp-error-end err))
         (face (fsharp-error-face err))
         (txt  (fsharp-error-text err))
         (file (fsharp-error-file err))
         (ofaces (mapcar (lambda (o) (overlay-get o 'face))
                         (overlays-in beg end)))
         )
    (unless (or (not (string= (file-truename buffer-file-name)
                              (file-truename file)))
             (and (eq face 'fsharp-warning-face)
                 (memq 'fsharp-error-face ofaces)))

      (when (and (eq face 'fsharp-error-face)
                 (memq 'fsharp-warning-face ofaces))
        (remove-overlays beg end 'face 'fsharp-warning-face))

      (let ((ov (make-overlay beg end)))
        (overlay-put ov 'face face)
        (overlay-put ov 'help-echo txt)))))

(defun fsharp-ac-clear-errors ()
  (interactive)
  (remove-overlays nil nil 'face 'fsharp-error-face)
  (remove-overlays nil nil 'face 'fsharp-warning-face)
  (setq fsharp-ac-errors nil))

;;; ----------------------------------------------------------------------------
;;; Error navigation
;;;
;;; These functions hook into Emacs' error navigation API and should not
;;; be called directly by users.

(defun fsharp-ac-message-safely (format-string &rest args)
  "Calls MESSAGE only if it is desirable to do so."
  (when (equal major-mode 'fsharp-mode)
    (unless (or (active-minibuffer-window) cursor-in-echo-area)
      (apply 'message format-string args))))

(defun fsharp-ac-error-position (n-steps errs)
  "Calculate the position of the next error to move to."
  (let* ((xs (->> (sort (-map 'fsharp-error-start errs) '<)
               (--remove (= (point) it))
               (--split-with (>= (point) it))))
         (before (nreverse (car xs)))
         (after  (cadr xs))
         (errs   (if (< n-steps 0) before after))
         (step   (- (abs n-steps) 1))
         )
    (nth step errs)))

(defun fsharp-ac/next-error (n-steps reset)
  "Move forward N-STEPS number of errors, possibly wrapping
around to the start of the buffer."
  (when reset
    (goto-char (point-min)))

  (let ((pos (fsharp-ac-error-position n-steps fsharp-ac-errors)))
    (if pos
        (goto-char pos)
      (error "No more F# errors"))))

(defun fsharp-ac-fsharp-overlay-p (ov)
  (let ((face (overlay-get ov 'face)))
    (or (equal 'fsharp-warning-face face)
        (equal 'fsharp-error-face face))))

(defun fsharp-ac/overlay-at (pos)
  (car-safe (-filter 'fsharp-ac-fsharp-overlay-p
                     (overlays-at pos))))

;;; HACK: show-error-at point checks last position of point to prevent
;;; corner-case interaction issues, e.g. when running `describe-key`
(defvar fsharp-ac-last-point nil)

(defun fsharp-ac/show-error-at-point ()
  (let ((ov (fsharp-ac/overlay-at (point)))
        (changed-pos (not (equal (point) fsharp-ac-last-point))))
    (setq fsharp-ac-last-point (point))

    (when (and ov changed-pos)
      (fsharp-ac-message-safely (overlay-get ov 'help-echo)))))

;;; ----------------------------------------------------------------------------
;;; Process handling
;;;
;;; Handle output from the completion process.

(defun fsharp-ac--get-msg (proc)
  (with-current-buffer (process-buffer proc)
    (goto-char (point-min))
    (let ((eofloc (search-forward "\n" nil t)))
      (when eofloc
        (when (numberp fsharp-ac-debug)
          (cond
           ((eq 1 fsharp-ac-debug)
            (fsharp-ac--log (format "%s ...\n" (buffer-substring (point-min) (min 100 eofloc)))))

           ((>= 2 fsharp-ac-debug)
            (fsharp-ac--log (format "%s\n" (buffer-substring (point-min) eofloc))))))

        (let ((json-array-type 'list)
              (json-object-type 'hash-table)
              (json-key-type 'string))
          (condition-case nil
              (progn
                (goto-char (point-min))
                (let ((msg (json-read)))
                  (delete-region (point-min) (+ (point) 1))
                  msg))
            (error
             (fsharp-ac--log (format "Malformed JSON: %s" (buffer-substring-no-properties (point-min) (point-max))))
             (message "Error: F# completion process produced malformed JSON"))))))))

(defun fsharp-ac-filter-output (proc str)
  "Filter output from the completion process and handle appropriately."
  (with-current-buffer (process-buffer proc)
    (save-excursion
      (goto-char (process-mark proc))
      (insert-before-markers str)))

  (let ((msg (fsharp-ac--get-msg proc)))
    (while msg
      (let ((kind (gethash "Kind" msg))
            (data (gethash "Data" msg)))
        (fsharp-ac--log (format "Received '%s' message of length %d\n"
                                kind
                                (hash-table-size msg)))
        (cond
         ((s-equals? "ERROR" kind) (fsharp-ac-handle-process-error data))
         ((s-equals? "INFO" kind) (when fsharp-ac-verbose (fsharp-ac-message-safely data)))
         ((s-equals? "completion" kind) (fsharp-ac-handle-completion data))
         ((s-equals? "helptext" kind) (fsharp-ac-handle-doctext data))
         ((s-equals? "errors" kind) (fsharp-ac-handle-errors data))
         ((s-equals? "project" kind) (fsharp-ac-handle-project data))
         ((s-equals? "tooltip" kind) (fsharp-ac-handle-tooltip data))
         ((s-equals? "finddecl" kind) (fsharp-ac-visit-definition data))
       (t
        (fsharp-ac-message-safely "Error: unrecognised message kind: '%s'" kind))))

    (setq msg (fsharp-ac--get-msg proc)))))

(defun fsharp-ac-handle-completion (data)
  (setq fsharp-ac-current-candidate (-map (lambda (s) (if (fsharp-ac--isNormalId s) s
                                                   (s-append "``" (s-prepend "``" s))))
                                          data)
        fsharp-ac-status 'acknowledged)
  (fsharp-ac--ac-start :force-init t)
  (ac-update)
  (setq fsharp-ac-status 'idle))

(defun fsharp-ac-handle-doctext (data)
  (maphash (lambda (k v) (puthash k v fsharp-ac-current-helptext)) data))

(defun fsharp-ac-visit-definition (data)
  (let* ((file (gethash "File" data))
         (line (gethash "Line" data))
         (col (gethash "Column" data)))
    (find-file file)
    (goto-char (fsharp-ac-line-column-to-pos line col))))

(defun fsharp-ac-handle-errors (data)
  "Display error overlays and set buffer-local error variables for error navigation."
  (when (equal major-mode 'fsharp-mode)
    (unless (or (active-minibuffer-window) cursor-in-echo-area)
      (fsharp-ac-clear-errors)
      (let ((errs (fsharp-ac-parse-errors data)))
        (setq fsharp-ac-errors errs)
        (mapc 'fsharp-ac/show-error-overlay errs)))))

(defun fsharp-ac-handle-tooltip (data)
  "Display information from the background process. If the user
has requested a popup tooltip, display a popup. Otherwise,
display a short summary in the minibuffer."
  ;; Do not display if the current buffer is not an fsharp buffer.
  (when (equal major-mode 'fsharp-mode)
    (unless (or (active-minibuffer-window) cursor-in-echo-area)
      (if fsharp-ac-awaiting-tooltip
          (progn
            (setq fsharp-ac-awaiting-tooltip nil)
            (if fsharp-ac-use-popup
                (fsharp-ac/show-popup data)
              (fsharp-ac/show-info-window data)))
        (fsharp-ac-message-safely (fsharp-doc/format-for-minibuffer data))))))

(defun fsharp-ac/show-popup (str)
  (if (display-graphic-p)
      (pos-tip-show str nil nil nil 300)
    ;; Use unoptimized calculation for popup, making it less likely to
    ;; wrap lines.
    (let ((popup-use-optimized-column-computation nil))
      (popup-tip str))))

(defconst fsharp-ac-info-buffer-name "*fsharp info*")

(defun fsharp-ac/show-info-window (str)
  (save-excursion
    (let ((help-window-select t))
      (with-help-window fsharp-ac-info-buffer-name
        (princ str)))))

(defun fsharp-ac-handle-project (data)
  (setq fsharp-ac-project-files (-map 'file-truename data))
  (fsharp-ac-parse-file (car (last fsharp-ac-project-files))))

(defun fsharp-ac-handle-process-error (str)
  (unless (s-matches? "Could not get type information" str)
    (fsharp-ac-message-safely str))
  (when (not (eq fsharp-ac-status 'idle))
    (setq fsharp-ac-status 'idle
          fsharp-ac-current-candidate nil)))

(provide 'fsharp-mode-completion)

;;; fsharp-mode-completion.el ends here
