(require 'test-common)

;;; Jump to defn

(defconst finddeclstr1
  (let ((file (concat fs-file-dir "Program.fs")))
    (format "DATA: finddecl\nfile stored in metadata is '%s'\n%s:1:6\n<<EOF>>\n" file file))
  "A message for jumping to a definition in the same file")

(defconst finddeclstr2
  (let ((file (concat fs-file-dir "FileTwo.fs")))
    (format "DATA: finddecl\nfile stored in metadata is '%s'\n%s:12:11\n<<EOF>>\n" file file))
    "A message for jumping to a definition in the another file")

(check "jumping to local definition should not change buffer"
  (let ((f (concat fs-file-dir "Program.fs")))
    (stubbing-process-functions
     (using-file f
                 (fsharp-ac-filter-output nil finddeclstr1)
                 (should (equal f (buffer-file-name)))))))

(check "jumping to local definition should move point to definition"
  (stubbing-process-functions
   (using-file (concat fs-file-dir "Program.fs")
               (fsharp-ac-filter-output nil finddeclstr1)
               (should (equal (point) 18)))))

(check "jumping to definition in another file should open that file"
  (let ((f1 (concat fs-file-dir "Program.fs"))
        (f2 (concat fs-file-dir "FileTwo.fs")))
      (stubbing-process-functions
       (using-file f1
         (fsharp-ac-filter-output nil finddeclstr2)
         (should (equal (buffer-file-name) f2))))))

(check "jumping to definition in another file should move point to definition"
    (stubbing-process-functions
     (using-file (concat fs-file-dir "Program.fs")
       (fsharp-ac-filter-output nil finddeclstr2)
       (should (equal (point) 127)))))

;;; Error parsing

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

(check "parses errors from given string"
    (should= 3 (length (fsharp-ac-parse-errors err-brace-str))))

(defmacro check-filter (desc &rest body)
  "Test properties of filtered output from the ac-process."
  (declare (indent 1))
  `(check ,desc
     (stubbing-process-functions
      (find-file (concat fs-file-dir "Program.fs"))
        (fsharp-ac-filter-output nil err-brace-str))
      ,@body))

(check-filter "error clears partial data"
  (should (equal "" fsharp-ac-partial-data)))

(check-filter "errors cause overlays to be drawn"
  (should (equal 3 (length (overlays-in (point-min) (point-max))))))

(check-filter "error overlay has expected text"
  (let* ((ov (overlays-in (point-min) (point-max)))
         (text (overlay-get (car-safe ov) 'help-echo)))
    (should (equal text
                   (concat "Possible incorrect indentation: "
                           "this token is offside of context started at "
                           "position (2:16)."
                           "\nTry indenting this token further or using standard "
                           "formatting conventions.")))))

(check-filter "first overlay should have the warning face"
  (let* ((ov (overlays-in (point-min) (point-max)))
         (face (overlay-get (car ov) 'face)))
    (should (eq 'fsharp-warning-face face))))

(check-filter "second overlay should have the error face"
  (let* ((ov (overlays-in (point-min) (point-max)))
         (face (overlay-get (cadr ov) 'face)))
    (should (eq 'fsharp-error-face face))))

;;; Loading projects

(defmacro check-project-loading (desc exists &rest body)
  "Test fixture for loading projects, stubbing process-related functions."
  (declare (indent 2))
  `(check ,(concat "check project loading " desc)
     (stubbing-process-functions
      (flet ((fsharp-ac/start-process ())
             (process-send-string (proc cmd))
             (file-exists-p (_) ,exists))
        ,@body))))

(check-project-loading "returns nil if not fsproj"
    'exists
  (should-not (fsharp-ac/load-project "foo")))

(check-project-loading "returns nil if the given fsproj does not exist"
    nil ; doesn't exist
  (should-not (fsharp-ac/load-project "foo")))

(check-project-loading "returns the project path if loading succeeded"
    'exists
  (should-match "foo.fsproj" (fsharp-ac/load-project "foo.fsproj")))

;;; Process handling

(defmacro check-handler (desc &rest body)
  "Test fixture for process handler tests.
Stubs out functions that call on the ac process."
  (declare (indent 1))
  `(check ,(concat "process handler " desc)
     (setq major-mode 'fsharp-mode)
       (flet ((log-to-proc-buf (p s))
              (fsharp-ac-parse-current-buffer () t)
              (process-send-string   (p s))
              (fsharp-ac-can-make-request () t)
              (expand-file-name (x &rest _) x))
         ,@body)))

(defmacro stub-fn (sym var &rest body)
  "Stub the given unary function, with the argument to the
function bound to VAR in BODY. "
  (declare (indent 2))
  `(let (,var)
     (flet ((,sym (x &rest xs) (setq ,var x)))
       ,@body)))

(defun format-output (header &optional content)
  (concat header "\n" content
          fsharp-ac-eom))

(check-handler "prints message on error"
  (stub-fn message err
    (fsharp-ac-filter-output nil (format-output "ERROR: foo"))
    (should-match "foo" err)))

(check-handler "does not print message on type information error"
  (stub-fn message called
    (fsharp-ac-filter-output nil (format-output "ERROR: could not get type information"))
    (should-not called)))

;;; Tooltips and typesigs

(check-handler "uses popup in terminal if tooltip is requested"
  (let ((fsharp-ac-use-popup t))
    (flet ((display-graphic-p () nil))
      (stub-fn popup-tip tip
        (fsharp-ac/show-tooltip-at-point)
        (fsharp-ac-filter-output nil (format-output "DATA: tooltip" "foo"))
        (should-match "foo" tip)))))

(check-handler "uses pos-tip in GUI if tooltip is requested"
  (let ((fsharp-ac-use-popup t))
    (flet ((display-graphic-p () t))
      (stub-fn pos-tip-show tip
        (fsharp-ac/show-tooltip-at-point)
        (fsharp-ac-filter-output nil (format-output "DATA: tooltip" "foo"))
        (should-match "foo" tip)))))

(check-handler "does not show popup if typesig is requested"
  (let ((fsharp-ac-use-popup t))
    (stub-fn popup-tip called
      (fsharp-ac/show-typesig-at-point)
      (fsharp-ac-filter-output nil (format-output "DATA: tooltip" "foo"))
      (should-not called))))

(check-handler "does not show popup if use-popup is nil"
  (let ((fsharp-ac-use-popup nil))
    (stub-fn popup-tip called
      (fsharp-ac/show-tooltip-at-point)
      (fsharp-ac-filter-output nil (format-output "DATA: tooltip" "foo"))
      (should-not called))))

(check-handler "displays tooltip in info window if use-popup is nil"
  (let ((fsharp-ac-use-popup nil))
    ;; HACK: stub internals of with-help-window.
    ;; with-help-window is a macro and macrolet and labels don't seem to work.
    (stub-fn help-window-setup win
      (fsharp-ac/show-tooltip-at-point)
      (fsharp-ac-filter-output nil (format-output "DATA: tooltip" "foo"))
      (should-match "fsharp info" (buffer-name (window-buffer win))))))

(check-handler "displays typesig in minibuffer if typesig is requested"
  (stub-fn message sig
    (fsharp-ac/show-typesig-at-point)
    (fsharp-ac-filter-output nil (format-output "DATA: tooltip" "foo"))
    (should= "foo" sig)))
