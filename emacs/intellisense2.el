
;; edit path
(setq ac-fsharp-complete-executable
      (concat (file-name-directory (or load-file-name buffer-file-name))
              "../bin/fsautocomplete.sh"))

(defvar ac-fsharp-status 'idle)
(defvar ac-fsharp-current-candidate nil)
(defvar ac-fsharp-completion-process nil)
(defvar ac-fsharp-saved-prefix "")
(defvar ac-fsharp-partial-data "")

(defun log-to-proc-buf (proc str)
  (when (buffer-live-p (process-buffer proc))
    (with-current-buffer (process-buffer proc)
      ;; Insert the text, advancing the process marker.
      (goto-char (process-mark proc))
      (insert str)
      (set-marker (process-mark proc) (point))
      (goto-char (process-mark proc)))))

(defun log-psendstr (proc str)
  (log-to-proc-buf proc str)
  (process-send-string proc str))

(defun ac-fsharp-send-script-file (proc)
  (message "Sending script file")
  (save-restriction
    (widen)
    (log-psendstr
     proc
     (format "script %s\n%s\n<<EOF>>\n"
             (buffer-file-name)
             (buffer-substring-no-properties (point-min) (point-max))))))

(defun ac-fsharp-reparse-script-file (proc)
  (message "Reparsing script file")
  (save-restriction
    (widen)
    (log-psendstr
     proc
     (format "parse full\n%s\n<<EOF>>\n"
             (buffer-substring-no-properties (point-min) (point-max))))))

(defun ac-fsharp-send-completion-request (proc)
  (let ((request (format "completion %d %d\n"
                               (- (line-number-at-pos) 1) (current-column))))
    (message (format "Sending completion request for: '%s' of '%s'" ac-prefix request))
    (save-restriction
      (widen)
      (ac-fsharp-reparse-script-file proc)
      (log-psendstr proc request))))


(defun ac-fsharp-send-shutdown-command (proc)
  (message "sending shut down")
  (log-psendstr proc "quit\n"))


(defun fsharp-completion-shutdown ()
  (interactive)
  (ac-fsharp-send-shutdown-command (get-process "fsharp-complete")))

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

  ;(add-hook 'kill-buffer-hook 'ac-fsharp-shutdown-process nil t)
  ;(add-hook 'before-save-hook 'ac-fsharp-reparse-buffer)

  ;(local-set-key (kbd ".") 'ac-fsharp-async-preemptive))
  )

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
     ;(with-current-buffer (process-buffer ac-fsharp-completion-process)
     ;  (erase-buffer))
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


(defun ac-fsharp-stash-partial (str)
  (setq ac-fsharp-partial-data (concat ac-fsharp-partial-data str)))


; This function is called whenever fsintellisense.exe writes something on stdout
(defun ac-fsharp-filter-output (proc str)

  (log-to-proc-buf proc str)

  (ac-fsharp-stash-partial str)

  (if (string= (substring string -7 nil) "<<EOF>>")
      (case ac-fsharp-status
        (idle
         (message "Received output when idle, ignored")
         (setq ac-fsharp-partial-data ""))
        
        (otherwise
         (setq str ac-fsharp-partial-data)
         (setq ac-fsharp-partial-data "")
         (setq str (replace-regexp-in-string "<<EOF>>" "" str))
         (setq str (replace-regexp-in-string "DONE: Background parsing started" "" str))
         (setq str (replace-regexp-in-string "\n\n" "\n" str))
         
         (let ((help (split-string str "[\n]+" t)))
           (message "ac-fsharp-filter-output setting current candidate")
           (setq ac-fsharp-current-candidate help)
           (setq ac-fsharp-status 'acknowledged)
           (ac-start :force-init t)
           (ac-update)
           (setq ac-fsharp-status 'idle))))))

(defun ac-fsharp-candidate2 ()
  (list "lol" "biscuits" "trontastic" "trance" "biscuil"))

(defvar ac-source-fsintellisense
  '((candidates . ac-fsharp-candidate)))

(defun ac-fsharp-config ()
  (setq ac-sources '(ac-source-fsintellisense))
  (setq ac-use-fuzzy nil))

(add-hook 'fsharp-mode-hook 'ac-fsharp-config)
(add-hook 'fsharp-mode-hook
          (lambda () (setq ac-auto-start nil)))
;(add-hook 'fsharp-mode-hook (lambda () (auto-complete-mode)))
;(setq fsharp-mode-hook '())

(defun ac-complete-fsintellisense ()
  (interactive)
  (auto-complete '(ac-source-fsintellisense)))

(global-set-key (kbd "C-c .") 'ac-complete-fsintellisense)

(defun attempt-completion ()
  (interactive)
  (ac-start))
