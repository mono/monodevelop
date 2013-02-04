(require 'ert)

(defun fsharp-mode-wrapper (body)
  "Load fsharp-mode and make sure any completion process is killed after test"
  (unwind-protect
      (progn (load-fsharp-mode)
             (funcall body))
    (ac-fsharp-quit-completion-process)
    (when (get-buffer "*fsharp-complete*")
      (kill-buffer "*fsharp-complete*"))))

(ert-deftest start-completion-process ()
  "Check that we can start the completion process and request help"
  (fsharp-mode-wrapper
   (lambda ()
     (let ((buf (find-file "Test1/Program.fs")))
       (ac-fsharp-launch-completion-process)
       (kill-buffer "Program.fs")
       (should (buffer-live-p (get-buffer "*fsharp-complete*")))
       (should (process-live-p (get-process "fsharp-complete")))
       (process-send-string "fsharp-complete" "help\n")
       (accept-process-output ac-fsharp-completion-process 1)
       (switch-to-buffer "*fsharp-complete*")
       (should (search-backward "trigger completion request" nil t))
       (kill-buffer buf)))))

(defconst waittime 2
  "Seconds to wait for data from background process")
(defconst sleeptime 5
  "Seconds to wait for data from background process")



(ert-deftest simple-runthrough ()
  "Just a quick run-through of the main features"
  (fsharp-mode-wrapper
   (lambda ()
     (find-file "Test1/Program.fs")
     (message "1")
     (ac-fsharp-load-project "Test1.fsproj")
     (message "2")
     (accept-process-output ac-fsharp-completion-process waittime)
     (message "3")
;     (sleep-for 5)
     (sleep-for sleeptime)
     (message "test message NOW")
     ;(message (mapconcat 'identity ac-fsharp-project-files ""))
     (should (and (string-match-p "Test1/Program.fs" (mapconcat 'identity ac-fsharp-project-files ""))
                  (string-match-p "Test1/FileTwo.fs" (mapconcat 'identity ac-fsharp-project-files ""))))
     (search-forward "X.func")
     (backward-char 2)
     (ac-fsharp-gotodefn-at-point)
     (accept-process-output ac-fsharp-completion-process waittime)
     (sleep-for sleeptime)
     (should (eq (point) 18))
     (search-forward "X.func")
     (delete-backward-char 2)
     (completion-at-point)
     (accept-process-output ac-fsharp-completion-process waittime)
     (sleep-for sleeptime)
     (beginning-of-line)
     (should (search-forward "X.func"))
     (beginning-of-line)
     (search-forward "X.func")
     (delete-backward-char 1)
     (backward-char)
     (ac-fsharp-get-errors)
     (accept-process-output ac-fsharp-completion-process waittime)
     (sleep-for sleeptime)
     (should (eq (length (overlays-at (point))) 1))
     (should (eq (overlay-get (car (overlays-at (point))) 'face)
                 'fsharp-error-face))
     (revert-buffer t t)
     (kill-buffer "Program.fs"))))
