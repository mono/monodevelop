(require 'ert)

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

