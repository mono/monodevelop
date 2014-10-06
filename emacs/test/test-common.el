(require 'ert)
(require 'cl)

(defmacro check (desc &rest body)
  "Wrap ert-deftest with a simpler interface."
  (declare (indent 1))
  `(ert-deftest
       ,(intern (replace-regexp-in-string "[ .]" "-" desc)) ()
     (noflet ((message (&rest args)))
       ,@body)))

(defmacro using-file (path &rest body)
  "Open the file at PATH in a buffer, execute BODY forms, then kill the buffer."
  (declare (indent 1))
  `(save-excursion
     (find-file ,path)
     (unwind-protect
         (progn ,@body)
       (set-buffer-modified-p nil)
       (kill-this-buffer))))

(defmacro using-temp-file (name &rest body)
  "Create a temporary file that will be deleted after executing BODY forms"
  (declare (indent 1))
  `(using-file (concat temporary-file-directory (symbol-name (gensym)) ,name)
     ,@body))

(defmacro stubbing-process-functions (&rest body)
  `(noflet ((process-live-p (p) t)
            (fsharp-ac--process-live-p () t)
            (start-process (&rest args))
            (set-process-filter (&rest args))
            (set-process-query-on-exit-flag (&rest args))
            (process-send-string (&rest args))
            (process-buffer (proc) fsharp-ac--completion-bufname)
            (process-mark (proc) (point-max))
            (fsharp-ac-parse-current-buffer () t)
            (log-to-proc-buf (p s)))
     ,@body))

(defun should-match (regex str)
  (should (string-match-p regex str)))

(defun should= (x y)
  (should (equal x y)))

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

(defconst default-dependencies '(popup s dash pos-tip auto-complete noflet faceup))

(defun load-packages ()
  "Load package dependencies for fsharp-mode."
  (init-melpa)
  (mapc 'require-package default-dependencies))

(defun run-fsharp-tests (&optional files)
  "Configure the environment for running tests, then execute
tests. If FILES is specified, load each member of the list to
search for tests. If FILES is nil then use all files matching the
glob emacs/test/*test.el."
  (interactive)
  (let ((test-files (if files files fsharp-test-files)))
    (mapc 'load-file test-files)
    (if noninteractive
        (ert-run-tests-batch-and-exit)
      (ert-run-tests-interactively t))))

(defconst fsharp-test-files
  (directory-files
   (file-name-directory (or load-file-name buffer-file-name))
   t
   ".*tests\.el")
  "All the files ending in 'tests.el' in the emacs/test directory.")

(defun run-fsharp-unit-tests ()
  (interactive)
  (configure-fsharp-tests)
  (run-fsharp-tests
   (--remove (s-match "integration-tests.el" it) fsharp-test-files)))

(defun run-fsharp-integration-tests ()
  (interactive)
  (configure-fsharp-tests)
  (run-fsharp-tests   
   (--filter (s-match "integration-tests.el" it) fsharp-test-files)))

;;; Configuration

(defun configure-fsharp-tests ()
  (init-melpa)
  (let ((var (getenv "TESTMODE")))
    (cond
     ((null var)          (test-configuration-default))
     ((equal var "melpa") (test-configuration-melpa))
     (t                   (test-configuration-package-file var)))))

(defun test-configuration-default ()
  (load-packages)
  (setq load-path tests-load-path)
  (mapc 'load-file src-files)
  (require 'fsharp-mode))

(defun test-configuration-melpa ()
  (require-package 'fsharp-mode))

(defun test-configuration-package-file (pkg)
  (load-packages)
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

;; Local Variables:
;; byte-compile-warnings: (not cl-functions)
;; End:
