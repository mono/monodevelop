(require 'ert)

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
        (ensure-packages '(pos-tip popup s dash))

        (push (expand-file-name "..") load-path)

        (push '("\\.fs[iylx]?$" . fsharp-mode) auto-mode-alist)
        (autoload 'fsharp-mode "fsharp-mode" "Major mode for editing F# code." t)
        (autoload 'turn-on-fsharp-doc-mode "fsharp-doc"))

       ((string= testmode "melpa") ; Install from MELPA
        (init-melpa)
        (ensure-packages '(fsharp-mode)))

       (t ; Assume `testmode` is a package file to install
          ; TODO: Break net dependency (pos-tip) for speed?
        (init-melpa)
        (ensure-packages '(pos-tip popup s dash))
        (package-install-file (expand-file-name testmode)))))))

(defun wait-for-condition (fun)
  "Wait up to 5 seconds for (fun) to return non-nil"
  (with-timeout (5)
    (while (not (funcall fun))
      (sleep-for 1))))

(defun fsharp-mode-wrapper (bufs body)
  "Load fsharp-mode and make sure any completion process is killed after test"
  (unwind-protect
      (progn (load-fsharp-mode)
             (funcall body))
    (sleep-for 1)
    (dolist (buf bufs)
      (when (get-buffer buf)
        (switch-to-buffer buf)
        (revert-buffer t t)
        (kill-buffer buf)))
    (mapc (lambda (buf)
            (when (member (buffer-file-name buf) fsharp-ac-project-files)
              (kill-buffer buf)))
          (buffer-list))
    (fsharp-ac/stop-process)
    (wait-for-condition (lambda () (not (fsharp-ac--process-live-p))))
    (when (fsharp-ac--process-live-p)
      (kill-process fsharp-ac-completion-process)
      (wait-for-condition (lambda () (not (fsharp-ac--process-live-p)))))
    (when (get-buffer "*fsharp-complete*")
      (kill-buffer "*fsharp-complete*"))))

(defun load-project-and-wait (file)
  (fsharp-ac/load-project file)
  (wait-for-condition (lambda () fsharp-ac-project-files)))

(ert-deftest check-project-files ()
  "Check the program files are set correctly"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "Test1/Program.fs")
     (wait-for-condition (lambda () fsharp-ac-project-files))
     (should-match "Test1/Program.fs" (s-join "" fsharp-ac-project-files))
     (should-match "Test1/FileTwo.fs" (s-join "" fsharp-ac-project-files)))))

(ert-deftest check-completion ()
  "Check completion-at-point works"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "Test1/Program.fs")
     (load-project-and-wait "Test1.fsproj")
     (log-psendstr fsharp-ac-completion-process "outputmode json")
     (search-forward "X.func")
     (delete-backward-char 2)
     (auto-complete)
     (ac-complete)
     (beginning-of-line)
     (should (search-forward "X.func")))))

(ert-deftest check-gotodefn ()
  "Check jump to definition works"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "Test1/Program.fs")
     (load-project-and-wait "Test1.fsproj")
     (search-forward "X.func")
     (backward-char 2)
     (fsharp-ac-parse-current-buffer t)
     (fsharp-ac/gotodefn-at-point)
     (wait-for-condition (lambda () (not (eq (point) 88))))
     (should= (point) 18))))

(ert-deftest check-tooltip ()
  "Check tooltip request works"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (let ((tiptext)
           (fsharp-ac-use-popup t))
       (noflet ((fsharp-ac/show-popup (s) (setq tiptext s)))
         (find-file "Test1/Program.fs")
         (load-project-and-wait "Test1.fsproj")
         (search-forward "X.func")
         (backward-char 2)
         (fsharp-ac-parse-current-buffer t)
         (fsharp-ac/show-tooltip-at-point)
         (wait-for-condition (lambda () tiptext))
         (should-match "val func : x:int -> int\n\nFull name: Program.X.func"
                       tiptext))))))

(ert-deftest check-errors ()
  "Check error underlining works"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "Test1/Program.fs")
     (load-project-and-wait "Test1.fsproj")
     (search-forward "X.func")
     (delete-backward-char 1)
     (backward-char)
     (fsharp-ac-parse-current-buffer t)
     (wait-for-condition (lambda () (> (length (overlays-at (point))) 0)))
     (should= (overlay-get (car (overlays-at (point))) 'face)
              'fsharp-error-face)
     (should= (overlay-get (car (overlays-at (point))) 'help-echo)
              "Unexpected keyword 'fun' in binding. Expected incomplete structured construct at or before this point or other token."))))

(ert-deftest check-script-tooltip ()
  "Check we can request a tooltip from a script"
  (fsharp-mode-wrapper '("Script.fsx")
   (lambda ()
     (let ((tiptext)
           (fsharp-ac-use-popup t))
       (noflet ((fsharp-ac/show-popup (s) (setq tiptext s)))
         (find-file "Test1/Script.fsx")
         (fsharp-ac-parse-current-buffer t)
         (search-forward "XA.fun")
         (fsharp-ac/show-tooltip-at-point)
         (wait-for-condition (lambda () tiptext))
         (should-match "val funky : x:int -> int\n\nFull name: Script.XA.funky"
                       tiptext))))))
