;;; Test registration of auto modes.

(defmacro check-automode (extension)
  `(check ,(concat "uses fsharp-mode for " extension " files")
     (using-temp-file ,extension
       (should (eq major-mode 'fsharp-mode)))))

(check-automode ".fs")
(check-automode ".fsx")
(check-automode ".fsi")
(check-automode ".fsl")

;;; Test our ability to find SLN files and projects.
;;; This is tricky to test comprehensively because there is a sln at the
;;; root of this repo.

(check "should not find fsharp project if none present"
  (should-not (fsharp-mode/find-sln-or-fsproj "/bin/")))

(check "should find sln at base of repo given a subdir"
  (should-match "AutoComplete.sln$" (fsharp-mode/find-sln-or-fsproj test-dir)))

(check "should find sln at base of repo given a file in subdir"
  (should-match "AutoComplete.sln$"
                (fsharp-mode/find-sln-or-fsproj (concat test-dir "file.fs"))))
