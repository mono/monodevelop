(defmacro check-process-with-file (desc file &rest body)
  "Check properties of the completion process when loading the given file."
  (declare (indent defun))
  `(check ,desc
     ;; Perform test with live completion process.
     (unwind-protect
         (using-file ,file
           (ac-fsharp-launch-completion-process)
           ,@body)
       ;; Kill process.
       (ignore-errors
         (ac-fsharp-quit-completion-process)
         (kill-buffer "*fsharp-complete*")))))

(defmacro* check-process (desc &rest body)
  (declare (indent defun))
  `(check-process-with-file ,desc ,fsharp-file1
     ,@body))

(defmacro* check-project (desc &rest body)
  "Check behaviours of loaded F# projects"
  (declare (indent defun))
  `(check-process ,desc
     (ac-fsharp-load-project fsharp-proj)
     ,@body))

(defconst process-wait-time 0.2
  "Seconds to wait for data from background process")

(defconst fsharp-file1 (concat fs-file-dir "Program.fs"))

(defconst fsharp-file2 (concat fs-file-dir "FileTwo.fs"))

(defconst fsharp-proj  (concat fs-file-dir "Test1.fsproj"))

(defconst fsharp-script-file (concat fs-file-dir "Script.fsx"))


;;; ----------------------------------------------------------------------------

(defun await-process-response ()
  (with-timeout (process-wait-time)
    (while (string= (buffer-string) "")
      (accept-process-output ac-fsharp-completion-process))))

;;; Process

(check-process "creates *fsharp-complete* buffer"
  (should (buffer-live-p (get-buffer "*fsharp-complete*"))))

(check-process "completion process is running"
  (should (process-live-p ac-fsharp-completion-process)))

(check-process "should receive help on request"
  (process-send-string ac-fsharp-completion-process "help\n")
  (switch-to-buffer "*fsharp-complete*")
  (await-process-response)
  (should-match "trigger completion request" (buffer-string)))

;;; Projects

(defun await-project-files ()
  (accept-process-output ac-fsharp-completion-process process-wait-time)
  (while (eq nil ac-fsharp-project-files)
    (sleep-for 0.1)))

(check-project "should contain Program.fs"
  (await-project-files)
  (should-match "Program.fs" (concat ac-fsharp-project-files)))

(check-project "should contain FileTwo.fs"
  (await-project-files)
  (should-match "FileTwo.fs" (concat ac-fsharp-project-files)))

(check-project "should be able to use completion-at-point"
  (search-forward "X.func")
  (delete-char -2)
  (completion-at-point)
  (accept-process-output ac-fsharp-completion-process process-wait-time)
  (beginning-of-line)
  (should-match (rx "X.func") (buffer-string)))

(check-project "can jump to definition"
  (await-project-files)
  (search-forward "X.func")
  (backward-char 2)
  (call-process "sleep" nil nil nil "3")
  (ac-fsharp-gotodefn-at-point)
  (while (eq (point) 88)
    (sleep-for 1))
  (should (eq (point) 18)))

(check-project "can request tooltips"
  (let    ((tiptext)
           (ac-fsharp-use-pos-tip t))
    (flet ((pos-tip-show (s) (setq tiptext s)))
      (await-project-files)

      (search-forward "X.func")
      (backward-char 2)
      (call-process "sleep" nil nil nil "3")

      ;; Request tooltip
      (ac-fsharp-tooltip-at-point)
      (with-timeout (5)
        (while (null tiptext)
          (sleep-for 0.1)))

      (should-match
       tiptext
       "val func : x:int -> int\n\nFull name: Program.X.func"))))

;;; Errors

(defun await-errors ()
  (call-process "sleep" nil nil nil "3")
  (ac-fsharp-get-errors)
  (while (eq (length (overlays-at (point))) 0)
    (sleep-for 0.1)))

(check-project "error should have expected message"
  (await-project-files)
  (search-forward "X.func")
  (delete-char -1)
  (backward-char)
  (await-errors)
  (should (equal (overlay-get (car (overlays-at (point))) 'face )
                 (concat "Unexpected keyword 'fun' in binding. "
                         "Expected incomplete structured construct at or "
                         "before this point or other token."))))

(check-project "error should be underlined"
  (await-project-files)
  (search-forward "X.func")
  (delete-char -1)
  (backward-char)
  (await-errors)
  (should (eq (overlay-get (car (overlays-at (point))) 'face)
              'fsharp-error-face)))

;;; Scripts

(check-process-with-file "can request a tooltip from a script" fsharp-script-file
  (let    ((tiptext)
           (ac-fsharp-use-pos-tip t))
    (flet ((pos-tip-show (s) (setq tiptext s)))
      (await-project-files)

      (search-forward "XA.fun")
      (backward-char 2)
      (call-process "sleep" nil nil nil "3")

      ;; Request tooltip
      (ac-fsharp-tooltip-at-point)
      (with-timeout (5)
        (while (null tiptext)
          (sleep-for 0.1)))

      (should-match
       "val funky : x:int -> int\n\nFull name: Script.XA.funky"
       tiptext))))
