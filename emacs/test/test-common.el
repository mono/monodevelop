(require 'ert)

(defmacro check (desc &rest body)
  "Wrap ert-deftest with a simpler interface."
  (declare (indent 1))
  `(ert-deftest
       ,(intern (replace-regexp-in-string "[ .]" "-" desc)) ()
     ,@body))

(defmacro using-file (path &rest body)
  "Open the file at PATH in a buffer, execute BODY forms, then kill the buffer."
  (declare (indent 1))
  `(save-excursion
     (find-file ,path)
     (unwind-protect
         (progn ,@body)
       (kill-buffer))))

(defun should-match (regex str)
  (should (string-match-p regex str)))

;;; ----------------------------------------------------------------------------

(defun init-melpa ()
  (setq package-archives
        '(("melpa"       . "http://melpa.milkbox.net/packages/")))
  (package-initialize)
  (unless package-archive-contents
    (package-refresh-contents)))

(defun ensure-packages (packages)
  (dolist (package packages)
    (unless (package-installed-p package)
      (package-install package))))

(defun load-fsharp-mode ()
  (unless (functionp 'fsharp-mode)
    (let ((testmode (getenv "TESTMODE")))
      (cond
       ((eq testmode nil) ; Load from current checkout
        (init-melpa)
        (ensure-packages '(pos-tip namespaces))

        (push (expand-file-name "..") load-path)

        (push '("\\.fs[iylx]?$" . fsharp-mode) auto-mode-alist)
        (autoload 'fsharp-mode "fsharp-mode" "Major mode for editing F# code." t)
        (autoload 'run-fsharp "inf-fsharp-mode" "Run an inferior F# process." t)
        (autoload 'turn-on-fsharp-doc-mode "fsharp-doc")
        (autoload 'ac-fsharp-launch-completion-process "fsharp-mode-completion" "Launch the completion process" t)
        (autoload 'ac-fsharp-quit-completion-process "fsharp-mode-completion" "Quit the completion process" t)
        (autoload 'ac-fsharp-load-project "fsharp-mode-completion" "Load the specified F# project" t))

       ((string= testmode "melpa") ; Install from MELPA
        (init-melpa)
        (ensure-packages '(fsharp-mode)))

       (t ; Assume `testmode` is a package file to install
          ; TODO: Break net dependency (pos-tip) for speed?
        (init-melpa)
        (ensure-packages '(pos-tip namespaces))
        (package-install-file (expand-file-name testmode)))))))

(defun fsharp-mode-wrapper (bufs body)
  "Load fsharp-mode and make sure any completion process is killed after test"
  (unwind-protect
      (progn (load-fsharp-mode)
             (funcall body))

    ;; ;; This seems to be more than long enough for the process to run
    ;; ;; successfully.
    ; (sleep-for 0.1)

    (ac-fsharp-quit-completion-process)
    (dolist (buf bufs)
      (when (get-buffer buf)
        (switch-to-buffer buf)
        (revert-buffer t t)
        (kill-buffer buf)))
    (when (get-buffer "*fsharp-complete*")
      (kill-buffer "*fsharp-complete*"))))

(provide 'test-common)
