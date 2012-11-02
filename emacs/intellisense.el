
;; edit path
(setq intellisense-wrapper "fsintellisense.exe")

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


; This function is called whenever fsintellisense.exe writes something on stdout
(defun filter (proc str)

  (when (buffer-live-p (process-buffer proc))
    (with-current-buffer (process-buffer proc)
      (let ((moving (= (point) (process-mark proc))))
        (save-excursion
          ;; Insert the text, advancing the process marker.
          (goto-char (process-mark proc))
          (insert str)
          (set-marker (process-mark proc) (point)))
        (if moving (goto-char (process-mark proc))))))
  
  ; cleanup hack
  (setq str (replace-regexp-in-string "<<EOF>>" "" str))
  (setq str (replace-regexp-in-string "DONE: Background parsing started" "" str))
  (setq str (replace-regexp-in-string "\n\n" "\n" str))

  (let ((help (split-string str "[\n]+" t)))

      (if (= mode 0)
          (or (eq str "\n") (eq str "\n") (eq str "")
              (th-show-tooltip-for-point str))

        (if (= mode 2)
            (show-doc str)

        (if esense-completion-list (esense-abort-completion))
        (unless (eq nil help)
          (let ((p (save-excursion (skip-syntax-backward "w_"))))
            (esense-start-completion (+ p (point)) help)))
          )))
)
