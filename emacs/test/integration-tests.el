
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
        (autoload 'turn-on-fsharp-doc-mode "fsharp-doc"))

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
    (sleep-for 1)
    (dolist (buf bufs)
      (when (get-buffer buf)
        (switch-to-buffer buf)
        (revert-buffer t t)
        (kill-buffer buf)))
    (fsharp-mode-completion/stop-process)
    (when (get-buffer "*fsharp-complete*")
      (kill-buffer "*fsharp-complete*"))))

(require 'ert)

(defconst waittime 5
  "Seconds to wait for data from background process")
(defconst sleeptime 1
  "Seconds to wait for data from background process")


(ert-deftest check-project-files ()
  "Check the program files are set correctly"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "Test1/Program.fs")
     (accept-process-output ac-fsharp-completion-process waittime)
     (with-timeout (waittime)
       (while (null ac-fsharp-project-files)
         (sleep-for 1)))
     (should (string-match-p "Test1/Program.fs" (mapconcat 'identity ac-fsharp-project-files "")))
     (should (string-match-p "Test1/FileTwo.fs" (mapconcat 'identity ac-fsharp-project-files ""))))))


(ert-deftest check-completion ()
  "Check completion-at-point works"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "Test1/Program.fs")
     (fsharp-mode-completion/load-project "Test1.fsproj")
     ;(sleep-for sleeptime)
     (search-forward "X.func")
     (delete-backward-char 2)
     (completion-at-point)
     (accept-process-output ac-fsharp-completion-process waittime)
     (beginning-of-line)
     (should (search-forward "X.func")))))


(ert-deftest check-gotodefn ()
  "Check jump to definition works"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "Test1/Program.fs")
     (fsharp-mode-completion/load-project "Test1.fsproj")
     (while (eq nil ac-fsharp-project-files)
       (sleep-for 1))
     (search-forward "X.func")
     (backward-char 2)
     (call-process "sleep" nil nil nil "3")
     (ac-fsharp-gotodefn-at-point)
     (while (eq (point) 88)
       (sleep-for 1))
     (should (eq (point) 18)))))

(ert-deftest check-tooltip ()
  "Check tooltip request works"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (let ((tiptext)
           (ac-fsharp-use-popup t))
       (flet ((popup-tip (s) (setq tiptext s)))
         (find-file "Test1/Program.fs")
         (fsharp-mode-completion/load-project "Test1.fsproj")
         (while (eq nil ac-fsharp-project-files)
           (sleep-for 1))
         (search-forward "X.func")
         (backward-char 2)
         (call-process "sleep" nil nil nil "3")
         (fsharp-mode-completion/show-tooltip-at-point)
         (with-timeout (5)
           (while (eq nil tiptext)
             (sleep-for 1)))
         (should
          (string-match-p "val func : x:int -> int\n\nFull name: Program.X.func"
                          tiptext)))))))

(ert-deftest check-errors ()
  "Check error underlining works"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "Test1/Program.fs")
     (fsharp-mode-completion/load-project "Test1.fsproj")
     (while (eq nil ac-fsharp-project-files)
       (sleep-for 1))
     (search-forward "X.func")
     (delete-backward-char 1)
     (backward-char)
     (call-process "sleep" nil nil nil "3")
     (in-ns fsharp-mode-completion (_ request-errors))
     (while (eq (length (overlays-at (point))) 0)
       (sleep-for 1))
     (should (eq (overlay-get (car (overlays-at (point))) 'face)
                 'fsharp-error-face))
     (should (string= (overlay-get (car (overlays-at (point))) 'help-echo)
                      "Unexpected keyword 'fun' in binding. Expected incomplete structured construct at or before this point or other token.")))))

(ert-deftest check-script-tooltip ()
  "Check we can request a tooltip from a script"
  (fsharp-mode-wrapper '("Script.fsx")
   (lambda ()
     (let ((tiptext)
           (ac-fsharp-use-popup t))
       (flet ((popup-tip (s) (setq tiptext s)))
         (find-file "Test1/Script.fsx")
         (ac-fsharp-parse-current-buffer)
         (call-process "sleep" nil nil nil "3")
         (search-forward "XA.fun")
         (fsharp-mode-completion/show-tooltip-at-point)
         (with-timeout (waittime)
           (while (null tiptext)
             (accept-process-output ac-fsharp-completion-process sleeptime)))
         (should (stringp tiptext))
         (should
          (string-match-p "val funky : x:int -> int\n\nFull name: Script.XA.funky"
                          tiptext)))))))
