(require 'ert)
(require 'test-utilities)


(ert-deftest change-to-mode-fs ()
  "Check that loading a .fs file causes us to change to fsharp-mode"
  (load-fsharp-mode)
  (find-file "Test1/FileTwo.fs")
  (should (eq major-mode 'fsharp-mode)))
