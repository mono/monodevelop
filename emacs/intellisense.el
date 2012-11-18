

;; edit path
(setq ac-fsharp-complete-executable
      (concat (file-name-directory (or load-file-name buffer-file-name))
              "../bin/fsautocomplete.sh"))

;; few bindings for the tests
(global-set-key (kbd "<f1>") 'tool-tip)
(global-set-key (kbd "<f2>") 'fsharp-complete)
(global-set-key (kbd "<f3>") 'reparse)
(global-set-key (kbd "<f4>") 'check-error)

;; there are two completions ui: frame or window
;(setq esense-completion-display-method 'frame)
(setq esense-completion-display-method 'window)

(defadvice process-send-string
  (before echo-process-send-string (proc str))

  (save-selected-window
    (select-window (get-buffer-window (process-buffer proc)))
    (goto-char (point-max))
    (insert str)
    (set-marker (process-mark proc) (point))))
  
;(ad-activate 'process-send-string)
;(ad-deactivate 'process-send-string)

(defun start-intellisense ()
  (interactive)
  (esense-mode t)
  (setq mode 0)
  (setq proc (start-process "fsharp-intellisense" "intelli-buffer"
                            intellisense-wrapper))
  (set-process-filter proc 'filter)     
  (process-send-string proc "script c:\\foo.fsx\n")
  (send-buffer nil)
)

(defun stop-intellisense ()
  (interactive)
  (kill-process "fsharp-intellisense"))

(defun send-script ()
  (interactive)
  (process-send-string proc "script c:\\foo.fsx\n")) ;; filename is unused for the moment

;; send the buffer to update intellisense-wrapper
(defun send-buffer (with-command)
  (process-send-string proc (concat "parse\n"
                                    (buffer-string)
                                    "\n<<EOF>>\n")))

(defun reparse ()
  (interactive)
  (setq mode 0)
  (send-buffer t))

(defun tool-tip ()
  (interactive)
  (reparse)
  (let ((li (1- (line-number-at-pos (point))))
        (col (current-column)))
    (process-send-string proc
                         (concat "tip " (number-to-string li) " " (number-to-string col) "\n"))
    (setq mode 0)
    ))

(defun check-error ()
  (interactive)
  (reparse)
  (process-send-string proc "errors\n")
  (setq mode 0))

(defun fsharp-complete ()
  (interactive)
  (reparse)
  (let ((li (1- (line-number-at-pos (point))))
        (col (current-column)))
    (process-send-string proc
                         (concat "completion " (number-to-string li) " " (number-to-string col) "\n"))
    (setq mode 1)
;    (print (concat "completion " (number-to-string li) " " (number-to-string col) "\n"))
    ))

(defun show-doc (doc)
  (if (not doc)
      (message (concat "There is no documentation for " item))

    (if (eq esense-completion-display-method 'window)
        (select-window (get-buffer-window esense-buffer-name))
      (select-frame esense-completion-frame))

    (let ((point-pos (unless esense-xemacs (esense-point-position))))
      (esense-show-tooltip-for-point doc (car point-pos) (cdr point-pos)))

;;     (if (eq esense-completion-display-method 'window)
;;         (select-window orig-window)
;;       (select-frame orig-frame))
)
)

(defun doc (str)
  (let* (
         (p esense-completion-symbol-beginning-position)
         (col (- p (save-excursion (beginning-of-line) (point))))
         (li (1- (line-number-at-pos p))))
;; get tooltip for the current completion item - buggy
;;      (process-send-string proc
;;                           (concat "tipCompletion " (number-to-string li) " " (number-to-string col) " " str "\n"))
     (setq mode 0)
    nil
    ))


;;;;;;;;;;;;
;; New development (adaptation of autocomplete-clang-async) below here

;;;
;;; Helper functions
;;;

(defsubst ac-fsharp-create-position-string (pos)
  (save-excursion
    (goto-char pos)
    (format "%d %d"
            (line-number-at-pos)
            (1+ (- (point) (line-beginning-position))))))

(defconst ac-fsharp-completion-pattern
  "^\\(%s[^\s\n:]*\\)")

(defun ac-fsharp-parse-output (prefix)
  (goto-char (point-min))
  (let ((pattern (format ac-fsharp-completion-pattern
                         (regexp-quote prefix)))
        match lines)
    (while (re-search-forward prefix)
      (setq match (match-string-no-properties 1))
      (push match lines))
    lines)
  )

  
  ;; (let ((pattern (format ac-fsharp-completion-pattern
  ;;                        (regexp-quote prefix)))
  ;;       lines match detailed-info
  ;;       (prev-match ""))
  ;;   (while (re-search-forward pattern nil t)
  ;;     (setq match (match-string-no-properties 1))
  ;;     (unless (string= "Pattern" match)
  ;;       (setq detailed-info (match-string-no-properties 2))

  ;;       (if (string= match prev-match)
  ;;           (progn
  ;;             (when detailed-info
  ;;               (setq match (propertize match
  ;;                                       'ac-fsharp-help
  ;;                                       (concat
  ;;                                        (get-text-property 0 'ac-fsharp-help (car lines))
  ;;                                        "\n"
  ;;                                        detailed-info)))
  ;;               (setf (car lines) match)
  ;;               ))
  ;;         (setq prev-match match)
  ;;         (when detailed-info
  ;;           (setq match (propertize match 'ac-fsharp-help detailed-info)))
  ;;         (push match lines))))
  ;;   lines))

;;;
;;; Async stuff
;;;

(defvar ac-fsharp-status 'idle)
(defvar ac-fsharp-current-candidate nil)
(defvar ac-fsharp-completion-process nil)
(defvar ac-fsharp-saved-prefix "")

(make-variable-buffer-local 'ac-fsharp-status)
(make-variable-buffer-local 'ac-fsharp-current-candidate)
(make-variable-buffer-local 'ac-fsharp-completion-process)

;;;
;;; Functions to speak with the clang-complete process
;;;

(defun ac-fsharp-send-script-file (proc)
  (message "Sending script file")
  (save-restriction
    (widen)
    (process-send-string proc (format "script %s\n" (buffer-file-name)))
    (process-send-string proc (buffer-substring-no-properties (point-min) (point-max)))
    (process-send-string proc "\n<<EOF>>\n")))

(defun ac-fsharp-send-reparse-request (proc)
  (message "Sending reparse request")
  (save-restriction
    (widen)
    (process-send-string proc "parse\n")
    (process-send-string proc (buffer-substring-no-properties (point-min) (point-max)))

    (process-send-string proc "\n<<EOF>>\n")))

(defun ac-fsharp-send-completion-request (proc)
  (message (format "Sending completion request for: %s" ac-prefix))
  (save-restriction
    (widen)
    (process-send-string proc "completion")
    (process-send-string proc (ac-fsharp-create-position-string (- (point) (length ac-prefix))))
    (process-send-string proc "60 \n")))

(defun ac-fsharp-send-shutdown-command (proc)
  (message "sending shut down")
  (if (eq (process-status "fsharp-complete") 'run)
    (process-send-string proc "quit\n"))
  )


(defun ac-fsharp-append-process-output-to-process-buffer (process output)
  "Append process output to the process buffer."
  (with-current-buffer (process-buffer process)
    (save-excursion
      ;; Insert the text, advancing the process marker.
      (goto-char (process-mark process))
      (insert output)
      (set-marker (process-mark process) (point)))
    (goto-char (process-mark process))))

(defun ac-fsharp-shutdown-process ()
  (interactive)
  (message "shutting down process")
  (if ac-fsharp-completion-process
      (ac-fsharp-send-shutdown-command ac-fsharp-completion-process)))

(defun ac-fsharp-reparse-buffer ()
  (if ac-fsharp-completion-process
      (ac-fsharp-send-reparse-request ac-fsharp-completion-process)))

(defun ac-fsharp-async-preemptive ()
  (interactive)
  (self-insert-command 1)
  (if (eq ac-fsharp-status 'idle)
      (ac-start)
    (setq ac-fsharp-status 'preempted)))

(defun ac-fsharp-launch-completion-process ()
  (interactive)
  (message "Launching completion process")
  (setq ac-fsharp-completion-process
        (let ((process-connection-type nil))
          (start-process "fsharp-complete"
                         "*fsharp-complete*"
                         ac-fsharp-complete-executable)))

  (set-process-filter ac-fsharp-completion-process 'ac-fsharp-filter-output)
  (set-process-query-on-exit-flag ac-fsharp-completion-process nil)
  ;; Pre-parse source code.
  (ac-fsharp-send-script-file ac-fsharp-completion-process)

  (add-hook 'kill-buffer-hook 'ac-fsharp-shutdown-process nil t)
  (add-hook 'before-save-hook 'ac-fsharp-reparse-buffer)

  (local-set-key (kbd ".") 'ac-fsharp-async-preemptive))

;;
;;  Receive server responses (completion candidates) and fire auto-complete
;;
(defun ac-fsharp-parse-completion-results (proc)
  (with-current-buffer (process-buffer proc)
    (ac-fsharp-parse-output ac-fsharp-saved-prefix)))

(defun ac-fsharp-filter-output (proc string)
  (ac-fsharp-append-process-output-to-process-buffer proc string)
  (if (string= (substring string -7 nil) "<<EOF>>")
      (case ac-fsharp-status
        (preempted
         (setq ac-fsharp-status 'idle)
         (ac-start)
         (ac-update))
        
        (otherwise
         (setq ac-fsharp-current-candidate (ac-fsharp-parse-completion-results proc))
         (message "ac-fsharp results arrived")
         (setq ac-fsharp-status 'acknowledged)
         (ac-start :force-init t)
         (ac-update)
         (setq ac-fsharp-status 'idle)))))

(defun ac-fsharp-candidate ()
  (case ac-fsharp-status
    (idle
     (message "ac-fsharp-candidate triggered - fetching candidates...")
     (setq ac-fsharp-saved-prefix ac-prefix)

     ;; NOTE: although auto-complete would filter the result for us, but when there's
     ;;       a HUGE number of candidates avaliable it would cause auto-complete to
     ;;       block. So we filter it uncompletely here, then let auto-complete filter
     ;;       the rest later, this would ease the feeling of being "stalled" at some degree.

     (message "saved prefix: %s" ac-fsharp-saved-prefix)
     (with-current-buffer (process-buffer ac-fsharp-completion-process)
       (erase-buffer))
     (setq ac-fsharp-status 'wait)
     (setq ac-fsharp-current-candidate nil)

     ;; send completion request
     (ac-fsharp-send-completion-request ac-fsharp-completion-process)
     ac-fsharp-current-candidate)

    (wait
     (message "ac-fsharp-candidate triggered - wait")
     ac-fsharp-current-candidate)

    (acknowledged
     (message "ac-fsharp-candidate triggered - ack")
     (setq ac-fsharp-status 'idle)
     ac-fsharp-current-candidate)

    (preempted
     (message "fsharp-async is preempted by a critical request")
     nil)))


; This function is called whenever fsintellisense.exe writes something on stdout
;; (defun ac-fsharp-filter-output (proc str)

;;   (when (buffer-live-p (process-buffer proc))
;;     (with-current-buffer (process-buffer proc)
;;       (let ((moving (= (point) (process-mark proc))))
;;         (save-excursion
;;           ;; Insert the text, advancing the process marker.
;;           (goto-char (process-mark proc))
;;           (insert str)
;;           (set-marker (process-mark proc) (point)))
;;         (if moving (goto-char (process-mark proc))))))
  
;;   ; cleanup hack
;;   (setq str (replace-regexp-in-string "<<EOF>>" "" str))
;;   (setq str (replace-regexp-in-string "DONE: Background parsing started" "" str))
;;   (setq str (replace-regexp-in-string "\n\n" "\n" str))

;;   (let ((help (split-string str "[\n]+" t)))

;;       (if (= mode 0)
;;           (or (eq str "\n") (eq str "\n") (eq str "")
;;               (th-show-tooltip-for-point str))

;;         (if (= mode 2)
;;             (show-doc str)

;;         (if esense-completion-list (esense-abort-completion))
;;         (unless (eq nil help)
;;           (let ((p (save-excursion (skip-syntax-backward "w_"))))
;;             (esense-start-completion (+ p (point)) help)))
;;           )))
;; )

; Autocomplete source for fsintellisense
(defvar ac-source-fsintellisense
  '((candidates . ac-fsharp-candidate)))



(add-hook 'fsharp-mode (lambda () (setq ac-sources '(ac-source-fsintellisense))))
(add-to-list 'ac-modes 'fsharp-mode)
