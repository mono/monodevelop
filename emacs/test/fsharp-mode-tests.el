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

(check "should find fsproj in test project directory"
  (should-match "Test1.fsproj"
                (fsharp-mode/find-sln-or-fsproj fs-file-dir)))

(check "should prefer sln to fsproj"
  (should-match "bar.sln"
                (fsharp-mode/find-sln-or-fsproj (concat test-dir
                                                        "FindSlnData/"))))

(check "should find closest sln"
  (should-match "foo.sln"
                (fsharp-mode/find-sln-or-fsproj (concat test-dir
                                                        "FindSlnData/"
                                                        "sln/"))))

(check "should find sln in parent dir"
  (should-match "bar.sln"
                (fsharp-mode/find-sln-or-fsproj (concat test-dir
                                                        "FindSlnData/"
                                                        "noproj/"
                                                        "test.fs"))))

