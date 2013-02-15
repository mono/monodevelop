(namespace fsharp-doc
  :export
  [ format-for-minibuffer ]
  :use
  [(fsharp-mode-completion ac-fsharp-tooltip-at-point)])

(defvar fsharp-doc-idle-delay 0.5
  "The number of seconds to wait for input idle before showing a tooltip.")

(define-minor-mode fsharp-doc-mode
  "Display F# documentation in the minibuffer."
  nil
  " Doc"
  nil
  ;; Body
  (in-ns fsharp-doc
    (_ reset-timer)
    (when fsharp-doc-mode
      (_ start-timer)
      (run-hooks 'fsharp-doc-mode-hook))
    (message "fsharp-doc-mode %s" (if fsharp-doc-mode "enabled" "disabled"))
    fsharp-doc-mode))

(defun turn-on-fsharp-doc-mode ()
  (fsharp-doc-mode t))

(defun turn-off-fsharp-doc-mode ()
  (fsharp-doc-mode nil))

;;; -----------------------------------------------------------------------------

(defmutable timer nil)

(defn start-timer ()
  (unless (@ timer)
    (@set timer (run-with-idle-timer fsharp-doc-idle-delay t (~ show-tooltip)))))

(defn reset-timer ()
  (when (@ timer)
    (cancel-timer (@ timer))
    (@set timer nil)))

;;; ----------------------------------------------------------------------------

(defn format-for-minibuffer (str)
  "Parse the result from the F# process."
  (destructuring-bind (x &rest xs) (split-string str "[\r\n]")
    (let ((line (if (string-match-p "^Multiple" x) (car-safe xs) x))
          (name (_ extract-full-name str)))
      (_ tidy-result
         (if name
             (_ replace-identifier line name)
           line)))))

(defn extract-full-name (str)
  (string-match "Full name: \\(.*\\)$" str)
  (match-string 1 str))

(defn replace-identifier (str fullname)
  (replace-regexp-in-string
   "^\\w+ \\(.*?\\) " fullname str 'fixcase "\1" 1))

(defn tidy-result (str)
  (replace-regexp-in-string "[ ]*=[ ]*" "" str))

;;; ----------------------------------------------------------------------------

(defn show-tooltip ()
  "Show tooltip info in the minibuffer."
  (interactive)
  (when (and fsharp-doc-mode
             (not (eobp))
             (not (eolp))
             (not executing-kbd-macro)
             (not (eq (selected-window) (minibuffer-window))))
    (ac-fsharp-tooltip-at-point)))
