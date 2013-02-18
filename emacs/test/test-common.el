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

(defmacro using-temp-file (name &rest body)
  "Create a temporary file that will be deleted after executing BODY forms"
  (declare (indent 1))
  `(using-file (concat temporary-file-directory (symbol-name (gensym)) ,name)
     ,@body))

(defun should-match (regex str)
  (should (string-match-p regex str)))

;;; ----------------------------------------------------------------------------
;;; Test runner functions

(defconst tests-load-path
  (mapcar 'expand-file-name `(,@load-path "." ".." "./tests")))

(defconst default-dependencies '(namespaces pos-tip))

(defun run-fsharp-tests ()
  "Configure the environment for running tests, then execute tests."
  (interactive)
  (message "Running tests with mode: %s" (getenv "TESTMODE"))
  (let ((var (getenv "TESTMODE")))
    (cond
     ((null var)          (test-configuration-default))
     ((equal var "melpa") (test-configuration-melpa))
     (t                   (test-configuration-package-file var))))
  (if noninteractive
      (ert-run-tests-batch-and-exit)
    (ert-run-tests-interactively t)))

(defun test-configuration-default ()
  (init-melpa)
  (mapc 'require-package default-dependencies)
  (setq load-path tests-load-path)
  (require 'fsharp-mode))

(defun test-configuration-melpa ()
  (init-melpa)
  (require-package 'fsharp-mode))

(defun test-configuration-package-file (pkg)
  (mapc 'require-package default-dependencies)
  (package-install-file (expand-file-name pkg)))

(defun init-melpa ()
  (setq package-archives '(("melpa" . "http://melpa.milkbox.net/packages/")))
  (package-initialize)
  (unless package-archive-contents
    (package-refresh-contents)))

(defun require-package (pkg)
  (unless (package-installed-p pkg)
    (package-install pkg))
  (require pkg))

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
