(require 'ert)
(require 'test-utilities)

(defconst finddeclstr1
  (let ((file (expand-file-name "Test1/Program.fs")))
    (format "DATA: finddecl\nfile stored in metadata is '%s'\n%s:1:6\n<<EOF>>\n" file file))
    "A message for jumping to a definition in the same file")

(ert-deftest jump-to-defn-same-file ()
  "Check that we can jump to a definition in same file"
  (load-fsharp-mode)
  (find-file "Test1/Program.fs")
  (ac-fsharp-filter-output nil finddeclstr1)
  (should (string= (buffer-name) "Program.fs"))
  (should (eq (point) 18))
  (kill-buffer "Program.fs"))

(defconst finddeclstr2
  (let ((file (expand-file-name "Test1/FileTwo.fs")))
    (format "DATA: finddecl\nfile stored in metadata is '%s'\n%s:12:11\n<<EOF>>\n" file file))
    "A message for jumping to a definition in the another file")

(ert-deftest jump-to-defn-another-file ()
  "Check that we can jump to a definition in another file"
  (load-fsharp-mode)
  (find-file "Test1/Program.fs")
  (ac-fsharp-filter-output nil finddeclstr2)
  (should (string= (buffer-name) "FileTwo.fs"))
  (should (eq (point) 127))
  (kill-buffer "Program.fs")
  (kill-buffer "FileTwo.fs"))

;; (ert-deftest jump-to-defn-another-project ()
;;   "Check that we can jump to a definition an imported project"
;;   (load-fsharp-mode)
;;   (should (string= "write this test" "now"))
;;   )

(defconst err-brace-str
  (mapconcat
     'identity
     '("DATA: errors"
       "[9:0-9:2] WARNING Possible incorrect indentation: this token is offside of context started at position (2:16)."
       "Try indenting this token further or using standard formatting conventions."
       "[11:0-11:2] ERROR Unexpected symbol '[<' in expression"
       "Followed by more stuff on this line"
       "[12:0-12:3] WARNING Possible incorrect indentation: this token is offside of context started at position (2:16).
Try indenting this token further or using standard formatting conventions."
       "<<EOF>>"
       "")
     "\n")
  "A list of errors containing a square bracket to check the parsing")

(ert-deftest error-message-containing-brace ()
  "Check that a errors containing a brace and newlines is parsed correctly"
  (fsharp-mode-wrapper
   '("Program.fs")
   (lambda ()
     (find-file "Test1/Program.fs")
     (ac-fsharp-filter-output nil err-brace-str)
     (should (string= "" ac-fsharp-partial-data))
     (should (eq 3 (length (overlays-in (point-min) (point-max)))))
     (should (string= (overlay-get (car (overlays-in (point-min) (point-max))) 'help-echo) "Possible incorrect indentation: this token is offside of context started at position (2:16).\nTry indenting this token further or using standard formatting conventions."))
     (should (eq 'fsharp-error-face (overlay-get (cadr (overlays-in (point-min) (point-max))) 'face)))
     (should (eq 'fsharp-warning-face (overlay-get (car (overlays-in (point-min) (point-max))) 'face))))))
