(require 'ert)
(require 'cl)

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

;;; Dirs

(defconst test-dir
  (file-name-directory (or load-file-name (buffer-file-name)))
  "The directory containing unit-tests.")

(defconst src-dir
  (expand-file-name (concat test-dir ".."))
  "The directory containing the elisp files under test.")

(defconst fs-file-dir
  (concat test-dir "Test1/")
  "The directory containing F# source files for testing.")

;;; Files

(defun loadable-el-file-p (x)
  "Match elisp files, except package spec files."
  (and (equal "el" (file-name-extension x))
       (not (string-match-p "^[#.]+" (file-name-nondirectory x)))
       (not (string-match-p "-pkg" x))))

(defun loadable-el-files (dir)
  (remove-if-not 'loadable-el-file-p (directory-files dir t)))

(defconst src-files (loadable-el-files src-dir))

;;; ----------------------------------------------------------------------------
;;; Test runner functions

(defconst tests-load-path
  (mapcar 'expand-file-name `(,@load-path "." ".." "./tests")))

(defconst default-dependencies '(namespaces pos-tip))

(defun run-fsharp-tests ()
  "Configure the environment for running tests, then execute tests.
When run interactively, this will run all current ert tests.
When running tests in batch mode, tests should be loaded as -l arguments to emacs."
  (interactive)
  (configure-fsharp-tests)
  (mapc 'load-file src-files)
  (if noninteractive
      (ert-run-tests-batch-and-exit)
    (ert-run-tests-interactively t)))

;;; Configuration

(defun configure-fsharp-tests ()
  (init-melpa)
  (let ((var (getenv "TESTMODE")))
    (cond
     ((null var)          (test-configuration-default))
     ((equal var "melpa") (test-configuration-melpa))
     (t                   (test-configuration-package-file var)))))

(defun test-configuration-default ()
  (mapc 'require-package default-dependencies)
  (setq load-path tests-load-path)
  (require 'fsharp-mode))

(defun test-configuration-melpa ()
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

(provide 'test-common)
