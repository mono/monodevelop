(defmacro check-automode (extension)
  `(check ,(concat "uses fsharp-mode for " extension " files")
     (using-temp-file ,extension
       (should (eq major-mode 'fsharp-mode)))))

(check-automode ".fs")
(check-automode ".fsx")
(check-automode ".fsi")
(check-automode ".fsl")
