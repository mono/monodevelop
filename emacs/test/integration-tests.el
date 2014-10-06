(require 'ert)

(defun wait-for-condition (fun)
  "Wait up to 5 seconds for (fun) to return non-nil"
  (with-timeout (5)
    (while (not (funcall fun))
      (sleep-for 1))))

(defun fsharp-mode-wrapper (bufs body)
  "Load fsharp-mode and make sure any completion process is killed after test"
  (unwind-protect
      ; Run the actual test
      (funcall body)

    ; Clean up below

    ; Close any buffer requested by the test
    (dolist (buf bufs)
      (when (get-buffer buf)
        (switch-to-buffer buf)
        (when (file-exists-p buffer-file-name)
          (revert-buffer t t))
        (kill-buffer buf)))

    ; Close any buffer associated with the loaded project
    (mapc (lambda (buf)
            (when (member (buffer-file-name buf) fsharp-ac-project-files)
              (switch-to-buffer buf)
              (revert-buffer t t)
              (kill-buffer buf)))
          (buffer-list))

    ; Stop the fsautocomplete process and close its buffer
    (fsharp-ac/stop-process)
    (wait-for-condition (lambda () (not (fsharp-ac--process-live-p))))
    (when (fsharp-ac--process-live-p)
      (kill-process fsharp-ac-completion-process)
      (wait-for-condition (lambda () (not (fsharp-ac--process-live-p)))))
    (when (get-buffer "*fsharp-complete*")
      (kill-buffer "*fsharp-complete*"))

    ; Kill the FSI process and buffer, if it was used
    (let ((inf-fsharp-process (get-process inferior-fsharp-buffer-subname)))
      (when inf-fsharp-process
        (when (process-live-p inf-fsharp-process)
          (kill-process inf-fsharp-process)
          (wait-for-condition (lambda () (not (process-live-p
                                          inf-fsharp-process)))))))))

(defun load-project-and-wait (file)
  (fsharp-ac/load-project file)
  (wait-for-condition (lambda () fsharp-ac-project-files)))

(ert-deftest check-project-files ()
  "Check the program files are set correctly"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "test/Test1/Program.fs")
     (wait-for-condition (lambda () fsharp-ac-project-files))
     (should-match "Test1/Program.fs" (s-join "" fsharp-ac-project-files))
     (should-match "Test1/FileTwo.fs" (s-join "" fsharp-ac-project-files))
     (should-match "Test1/bin/Debug/Test1.exe" fsharp-ac--output-file))))

(ert-deftest check-completion ()
  "Check completion-at-point works"
  (fsharp-mode-wrapper '("Program.fs")
   (lambda ()
     (find-file "test/Test1/Program.fs")
     (load-project-and-wait "Test1.fsproj")
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
     (find-file "test/Test1/Program.fs")
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
         (find-file "test/Test1/Program.fs")
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
     (find-file "test/Test1/Program.fs")
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
         (find-file "test/Test1/Script.fsx")
         (fsharp-ac-parse-current-buffer t)
         (search-forward "XA.fun")
         (fsharp-ac/show-tooltip-at-point)
         (wait-for-condition (lambda () tiptext))
         (should-match "val funky : x:int -> int\n\nFull name: Script.XA.funky"
                       tiptext))))))

(ert-deftest check-inf-fsharp ()
  "Check that FSI can be used to evaluate"
  (fsharp-mode-wrapper '("tmp.fsx")
   (lambda ()
     (fsharp-run-process-if-needed inferior-fsharp-program)
     (wait-for-condition (lambda () (get-buffer inferior-fsharp-buffer-name)))
     (find-file "tmp.fsx")
     (goto-char (point-max))
     (insert "let myvariable = 123 + 456")
     (fsharp-eval-phrase)
     (switch-to-buffer inferior-fsharp-buffer-name)
     (wait-for-condition (lambda () (search-backward "579" nil t)))
     (should-match "579" (buffer-substring-no-properties (point-min) (point-max))))))
