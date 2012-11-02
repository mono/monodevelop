;; esense.el - Erlang "IntelliSense" for Emacs
;;
;; Copyright (C) 2006  Tamas Patrovics
;;
;; This file is free software; you can redistribute it and/or modify
;; it under the terms of the GNU General Public License as published by
;; the Free Software Foundation; either version 2, or (at your option)
;; any later version.
;;
;; This file is distributed in the hope that it will be useful,
;; but WITHOUT ANY WARRANTY; without even the implied warranty of
;; MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;; GNU General Public License for more details.
;;
;; You should have received a copy of the GNU General Public License
;; along with GNU Emacs; see the file COPYING.  If not, write to
;; the Free Software Foundation, Inc., 59 Temple Place - Suite 330,
;; Boston, MA 02111-1307, USA.
;;
;;
;; Commentary:
;;
;;
;; ESense (ErlangSense) is a minor mode which provides features
;; similar to IntelliSense or CodeSense in other editors. It can be
;; enabled in an Erlang buffer or in the Erlang Shell buffer.
;;
;;
;; Configuration:
;; --------------
;;
;; The most important variables are `esense-index-directory' (which
;; does not need to be set if you use the default directory when
;; generating index files), `esense-module-search-directories',
;; `esense-include-search-directories' and `esense-indexer-program'.
;;
;; See other configuration variables in the "User Configuration"
;; section below.
;;
;;
;; Activation:
;; -----------
;;
;; In your .emacs add to erlang-load-hook:
;;
;;   (require 'esense)
;;
;; You can also set ESense-related configuration variables here:
;;
;;   (setq esense-indexer-program
;;      (concat (getenv "HOME") "/path/to/esense.sh"))
;;
;; Add to erlang-mode-hook
;;
;;   (esense-mode)
;;
;;
;; How it works:
;; -------------
;;
;; ESense uses a single activation key (by default F1) which does
;; different things depending on the context:
;;
;; If the cursor is ON a symbol it shows the documentation of the
;; symbol in a tooltip.
;;
;; If the cursor is at the end of a symbol it tries to complete the
;; symbol.
;;
;; If the cursor is in a parameter list and the two conditions above
;; are not met then it shows the documentation of the entity the
;; parameter list belongs to in a tooltip.
;;
;; If the modifier CTRL is also pressed then in contexts where
;; normally documentation is shown the definition of the symbol is
;; visited instead. The original location is put onto the tag stack
;; of etags, so M-* can be used to return to the starting location.
;;
;; ALT+<activation key> jumps to an arbitrary module/function with
;; completion.
;;
;; If the cursor is on an -include line the corresponding header file
;; is visited.
;;
;;
;; Code:
;;

(eval-when-compile (require 'cl))
(require 'etags)
(unless (featurep 'xemacs)
  (require 'tooltip))

;----------------------------------------------------------------------
;
; User configuration
;

(defgroup esense nil
  "ESense configuration"
  :group 'convenience)

(defcustom esense-index-directory "$HOME/.esense"
  "The generated index files are in this directory.

This variable can also be a list of directories in which case a given
index file is searched for in all the listed directories. On-demand
generated index files will be put into the first directory of the list.

It is useful if one needs to switch frequently between different
software versions (e.g. OTP versions) which naturally need different
index information. It can be done easily by simply pointing this variable
to a different set of index directories and invoking `esense-initialize'."
  :type 'directory
  :group 'esense)

(defcustom esense-module-search-directories nil
  "List of directories to search for module files if no
index file is found. These directories are searched only if the given
source file cannot be found via `esense-index-directory'.

Note that the directories can contain wildcard patterns, so more than
one directory can be specified with a single directory pattern.

The current directory is always searched first implicitly."
  :type '(repeat directory)
  :group 'esense)

(defcustom esense-include-search-directories nil
  "List of directories to search for include files if no
index file is found. These directories are searched only if the given
source file cannot be found via `esense-index-directory'.

Note that the directories can contain wildcard patterns, so more than
one directory can be specified with a single directory pattern.

The current directory is always searched first implicitly."
  :type '(repeat directory)
  :group 'esense)

(defcustom esense-setup-otp-search-directories nil
  "If t then search directories for OTP sources are set up
automatically. `esense-module-search-directories' and
`esense-include-search-directories' take prefrence if they are given.

The setup is done only once when the package is loaded.

This option has an effect only if `erlang-root-dir' is set properly.")

(defcustom esense-setup-otp-html-search-directories nil
  "If t then search directories for OTP HTML documentation are set up
automatically. Currently HTML documentation is searched only if this
option is set, so `esense-module-search-directories' has no effect for
HTML docs.

The setup is done only once when the package is loaded.

This option has an effect only if `erlang-root-dir' is set properly.")

(defcustom esense-indexer-program "esense.sh"
  "Path of the program for creating index files."
  :type 'string
  :group 'esense)

(defcustom esense-erlang-interpreter-program "erl"
  "Path of the program for running the Erlang interpreter.
If you plan to use option `esense-find-function-matching-invocation-pattern'
then make sure it points to at least the R10B version of the interpreter,
otherwise finding the matching function will not work."
  :type 'string
  :group 'esense)

(defcustom esense-completion-display-method 'window
  "The completion list can be displayed in a new window or in a new frame.
The value can be either the symbol `frame' or `window'."
  :type '(choice :tag "Cycle through..."
                 (const :tag "Window" window)
                 (const :tag "Frame" frame))
  :group 'esense)

(defcustom esense-electric-commands nil
  "Controls whether electric commands (automatic completion)
during typing are enabled."
  :type 'boolean
  :group 'esense)

(defcustom esense-ignore-unknown-includes t
  "If set to t no error is signalled when unindexed include files are
encountered. A warning is shown instead and the unknown files are ignored."
  :type 'boolean
  :group 'esense)

(defcustom esense-show-number-of-function-arguments nil
  "If set to t the number of arguments of the function the point is on is
shown in the echo area when Emacs is idle.
This option needs to be set before ESense is loaded, otherwise the
idle timer will not be started."
  :type 'boolean
  :group 'esense)

(defcustom esense-preload-index-information-when-idle nil
  "The number of idle seconds after which index information for
to the current file is loaded automatically, so that it's readily
available when needed.
This option needs to be set before ESense is loaded, otherwise the
idle timer will not be started."
      :type '(choice integer
                     (const nil))
      :group 'esense)

(defcustom esense-find-function-matching-invocation-pattern nil
  "If set to t an attempt is made to find the function which matches the
actual invocation pattern when jumping to the definition, instead of
jumping to the first function clause with the matching arity.

This option is useful in trace buffers where the invocation arguments of functions
are fully expanded."
  :type 'boolean
  :group 'esense)

(defcustom esense-resolve-macros nil
  "If set to t then macros are resolved first and the resolved value is acted
upon. if the value is the symbol `ask' the user is asked what to do."
  :type '(choice boolean
                 (const ask))
  :group 'esense)

(if (eq window-system 'w32)
    (defcustom esense-titlebar-height 30
      "Height of Emacs window titlebar. It mostly depends on your window
manager settings. Correct titlebar height will help ESense to display
popup windows in a proper position."
      :type 'integer
      :group 'esense)

  (defconst esense-titlebar-height 0
    "On Linux the title bar is not the part of the window, so we
don't have to consider its height in calculations."))


(if (featurep 'xemacs)
    (progn
      (setq esense-tooltip-face 'default)
      (copy-face 'bold 'esense-tooltip-header-face))

  (setq esense-tooltip-face 'tooltip)
  (defface esense-tooltip-header-face
    '((((class color))
       :inherit bold)
      (t
       :inherit bold))
    "Face for section headers in ESense tooltips."
    :group 'esense))

;----------------------------------------------------------------------
;
; Initialization
;

(if esense-setup-otp-search-directories
    (setq esense-include-search-directories
          (append esense-include-search-directories
                  (list (concat erlang-root-dir "/lib/*/include")))))

(setq esense-otp-module-search-directories
      (if esense-setup-otp-search-directories
          (list (concat erlang-root-dir "/lib/*/src"))))

(setq esense-otp-html-module-search-directories
      (if esense-setup-otp-html-search-directories
          (list (concat erlang-root-dir "/lib/*/doc/html"))))

;----------------------------------------------------------------------

(defvar esense-version "1.12"
  "Full string version number of ESense mode.")


(defvar esense-buffer-name "*esense*"
  "Temporary working buffer (for loading index files, etc.).")

(defvar esense-secondary-buffer-name "*esense2*"
  "Temporary working buffer used when the main working buffer is busy.")


(defconst esense-value-begin-end-tag 30
  "ASCII character code indicating the begin and end of a multi line
value in the index files.")

(defvar esense-max-tooltip-line-length 70
  "The maximum length of lines in a tooltip window. Lines longer than that are wrapped.")

(defvar esense-max-tooltip-lines 25
  "The maximum number of lines shown in a tooltip.
The tooltip is truncated if necessary.")

(defconst esense-path-separator-in-file-name "!"
  "Separator string instead of / for file names.")

(defconst esense-module-index-directory "modules"
  "Subdirectory of module index files within `esense-index-directory'.")

(defconst esense-include-index-directory "includes"
  "Subdirectory of include index files within `esense-index-directory'.")

(defconst esense-include-regexp
  "^\\s-*-\\(include\\(_lib\\)?\\)(\\s-*\"\\(.*\\)\"\\s-*)\\."
  "Regular expression for matching an include directive.")

(defconst esense-module-regexp
  "^\\s-*-module(\\(.*\\))\\."
  "Regular expression for matching a module directive.")

(defconst esense-macro-regexp "^-define('?%s'?\\s-*\\((\\|,\\)"
  "Regular expression for matching a macro definition.
Before use %s should be substituted for the macro name ")


;; I know, I know. F1 shouldn't be bound by a minor mode,
;; but I hate long C-c ... key sequences
(defvar esense-minor-mode-map
  (let ((map (make-sparse-keymap)))
     (define-key map (kbd ".") 'esense-electric-character)
     map))


(defun foo () (error "fail!"))

(defvar esense-minor-mode-completion-map
   (let ((map (make-sparse-keymap)))
     (define-key map (kbd "<up>") 'esense-completion-previous-item)
     (define-key map (kbd "<down>") 'esense-completion-next-item)
     (define-key map (kbd "<prior>") 'esense-completion-previous-page)
     (define-key map (kbd "<next>") 'esense-completion-next-page)
     (define-key map (kbd "<RET>") 'esense-completion-insert-item)
     (define-key map "\C-i" 'esense-completion-insert-item)
     (define-key map (kbd ".") 'esense-dot-validation)
     (define-key map (kbd "(") 'esense-completion-self-insert-item)
     (define-key map (kbd "<ESC>") 'esense-abort-completion)
     map))


(defvar esense-completion-symbol-beginning-position nil
   "If a completion is in progress this is the character position of the
beginning of the symbol being completed.")

(defvar esense-completion-list nil
  "If a completion is in progress this is the list of possible completions,
otherwise it's nil.")

(defvar esense-completion-inserter-function nil
  "The function which is called to insert a completion into the buffer.")

(defvar esense-completion-popup-documentation-function nil
  "The function which is called to get popup documentation for the currently
selected completion item.")

(defvar esense-completion-module-info nil
   "Name of the module used in current completion or nil if no module
name was given.")

(defvar esense-completion-record-info-list nil
   "List of information on records used in current completion.")

;; FIXME: Is there a nicer solution for this?
(defvar esense-completion-startup nil
   "If non-nil it indicates that the completion process is just started,
so the post-command-hook should ignore the current command.")

(defvar esense-completion-frame nil
   "If `esense-completion-display-method' is `frame' then this variable
contains the id of the completion frame.")

(defvar esense-completion-saved-window-configuration nil
   "If `esense-completion-display-method' is `window' then this variable
contains the saved configuration of windows before the completion started.")

(defvar esense-timer nil
   "The value of an active timer or nil if no timer is active")

(defvar esense-function-arg-timer nil
   "The value of the timer for showing the number of function arguments
when Emacs idle.")

(defvar esense-preload-timer nil
   "The value of the timer for preloading index information for the current
file.")


(defvar esense-cache nil
  "List containing the most recent entries read from index files.")

(defvar esense-max-cache-size 50
  "The maximum number of items held in the cache.")


(defvar esense-xemacs (eval-when-compile (featurep 'xemacs))
  "Non-nil if it's XEmacs.")


(defvar esense-interpreter-process nil
  "Process object of the Erlang interpreter subprocess.")

(defvar esense-interpreter-buffer "*esense-interpreter*"
  "Name of the buffer associated with the Erlang interpreter
subprocess.")


(defvar esense-cached-includes nil
  "If non-nil then it's a list of (LAST-MODIFICATION . INCLUDE-INFO)
where LAST-MODIFICATION is indicator of the state of the buffer after
the last modification and INCLUDE-INFO is included file information
pertaining to the current buffer.

ESense functions will use the cached include information for the
current buffer if the buffer was not modified since the information was cached.")

(make-variable-buffer-local 'esense-cached-includes)


(defstruct esense-erlang
  "Structure describing the contents of an Erlang file."
  ;; name of the module or nil if it's an include file
  modulename
  ;; path of the source file
  source
  ;; list of functions
  functions
  ;; list of records
  records
  ;; list of macros
  macros
  ;; list of included files
  includes
  ;; list of imports
  imports)


(defstruct esense-function
  "Structure describing a function."
  name
  arity
  %% function clause specification with argument names
  spec
  ;; the line in the source include file where the function is defined
  line
  doc
  docref
  params
  exported)


(defstruct esense-record
  "Structure describing the contents of a record."
  ;; record name
  name
  ;; the line in the source include file where the record is defined
  line
  ;; documentation of the record
  doc
  ;; list of fields in the record
  fields)


(defstruct esense-record-field
  "Structure describing a record field."
  ;; field name
  name
  ;; documentation
  doc)


(defstruct esense-macro
  "Structure describing a macro."
  ;; macro name
  name
  ;; for simple values it is the actual value of the macro, for
  ;; more complex types which cannot be displayed easily it is nil
  value)


(defstruct esense-import
  "Structure describing imported functions from a module."
  ;; name of the module
  module
  ;; import functions from the module
  functions)


(defstruct esense-imported-function
  "Structure describing an imported function."
  name
  arity)


;; error definitions

(put 'esense-error
     'error-conditions
     '(error esense-error))


;; predefined macros

(defconst esense-predefined-macros
  (mapcar (lambda (descriptor)
            (let ((macro (make-esense-macro)))
              (setf (esense-macro-name macro) (car descriptor))
              (setf (esense-macro-value macro) (cdr descriptor))
              macro))

          '(("MODULE" . "The name of the current module.")
            ("MODULE_STRING" . "The name of the current module, as a string.")
            ("FILE" . "The file name of the current module.")
            ("LINE" . "The current line number.")
            ("MACHINE" . "The machine name."))))

;----------------------------------------------------------------------
;
; XEmacs compatibility
;

(when esense-xemacs
  (unless (fboundp 'match-string-no-properties)
    (defalias 'match-string-no-properties 'match-string))
  (defalias 'frame-char-width 'frame-width)
  (defalias 'frame-char-height 'frame-height)

  (unless (fboundp 'propertize)
    (defun propertize (string &rest props)
      string)))

;----------------------------------------------------------------------

(easy-mmode-define-minor-mode
  esense-mode
  "ErlangSense mode."
  nil
  " ESense"
  esense-minor-mode-map

  (esense-maybe-initialize)

  (if esense-show-number-of-function-arguments
      (unless esense-function-arg-timer
        (setq esense-function-arg-timer
              (run-with-idle-timer 0.5 t
                                   'esense-show-number-of-function-arguments))))

  (if esense-preload-index-information-when-idle
      (unless esense-preload-timer
        (setq esense-preload-timer
              (run-with-idle-timer
               esense-preload-index-information-when-idle t
               'esense-load-index-information-for-current-file))))
  )


(defun esense-maybe-initialize ()
  "Call `esense-initialize' if necessary."
  (unless (boundp 'esense-modules)
    (esense-initialize)))


(defun esense-initialize ()
  "Read module and include file information from the index directory/ies."
  (interactive)

  (message "Rebuilding indexes...")

  (setq esense-cache nil)
  (setq esense-modules nil)
  (setq esense-include-files nil)

  ;; is there a simpler way to do this?
  (dolist (buffer (buffer-list))
    (with-current-buffer buffer
      (kill-local-variable 'esense-cached-includes)))

  (dolist (directory (if (listp esense-index-directory)
                         esense-index-directory
                       (list esense-index-directory)))
    (destructuring-bind
        (modules-info includes-info)
        (esense-initialize-directory directory)
      (setq esense-modules (nunion esense-modules
                                   modules-info
                                   :test 'equal))
      (setq esense-include-files (nunion esense-include-files
                                         includes-info
                                         :test 'equal))))

  (message "Done."))


(defun esense-initialize-directory (directory)
  "Read module and include file information from the given DIRECTORY
and return it as a list (MODULES-INFO INCLUDES-INFO)."
  (mapcar (lambda (index-info)
            (destructuring-bind (subdir transformer) index-info
              (if (file-readable-p subdir)
                (let ((files '()))
                  (dolist (file (directory-files subdir))
                    (unless (or (equal file ".")
                                (equal file ".."))
                      (push (if transformer
                                (funcall transformer file)
                              file)
                            files)))
                  files)

                (message "Skipping unreadable directory %s" subdir)
                nil)))

          (let ((expanded-directory (esense-expand-file-name directory)))
            `((,(concat expanded-directory "/" esense-module-index-directory)
               nil)
              (,(concat expanded-directory "/" esense-include-index-directory)
               esense-include-index-file-transformer)))))


(defun esense-do-something-at-point ()
  "Check if there is something recognizable at or around point
and do something with it."
  (interactive)
    ;; occurring esense errors are converted to standard errors, so
    ;; that Emacs can show them in the message buffer
    (condition-case error-data
        (if (save-excursion
              (goto-char (point-at-bol))
              (looking-at esense-include-regexp))
            (esense-go-to-include-file (esense-get-include-file))

          (esense-do-something-with-symbol-at-point))

      (esense-error (error (cdr error-data)))))


(defun esense-electric-character (&optional arg)
  "Insert the typed character and pop up a completion buffer.

Behaves just like the normal semicolon when supplied with a
numerical arg."
  (interactive "P")
  (self-insert-command 1) ; (prefix-numeric-value arg))
  (fsharp-complete))


;; (defun esense-electric-character (&optional arg)
;;   "Insert the typed character and pop up a completion buffer.

;; Behaves just like the normal semicolon when supplied with a
;; numerical arg."
;;   (interactive "P")
;;   (self-insert-command (prefix-numeric-value arg))
;;   (unless (or arg
;;               (not esense-electric-commands)
;;               (frame-live-p esense-completion-frame)
;;               (erlang-in-literal))

;;     ;; esense errors occurring when typing an electric character are
;;     ;; converted to simple messages to avoid bothering the user too
;;     ;; much
;;     (condition-case error-data
;;         (esense-do-something-with-symbol-at-point nil nil t)
;;       (esense-error
;;        (message (cdr error-data))))))


(defun esense-do-something-with-symbol-at-point (&optional go-to-doc recursive electric)
  "Check if there is a recognizable symbol at or around point and either offer
completions for it or show popup help. If GO-TO-DOC is t then go to documentation
instead of showing popup help.
If RECURSIVE is t then the function was invoked by itself when looking for an object
to act upon.
If ELECTRIC is t then print fewer messages than when the completion is invoked explicitly.

Recursive operations run under `save-excursion' and sometimes their
result involve the same buffer this function was invoked in. In those cases
the function returns a function object which embodies the operation
to be executed *outside* `save-excursion'."
  (interactive)
  (let* ((symbol-info (esense-get-symbol-at-point))
         (symbol (first symbol-info))
         (char-before (second symbol-info))
         (symbol-beginning-position (third symbol-info))
         (at-end (fourth symbol-info))
         (char-after (fifth symbol-info))
         (symbol-end-position (sixth symbol-info)))

    ;; if we're standing at the end of the buffer set char-after
    ;; to a dummy non-word character to avoid comparison with nil
    (unless char-after
      (setq char-after ?|))

    ;; same for char-before
    (unless char-before
      (setq char-before ?|))

    (cond
     ;; macro
     ((= char-before ??)
      (if go-to-doc
          (esense-maybe-resolve-macro symbol t char-after
                                      symbol-end-position)

        (if (and at-end
                 (not recursive))
            (esense-complete-macro symbol symbol-beginning-position)
          (esense-maybe-resolve-macro symbol nil char-after
                                      symbol-end-position))))

     ;; record
     ((= char-before ?#)
      (if go-to-doc
          (if recursive
              ;; in a recursive call we're in a save-excursion, so
              ;; we can't jump to the definition if it is in the
              ;; same buffer the function was invoked in
              ;;
              ;; we return a closure instead which the caller
              ;; will execute
              (lexical-let ((record symbol))
                (lambda ()
                  (esense-go-to-record-definition record)))

            (esense-go-to-record-definition symbol))

        (if (and at-end
                 (not recursive))
            (esense-complete-record symbol nil symbol-beginning-position)

          (esense-show-record-help symbol))))

     ;; record field
     ((= char-before ?.)
      (let ((field symbol)
            record)
        (save-excursion
          (goto-char symbol-beginning-position)
          ;; skip possible quotation mark for quoted field name
          (skip-chars-backward "'")
          (backward-char)
          ;; skip possible quotation mark for quoted record name
          (skip-chars-backward "'")
          (unless (bobp)
            (let* ((symbol-info (esense-get-symbol-at-point))
                   (char-before (second symbol-info)))
              (if (= char-before ?#)
                  (setq record (first symbol-info))))))

        (if (not record)
            (unless electric
              (message (concat "It looks like a record field, "
                               "but I cannot determine the record name. "
                               "It seems the # character is missing.")))

          (if go-to-doc
              (esense-go-to-record-definition record field)

            (if (and at-end
                     (not recursive))
                (esense-complete-record record field symbol-beginning-position)

              (esense-show-record-help record field))))))

     ;; module:function
     ((or (= char-before ?:)
          (= char-after ?:))
      (let (module function arguments)
        (if (= char-after ?:)
            (progn (setq module symbol)
                   (save-excursion
                     (goto-char symbol-end-position)
                     ;; skip possible quotation mark for quoted module name
                     (skip-chars-forward "'")
                     (forward-char)
                     ;; skip possible quotation mark for quoted function name
                     (skip-chars-forward "'")
                     (setq function (first (esense-get-symbol-at-point)))
                     (setq arguments (esense-get-function-invocation))))

          (setq function symbol)
          (setq arguments (esense-get-function-invocation))
          (save-excursion
            (goto-char symbol-beginning-position)
            ;; skip possible quotation mark for quoted function name
            (skip-chars-backward "'")
            (backward-char)
            ;; skip possible quotation mark for quoted module name
            (skip-chars-backward "'")
            (let* ((symbol-info (esense-get-symbol-at-point)))
              (setq module (first symbol-info)))))

        (if (= (length module) 0)
            (message (concat "It looks like a function, "
                             "but I cannot determine the module name."))

          (if go-to-doc
              (esense-go-to-function-definition
               module function
               (esense-get-number-of-function-arguments) arguments)

            (if (and at-end
                     (/= char-after ?:)
                     (not recursive))
                (esense-complete-function module function symbol-beginning-position)

              (esense-show-function-help
               module function (esense-get-number-of-function-arguments)))))))

     ;; something else
     (t
      ;; check if we're standing in the parameter list of a function
      ;; or in a record
      (let* ((bound (save-excursion
                      (erlang-beginning-of-clause)
                      (point)))
             (pos (save-restriction
                    (narrow-to-region bound (point))
                    (condition-case nil
                        (scan-lists (point) -1 1)
                      ;; XEmacs doesn't know scan-error, so we have to
                      ;; catch 'error which is a pity, because it
                      ;; swallows all kinds of errors, not just the
                      ;; ones we want to catch :(
                      ((scan-error error) nil))))
             (symbol-info (if pos
                              (save-excursion
                                (goto-char pos)
                                ;; skip possible quotation mark for quoted
                                ;; atoms
                                (skip-chars-backward "'")
                                (setq pos (point))
                                (esense-get-symbol-at-point))))
             (parent-symbol (first symbol-info))
             (char-before-parent (second symbol-info))
             (parent-symbol-beginning-position (sixth symbol-info)))

        ;; if there is something at point
        (if (> (length symbol) 0)
            (if (and parent-symbol
                     (> (length parent-symbol) 0)
                     ;; if we're standing in a record
                     (eq char-before-parent ?#)
                     ;; and it seems a right place for a record field
                     (save-excursion
                       (goto-char symbol-beginning-position)
                       (skip-syntax-backward " ")
                       (or (eq (char-before) ?{)
                           (eq (char-before) ?,))))

                (if go-to-doc
                    (esense-go-to-record-definition parent-symbol symbol)

                  (if at-end
                      (esense-complete-record parent-symbol symbol
                                              symbol-beginning-position)
                    (esense-show-record-help parent-symbol symbol)))

              ;; let's say it's a function name
              (let ((function symbol))
                (if go-to-doc
                    (let ((numargs (esense-get-number-of-function-arguments)))
                      (if recursive
                          ;; in a recursive call we're in a save-excursion, so
                          ;; we can't jump to the definition if it is in the
                          ;; same buffer the function was invoked in
                          ;;
                          ;; we return a closure instead which the caller
                          ;; will execute
                          (lexical-let ((function function)
                                        (numargs numargs))
                            (lambda ()
                              (esense-go-to-function-definition nil function
                                                                numargs)))

                        (esense-go-to-function-definition nil function
                                                          numargs)))

                  (if (and at-end
                           (not recursive))
                      (esense-complete-function nil function
                                                symbol-beginning-position)

                    (esense-show-function-help
                     nil function (esense-get-number-of-function-arguments))))))

          ;; there is nothing at point
          ;; try to guess from the context what should be done
          (assert (not recursive))

          (if (and parent-symbol
                   (> (length parent-symbol) 0))
              (if (and
                   ;; if we're standing in a record
                   (eq char-before-parent ?#)
                   ;; and the user didn't want to jump to the defintion
                   (not go-to-doc)
                   ;; and it seems a right place for a record field
                   (save-excursion
                     (skip-syntax-backward " ")
                     (or (eq (char-before) ?{)
                         (eq (char-before) ?,))))
                  ;; start record field completion
                  (esense-complete-record parent-symbol "" (point))

                ;; otherwise do something else with the parent symbol
                (let ((operation
                       (save-excursion
                         (goto-char pos)
                         (esense-do-something-with-symbol-at-point
                          go-to-doc t))))
                  (if (functionp operation)
                      (funcall operation))))

            ;; if nothing interesting is found start function
            ;; completion or go to the documentation depending on
            ;; how we were invoked
            (if go-to-doc
                (esense-go-to-function-documentation)

              (esense-complete-function
               nil "" symbol-beginning-position)))))))))


(defun esense-go-to-documentation ()
  "Go to the documentation of thing at point or definition if documentation is
not available."
  (interactive)

  ;; we use the marker ring of etags to record where we came from
  (if esense-xemacs
      (push-tag-mark))

  (let ((origin (point-marker)))
    ;; occuring esense errors are converted to standard errors, so
    ;; that Emacs can show them in the message buffer
    (condition-case error-data
        (esense-do-something-with-symbol-at-point t)
      (esense-error (error (cdr error-data))))

    ;; we use the marker ring of etags to record where we came from if
    ;; no error occurred
    (unless esense-xemacs
      (ring-insert find-tag-marker-ring origin))))


(defun esense-go-to-include-file (file)
  "Visit include file FILE."
  (let* ((includes (let ((esense-ignore-unknown-includes nil))
                     (esense-get-include-data (list file) t)))
         (file (if (= (length includes) 1)
                   (esense-erlang-source (car includes))
                 (esense-completing-read
                  (concat "There are more than one candidates. "
                          "Select which one to visit: ")
                  (mapcar (lambda (include-data)
                            (list (esense-erlang-source
                                   include-data)))
                          includes)
                  nil t))))
    (find-file file)))


(defun esense-complete-function (module function symbol-beginning-position)
  "Show function completion list for FUNCTON of MODULE if possible.
If module is nil then auto exported functions from module `erlang',
functions from the current file, from included header files
 and module names are offered for completion."

  (setq esense-completion-module-info module)

  (let ((module-given module)
        completion-list)
    (if module
        ;; make sure there is such a module
        (esense-lookup-module module)

      ;; append all module names to the completion list
      (dolist (module-name esense-modules)
        (push module-name completion-list))

      ;; add all functions from the current file and from included
      ;; header files
      (dolist (file (esense-collect-available-data))
        (dolist (function (esense-erlang-functions file))
          (setq completion-list
                (adjoin (esense-function-spec function)
                        completion-list
                        :test 'equal))))

      ;; add imported functions
      (let ((module-info (esense-get-current-module-info)))
        (if module-info
            (dolist (import (esense-erlang-imports module-info))
              (let* ((imported-module (esense-lookup-module
                                       (esense-import-module import))))
                (dolist (function (esense-import-functions import))
                  (let* ((name (esense-imported-function-name function))
                         (arity (esense-imported-function-arity function))
                         (result (esense-get-function-data-from-file
                                  imported-module
                                  ;; FIXME: this shouldn't be duplicated
                                  ;; see esense-get-function-data
                                  (list (concat name "/" (int-to-string arity))
                                        (concat name "("))
                                  (esense-imported-function-arity function)
                                  t)))

                    (if result
                        (dolist (imported-function (car result))
                          (setq completion-list
                                (adjoin (esense-function-spec imported-function)
                                        completion-list
                                        :test 'equal))))))))))

      ;; add all functions from the erlang module
      (setq module "erlang"))

    (dolist (function (esense-erlang-functions (esense-lookup-module module)))
      (if (or (esense-function-exported function)
              ;; not auto-imported functions from module erlang
              ;; shown only if module name is given explicitly
              (and module-given (equal module "erlang")))
          (push (esense-function-spec function) completion-list)))

    (esense-start-completion symbol-beginning-position
                             completion-list
                             'esense-completion-insert-function
                             'esense-completion-get-function-documentation)))


(defun esense-show-function-help (module function &optional arity)
  "Show popup help for MODULE:FUNCTION."
  (let ((doc (esense-get-function-documentation module function arity)))
    (if doc
        (esense-show-tooltip-for-point doc)
      (message (concat "No documentation found for "
                       (if module (concat module ":"))
                       function
                       (if arity (concat "/" (int-to-string arity))))))))


(defun esense-get-function-documentation (module function &optional arity)
  "Return a formatted documentation for all functions of MODULE which
name is FUNCTION.

If MODULE is nil then search for function in the `erlang' module and in
the current buffer too if it's a module.

If no documentation found nil is returned."
  (let (doc)
    (setq
     doc
     (mapcan
      (lambda (func-data)
        (let ((functions (car func-data))
              (file (cdr func-data)))

          (when functions
            (mapcar
             (lambda (funcdata)
               (let ((doc (esense-function-doc funcdata))
                     (params (esense-function-params funcdata)))

                 (if (not doc)
                     (setq doc "No documentation available.")

                   (if params
                       (setq params
                             (concat (mapconcat (lambda (param) param)
                                                params
                                                "\n")
                                     "\n\n"))

                     (setq params ""))

                   ;; if doc string has newlines in it then we consider it
                   ;; preformatted

                   (when (not (string-match (char-to-string
                                             esense-value-begin-end-tag)
                                            function))

                     ;; remove HTML tags
                     (while (string-match "<[^>]*>" doc)
                       (setq doc (replace-match "" nil nil doc)))

                     ;; convert character entities
                     (mapc
                      (lambda (rule)
                        (let ((entity (car rule))
                              (replacement (cdr rule)))
                          (while (string-match entity doc)
                            (setq doc (replace-match replacement nil nil doc)))))
                      `(("&#62;" . ">")
                        ("&#60;" . "<")
                        ("&#38;" . "&")
                        ("&#34;" . "\"")
                        ;; convert non-breaking space to a simple space
                        (,(char-to-string 160) . " ")))

                     ;; wrap text
                     (let ((count 0)
                           (pos 0)
                           prevspace)
                       (while (< pos (length doc))
                         (let ((char (aref doc pos)))
                           (cond ((= char ?\n)
                                  (setq count 0))
                                 ((= char ? )
                                  (if (< count esense-max-tooltip-line-length)
                                      (progn (setq prevspace pos)
                                             (incf count))

                                    ;; insert newline
                                    (if prevspace
                                        (progn (aset doc prevspace ?\n)
                                               (setq count (- pos prevspace)))
                                      (aset doc pos ?\n)
                                      (setq count 0))

                                    (setq prevspace nil)))
                                 (t
                                  (incf count)))
                           (incf pos))))))

                 (cons (concat
                        (if (esense-erlang-modulename file)
                            (esense-erlang-modulename file)
                          (esense-truncate-path (esense-erlang-source file)
                                                40))
                        ":"
                        (esense-function-spec funcdata))
                       (concat params doc))))

             functions))))

      (esense-get-function-data function module arity)))

    ;; filter out null values
    (setq doc (delete-if 'null doc))

    (if doc
        (esense-format-documentation doc))))


(defun esense-go-to-function-definition (module &optional function arity arguments)
  "Go to the definition of MODULE:FUNCTION or MODULE only if FUNCTION is not given.
If the index file of the relevant module was created from HTML documentation then
go to the relevant manpage instead.

If MODULE is nil then search for function in the `erlang' module and in
the current buffer too if it's a module.

If ARITY is non-nil then it's the arity of the function determined
from the context.

If ARGUMENTS is non-nil then it's the string of the invocation arguments of
the function."
  (let (functions file source)
    (if (equal function "")
        (setq file (esense-lookup-module module))

      (let ((files (esense-get-function-data function module arity)))
        (if (not files)
            (error (concat "Function "
                           (if module
                               (concat module ":")
                             "")
                           function
                           (if arity
                               (concat "/" (int-to-string arity))
                             "")
                           " is not found")))

        (if (= (length files) 1)
            ;; only one file matches
            (progn
              (setq functions (caar files))
              (setq file (cdar files)))

          (let* ((completions
                  (mapcar (lambda (file-info)
                            (let ((descriptor (cdr file-info)))
                              (cons
                               (if (esense-erlang-modulename descriptor)
                                   (esense-erlang-modulename descriptor)
                                 (esense-erlang-source descriptor))
                               file-info)))
                          files))
                 (selection
                  (esense-completing-read
                   (concat "There are more than one possible locations"
                           " for this function. Select one: ")
                   completions
                   nil t))
                 (selected (cdr (assoc selection completions))))

            (setq functions (car selected))
            (setq file (cdr selected))))))

    (setq source (esense-erlang-source file))

    (cond
     ((string= (file-name-extension source) "html")
      ;; a module with html documentation is probably a standard
      ;; module, so its manual page can be shown
      (erlang-man-function (concat (esense-erlang-modulename file)
                                   (if (and function
                                            (not (equal function "")))
                                       (concat ":" function)))))

     (t
     (if (equal function "")
         (progn
           (find-file source)
           (goto-char (point-min)))

       (assert functions)
       (let ((clause-found (= (length functions) 1)))
         (esense-find-file source)
         (goto-line
          ;; choosing the first function in the file with the
          ;; given name if there are more than one matches
          (apply 'min
                 (mapcar (lambda (function)
                           (esense-function-line function))
                         functions)))

         (if clause-found
             (if (and arguments
                      esense-find-function-matching-invocation-pattern)
                 (esense-find-function-for-invocation function arguments))

           (assert (not arity))
           (message (concat "Arity not given. "
                            "Choosing the first matching function.")))))))))



(defun esense-complete-record (record field symbol-beginning-position)
  "Complete record or record field at point."
  (let (symbol completion-list)
    (if field
        (progn
          (setq symbol field)
          (setq esense-completion-record-info-list
                (esense-get-record-data record))
          (dolist (record-info esense-completion-record-info-list)
            (dolist (field (esense-record-fields (car record-info)))
              (setq completion-list
                    ;; It is possible the location of an include file
                    ;; cannot be determined and there are more than
                    ;; one possible candidates. Make sure only unique
                    ;; names get to the list.
                    ;; Note that this means if there are records
                    ;; with the same name, but different definitions
                    ;; in the included files their fields will be
                    ;; merged in the completion list.
                    (adjoin (esense-record-field-name field)
                            completion-list
                            :test 'equal)))))

      (setq symbol record)

      (let ((record-list-list (mapcar 'car (esense-collect-record-data))))
        ;; It is possible the location of an include file
        ;; cannot be determined and there are more than
        ;; one possible candidates. Make sure only unique
        ;; record names get to the list.
        (dolist (record-list record-list-list)
          (setq completion-list
                (nunion completion-list
                        (mapcar 'esense-record-name
                                record-list)
                        :test 'equal)))))

    (esense-start-completion symbol-beginning-position
                             completion-list
                             nil
                             (if field
                                 'esense-get-record-field-documentation
                               'esense-get-record-documentation))))


(defun esense-show-record-help (record &optional field)
  "Show popup help for RECORD or RECORD.FIELD if FIELD is non-nil."
  (esense-show-tooltip-for-point
   (esense-get-record-documentation record field)))


(defun esense-get-record-documentation (record &optional field)
  "Return documentation of record or the text 'No documentation' if there
isn't any.
If FIELD is non-nil then the documentation of the field is shown instead."
  (let ((record-data (esense-get-record-data record)))
    (if field
         (esense-get-record-field-documentation field record-data)

      (esense-format-documentation
       (mapcar (lambda (record-info)
                 (let ((doc (esense-record-doc (car record-info))))
                   (cons (esense-truncate-path (cdr record-info))
                         (if doc
                             doc
                           "No documentation."))))
               record-data)))))


(defun esense-get-record-field-documentation (field &optional record-info-list)
  "Return documentation of record field or the text 'No documentation'
if there isn't any.
The record information is taken from RECORD-INFO if given,
otherwise `esense-completion-record-info-list' is used."
  (unless record-info-list
    (setq record-info-list esense-completion-record-info-list))

  ;; in the included header files there can be records with the same
  ;; name, but different field list
  ;;
  ;; for those records wich do not have a field with this name
  ;; mapcar returns nil and those entries are removed from the
  ;; list before formatting the documentation
  (let ((doc
         (delete-if
          'null
          (mapcar
           (lambda (record-info)
             (let ((result
                    (some
                     (lambda (field-info)
                       (if (equal
                            (esense-record-field-name field-info)
                            field)
                           field-info))
                     (esense-record-fields (car record-info)))))
               (if result
                   (let ((doc (esense-record-field-doc result)))
                     (cons (esense-truncate-path (cdr record-info))
                           (if doc
                               doc
                             "No documentation."))))))
           record-info-list))))
    (if doc
        (esense-format-documentation doc)
      (error "Record %s has no field with name %s."
             (esense-record-name (caar record-info-list))
             field))))


(defun esense-go-to-record-definition (record &optional field)
  "Go to the definition of RECORD. If FIELD is non-nil then jump to its
definition within the record."
  (let* ((record-data (esense-get-record-data record))
         (file (if (= (length record-data) 1)
                   (cdar record-data)
                 (esense-completing-read
                  (concat "There are more than one candidates. "
                          "Select which one to visit: ")
                  (mapcar (lambda (record-info)
                            (list (cdr record-info)))
                          record-data)
                  nil t)))
         (record (rassoc file record-data)))

    (assert record)
    (find-file (cdr record))
    (goto-line (esense-record-line (car record)))
    (when field
      (search-forward field)
      (recenter))))


(defun esense-complete-macro (macro symbol-beginning-position)
  "Complete macro at point."
  (let (completion-list
        (macro-list-list (mapcar 'car (esense-collect-macro-data))))

    ;; It is possible the location of an include file
    ;; cannot be determined exactly and there are more than
    ;; one possible candidates. Make sure only unique
    ;; names get to the list.
    (dolist (macro-list macro-list-list)
      (setq completion-list
            (nunion completion-list
                    (mapcar 'esense-macro-name macro-list)
                    :test 'equal)))

    (esense-start-completion symbol-beginning-position
                             completion-list
                             'esense-completion-insert-macro
                             'esense-get-macro-documentation)))


(defun esense-completion-insert-macro (macro)
  "Insert macro header into the buffer stripping off unnecessary parts."
  (insert (esense-strip-function macro))
  (if (esense-function-name-p macro)
      (insert "(")))


(defun esense-show-macro-help (macro)
  "Show popup help for MACRO."
  (esense-show-tooltip-for-point
   (esense-get-macro-documentation macro)))


(defun esense-get-macro-documentation (macro)
  "Return documentation of macro or the text 'No documentation' if there
isn't any."
  ;; Macros are usually not documented, that's why macro documentation
  ;; retrieval is not yet implemented.
  (esense-format-documentation
   (mapcar (lambda (macro-info)
             (let ((doc (concat (esense-macro-name (car macro-info)) " = "))
                   (value (esense-macro-value (car macro-info)))
                   (file (cdr macro-info)))

               (cons (if file
                         (esense-truncate-path file)
                       "Predefined macro")
                     (concat doc (esense-resolve-macro-value value)))))

           (esense-get-macro-data (esense-strip-function macro)))))


(defun esense-maybe-resolve-macro (macro go-to-doc char-after
                                         symbol-end-position)
  "Possibly resolve MACRO value and act upon the resolved value if
option `esense-resolve-macros' is set.

If GO-TO-DOC is t then go to documentation instead of showing popup help.

CHAR-AFTER is the character after the macro. SYMBOL-END-POSITION is the position
where the macro symbol ends."
  (if (and esense-resolve-macros
           (eq char-after ?:)
           (or (not (equal esense-resolve-macros 'ask))
               (y-or-n-p "Do you want to resolve the macro and act on the resolved value? ")))
      (save-excursion
        (goto-char (1+ symbol-end-position))
        (let ((function (car (esense-get-symbol-at-point)))
              (module (esense-macro-value
                       (caar (esense-get-macro-data macro)))))

          (if go-to-doc
              (esense-go-to-function-definition module function)
            (esense-show-function-help module function))))

    (if go-to-doc
        (esense-go-to-macro-definition macro)
      (esense-show-macro-help symbol))))


(defun esense-go-to-macro-definition (macro)
  "Go to the definition of macro MACRO."
  (let* ((macro-data (esense-get-macro-data macro))
         (file (if (= (length macro-data) 1)
                   (cdar macro-data)
                 (esense-completing-read
                  (concat "There are more than one candidates. "
                          "Select which one to visit: ")
                  (mapcar (lambda (macro-info)
                            (list (cdr macro-info)))
                          macro-data)
                  nil t))))

    (if (not file)
        (message "Predefined macro.")

      (assert macro)
      (find-file file)
      (goto-char (point-min))
      (re-search-forward (format esense-macro-regexp macro))
      (beginning-of-line))))


(defun esense-go-to-function-documentation ()
  "Read a module name and function name and go to the documentation."
  (interactive)
  (let* ((default-module (esense-get-current-module-name))
         (module (completing-read (concat "Module"
                                          (if default-module
                                              (concat " ("
                                                      default-module
                                                      ")")
                                            "")
                                          ": ")
                                  (mapcar (lambda (module-name)
                                            (list module-name))
                                          esense-modules)
                                  nil t nil nil default-module)))
    (if (and module
             (not (equal module "")))
        (let* ((functions (delete-duplicates
                           (mapcar (lambda (function)
                                     (list (esense-strip-function
                                            (esense-function-spec function))))
                                   (esense-erlang-functions
                                    (esense-lookup-module module)))
                           :test 'equal))
               (function (completing-read "Function: " functions nil t)))
          (esense-go-to-function-definition module function)))))


(defun esense-update-completion-list (filter)
   (let ((buffer (get-buffer-create esense-buffer-name))
         (filtered-list (esense-filter-completion-list esense-completion-list filter)))
     (set-buffer buffer)
     (erase-buffer)
     (esense-cancel-timer)
     (if (not filtered-list)
         (message "No completions found. Try deleting a few characters.")

       (mapc (lambda (x) (insert x "\n")) filtered-list)
       (delete-backward-char 1)
       (goto-char (point-min))
       ;; show selection
       (esense-completion-previous-item))))


(defun esense-filter-completion-list (list filter)
  "Return only those completions from LIST which match FILTER.
The strings are compared case-insensitively."
  (let ((filter-length (length filter)))
    (setq filter (downcase filter))
    (remove-if-not (lambda (x) (esense-string-begins-with
                                (downcase x) filter))
                   list)))


(defun esense-completion-show-popup-documentation ()
  "Show popup documentation for the currently selected completion item."
  (let ((orig-window (selected-window))
        (orig-frame (selected-frame)))

    (if (eq esense-completion-display-method 'window)
        (select-window (get-buffer-window esense-buffer-name))
      (select-frame esense-completion-frame))

    (let ((item (buffer-substring-no-properties (point-at-bol)
                                                (point-at-eol))))

      ;; we need to switch the window back here, because some
      ;; documentation retrieval function needs information from the
      ;; current buffer
      (if (eq esense-completion-display-method 'window)
          (select-window orig-window)
        (select-frame orig-frame))

      (let ((doc (funcall esense-completion-popup-documentation-function item)))
        (if (not doc)
            (message (concat "There is no documentation for " item))

          (if (eq esense-completion-display-method 'window)
              (select-window (get-buffer-window esense-buffer-name))
            (select-frame esense-completion-frame))

          (let ((point-pos (unless esense-xemacs (esense-point-position))))
            (esense-show-tooltip-for-point doc (car point-pos) (cdr point-pos)))

          (if (eq esense-completion-display-method 'window)
              (select-window orig-window)
            (select-frame orig-frame)))))))


(defun esense-completion-get-function-documentation (function)
  "Return documentation for the currently selected function or module
in the completion list."
  (let ((pos (esense-function-name-p function)))
    (if pos
        (if esense-completion-module-info
            (esense-get-function-documentation
             esense-completion-module-info function)

          ;; FIXME: if module is not given the we have to jump some
          ;; hoops to handle imported functions properly
          ;; Function arity should be read from the index file, instead
          ;; of determining it here
          (let ((name (substring function 0 pos))
                (arity (if (equal (substring function pos (1+ pos)) "/")
                           (string-to-int (substring function (1+ pos)))
                         (esense-get-arity-from-string function))))
            (esense-get-function-documentation
            esense-completion-module-info
            name arity)))

      "Module name.")))


(defun esense-completion-next-item ()
  "Select the next item in the completion list."
  (interactive)
  (esense-completion-select-item
    (lambda ()
      (forward-line +1))))


(defun esense-completion-previous-item ()
  "Select the previous item in the completion list."
   (interactive)
   (esense-completion-select-item
    (lambda ()
      (forward-line -1))))


(defun esense-completion-next-page ()
  "Go to the next page of the completion list."
  (interactive)
  (esense-completion-select-item
    (lambda ()
      (condition-case nil
          (scroll-up)
        (end-of-buffer (goto-char (point-max)))))))


(defun esense-completion-previous-page ()
  "Go to the previous page of the completion list."
   (interactive)
   (esense-completion-select-item
    (lambda ()
      (condition-case nil
          (scroll-down)
        (beginning-of-buffer (goto-char (point-min)))))))


(defun esense-completion-select-item (move-func)
  "Select an other item from the completion list by invoking
MOVE-FUNC.
MOVE-FUNC is a function object which selects an other item
from the completion list."
   (let ((orig-window (selected-window))
         (orig-frame (selected-frame)))

     (if (eq esense-completion-display-method 'window)
         (select-window (get-buffer-window esense-buffer-name))
       (select-frame esense-completion-frame))

     (if esense-xemacs
	 (let ((extent (extent-at (point-at-bol))))
	   (when extent
	     (delete-extent extent)))

       (let ((overlays (overlays-at (point-at-bol))))
	 (when overlays
	   (delete-overlay (car overlays)))))

     (funcall move-func)
     (end-of-line)

     (if esense-xemacs
	 (let ((extent (make-extent (point-at-bol)
				    (point-at-eol))))
	   (set-extent-property extent 'face 'highlight))

       (let ((overlay (make-overlay (point-at-bol)
				    (point-at-eol))))
	 (overlay-put overlay 'face 'region)))

     (if (eq esense-completion-display-method 'window)
         (select-window orig-window)
       (select-frame orig-frame)))

   (esense-cancel-timer)
   ;; start popup documentation timer if there is a documentation getter function set
   (when (and esense-completion-popup-documentation-function
              (not esense-xemacs)) ; it does not work on XEmacs for some reason
     (setq esense-timer (run-with-idle-timer 1 nil
                                             'esense-completion-show-popup-documentation))))



(defun esense-completion-self-insert-item (arg)
  "Insert selected completion item, followed by the character."
  (interactive "P")
  (esense-completion-insert-item)
  (self-insert-command 1))

(defun esense-dot-validation (arg)
  "Insert selected completion item, followed by the character."
  (interactive "P")
  (esense-completion-insert-item)
  (self-insert-command 1)
  (fsharp-complete))

(defun esense-completion-insert-item (&optional item)
  "Insert selected completion item.
If ITEM is not given then insert the current selection from the completion list."
  (interactive)
  (unless item
    (setq item (with-current-buffer (get-buffer esense-buffer-name)
                 (buffer-substring-no-properties (point-at-bol)
                                                 (point-at-eol))))
    (esense-abort-completion))
  (when (> (length item) 0)
    ;; cannot use esense-completion-symbol-beginning-position here,
    ;; because when esense-completion-insert-item is invoked directly, it is not set
    ;; FIXME: check if this comment still applies
    (let ((p (point)))
      (skip-syntax-backward "w_")
      (delete-region (point) p))
    (if esense-completion-inserter-function
        (progn
          (funcall esense-completion-inserter-function item)
          ;; reset to nil
          (setq esense-completion-inserter-function nil))
      (esense-completion-insert-atom item))))


(defun esense-completion-insert-atom (atom)
  "Insert an Erlang atom into the buffer adding single quotes around
it if necessary."

;;   (when (or
;;          ;; there is already a single quote before the atom
;;          (eq (char-before) ?')
;;          ;; does not begin with a lowercase letter
;;          (or (< (elt atom 0) ?a) (> (elt atom 0) ?z))
;;          ;; contains other characters than alphanumeric characters,
;;          ;; underscore or @
;;          (some (lambda (c)
;;                  (not (or
;;                        (and (>= c ?a) (<= c ?z))
;;                        (and (>= c ?A) (<= c ?Z))
;;                        (and (>= c ?0) (<= c ?9))
;;                        (eq c ?@)
;;                        (eq c ?_))))
;;                atom))

;;     (if (not (eq (char-before) ?'))
;;         (insert "'"))
;;     (when (not (eq (char-after) ?'))
;;         (insert "'")
;;         (backward-char)))

  (insert atom)
  ;; put cursor after the closing single quote if there is any
;;   (if (eq (char-after) ?')
;;       (forward-char))
)


(defun esense-completion-insert-function (function)
  "Insert function header into the buffer stripping off unnecessary parts."
  (esense-completion-insert-atom (esense-strip-function function))
  (let ((pos (esense-function-name-p function)))
    (when pos
      (insert "(")
      (let ((arity (substring function pos (+ pos 2))))
        (if (or (equal arity "/0")
                (equal arity "()"))
            (insert ")"))))))


(defun esense-strip-function (function)
  "Strip unnecessary parts from FUNCTION."
  (substring function 0 (esense-function-name-p function)))


(defun esense-command-hook ()
  (if esense-completion-startup
      ;; do nothing if the completion is just started
      (setq esense-completion-startup nil)

    (cond ((and (eq esense-completion-display-method 'window)
                (not (window-live-p (get-buffer-window esense-buffer-name)))
)
           (esense-abort-completion))

          ((eq this-command 'self-insert-command)
           (if (and (/= (char-syntax (char-before)) ?w)
                    (/= (char-syntax (char-before)) ?_))
               (esense-abort-completion)

             (esense-update-completion-list
              (buffer-substring-no-properties
               esense-completion-symbol-beginning-position (point)))))

          ((or (eq this-command 'esense-completion-next-item)
               (eq this-command 'esense-completion-previous-item)
               (eq this-command 'esense-completion-previous-page)
               (eq this-command 'esense-completion-next-page)
               (eq this-command 'esense-completion-insert-item)
               (and (eq esense-completion-display-method 'frame)
                    (eq this-command 'handle-switch-frame)))
           nil)

          ((or (eq this-command 'backward-delete-char-untabify)
	       (eq this-command 'delete-backward-char)
               (eq this-command 'fsharp-electric-backspace))
           (if (< (point) esense-completion-symbol-beginning-position)
               (esense-abort-completion)
             (esense-update-completion-list
              (buffer-substring-no-properties
               esense-completion-symbol-beginning-position (point)))))

          (t
           (esense-abort-completion)))))


(defun esense-abort-completion ()
  "Abort the completion in progress."
  (interactive)
  (remove-hook 'post-command-hook 'esense-command-hook)
  (esense-cancel-timer)

  ;; restore the mode line in the completion buffer
  ;; if necessary and unhide the cursor
  (let ((mode-line mode-line-format))
    (with-current-buffer esense-buffer-name
      (unless mode-line-format
        (setq mode-line-format mode-line))
      (setq cursor-type t)))

  (if (eq esense-completion-display-method 'frame)
      (delete-frame esense-completion-frame)

    (set-window-configuration esense-completion-saved-window-configuration))

  ;; restore minor mode map
  (setcdr (assoc 'esense-mode minor-mode-map-alist)
          esense-minor-mode-map)

  ;; set the completion list to nil to indicate the completion
  ;; is no longer in progress
  (setq esense-completion-list nil))


(defun esense-lookup-module (module)
  "Lookup information for MODULE and return it.

If the module is not found in the index, but there is an index file on the
disk for MODULE then try adding the module to the index on-the-fly.

If the module can't be found in or added to the index signal an error."
  (cond ((member module esense-modules)
         (esense-read-module-file module))

        ((file-readable-p (esense-get-module-index-file-name module))
         (let ((result (esense-read-module-file module)))
           (push module esense-modules)
           result))

        ;; try the search directories
        ((let ((file (esense-search-module module)))
           (if file
               (let ((result (esense-read-module-file module file)))
                 (push module esense-modules)
                 result))))

        (t
         (signal 'esense-error
                 (format "Module %s is unknown." module)))))

(defun esense-start-completion (symbol-beginning-position
                                completion-list
                                &optional documentation-getter inserter)
  "Prepare and show the completion buffer.
The variable `esense-completion-symbol-beginning-position' is set to SYMBOL-BEGINNING-POSITION and `esense-completion-list' to COMPLETION-LIST.
If INSERTER is given it is stored in `esense-completion-inserter-function'.
If DOCUMENTATION-GETTER is given it is stored in `esense-completion-popup-documentation-function'."
  (unless completion-list
    (signal 'esense-error "No completions found."))

  (setq esense-completion-symbol-beginning-position symbol-beginning-position)
  (setq esense-completion-list completion-list)
  (setq esense-completion-inserter-function inserter)
  (setq esense-completion-popup-documentation-function documentation-getter)

  (let* ((filter (buffer-substring-no-properties esense-completion-symbol-beginning-position
                                                 (point)))
;;         (completions completion-list))
          (completions (esense-filter-completion-list esense-completion-list
                                                      filter)))
    (if (let ((stripped-completions
               (delete-duplicates
                (mapcar (lambda (completion)
                          (esense-strip-function completion))
                        completions)
                :test 'equal)))
          (and (car stripped-completions)
             (not (cdr stripped-completions))))
        ;; if there is only one possible completion
        (progn
          (esense-completion-insert-item (car completions))
          ;; set the completion list to nil to indicate the completion
          ;; is no longer in progress
          (setq esense-completion-list nil)
          (message "Exactly one completion."))

      ;; complete additional characters if possible
      (let ((first (first completions))
            (pos (length filter))
            char newcomp)
        (while (and (< pos (length first))
                    (progn (setq char (aref first pos))
                           (setq newcomp (concat filter (char-to-string char)))
                           (= (length (esense-filter-completion-list completions newcomp))
                              (length completions))))
          (setq filter newcomp)
          (insert char)
          (incf pos)))

      (if (eq esense-completion-display-method 'window)
          (let ((orig-window (selected-window)))
            (setq esense-completion-saved-window-configuration
                  (current-window-configuration))
            (if esense-xemacs
                (switch-to-buffer-other-window esense-buffer-name)
              (switch-to-buffer-other-window esense-buffer-name t))
            (select-window orig-window))

        ;; popup a new frame with the completions
        (let* ((this-frame (selected-frame))
               (width (max (+ 2         ; padding
                              (apply 'max (mapcar 'length completion-list)))
                           15))
               (height (max
                        (min (length completion-list) 20)
                        5))
               (pixel-width (* width (frame-char-width)))
               (pixel-height (*
                              ;; let's say the titlebar has the same
                              ;; height as a text line
                              (1+ height)
                              (frame-char-height)))
               (position (esense-calculate-popup-position pixel-width
                                                          pixel-height
                                                          'below)))
          (setq esense-completion-frame
                (make-frame `((top . ,(cdr position))
                              (left . ,(car position))
                              (width . ,width)
                              (height . ,height)
                              (minibuffer . nil)
                              (menu-bar-lines . 0)
                              (tool-bar-lines . 0)
                              (title . "Completions"))))
          (select-frame esense-completion-frame)
          (switch-to-buffer esense-buffer-name)
          (setq mode-line-format nil)
          (if (> emacs-major-version 21)
              (set-window-fringes nil 0 0))

          ;; this seems to solve problem of the first appearing tooltip
          ;; in the completion list being hidden immediately
          ;(set-mouse-position esense-completion-frame 0 0)

          ;; this is to prevent a tab bar to appear in the completion
          ;; frame if tabbar-mode is used
          (set-window-dedicated-p (selected-window) t)

          (redirect-frame-focus esense-completion-frame this-frame)))

      (setq esense-completion-list (sort esense-completion-list
                                         (lambda (first second)
                                           (string< (downcase first)
                                                    (downcase second)))))
      (esense-update-completion-list filter)

      ;; change the minor mode map to enable completion
      (setcdr (assoc 'esense-mode minor-mode-map-alist)
              esense-minor-mode-completion-map)

      ;; hide cursor in completion buffer
      (with-current-buffer (get-buffer esense-buffer-name)
        (setq cursor-type nil))

      ;; install our own command hook to monitor typed characters
      (setq esense-completion-startup t)
      (add-hook 'post-command-hook 'esense-command-hook)
)))



(defun get-point-pixel-position ()
  "Return the position of point in pixels within the frame."
  (let ((point-pos (esense-point-position)))
    (esense-get-pixel-position (car point-pos) (cdr point-pos))))


(defun esense-get-pixel-position (x y)
  "Return the pixel position of location X Y (1-based) within the frame."
  (let ((old-mouse-pos (mouse-position)))
    (set-mouse-position (selected-frame)
                        ;; the fringe is the 0th column, so x is OK
                        x
                        (1- y))
    (let ((point-x (car (cdr (mouse-pixel-position))))
          (point-y (cdr (cdr (mouse-pixel-position)))))
      ;; on Linux with the Enlightenment window manager restoring the
      ;; mouse coordinates didn't work well, so for the time being it
      ;; is enabled for Windows only
      (when (eq window-system 'w32)
        (set-mouse-position
         (selected-frame)
         (cadr old-mouse-pos)
         (cddr old-mouse-pos)))
      (cons point-x point-y))))


(defun esense-string-begins-with (str begin)
  "Return t if STR begins with the string BEGIN, or nil otherwise."
  (let ((begin-length (length begin)))
    (and (>= (length str)
             begin-length)
         (string= (substring str 0 begin-length)
                  begin))))


(defun esense-cancel-timer ()
  "Cancel timer if it is active."
  (when esense-timer
    (if esense-xemacs
	(delete-itimer esense-timer)
      (cancel-timer esense-timer))
    (setq esense-timer nil)))


(defun esense-show-tooltip-for-point (msg &optional x y)
  "Show tooltip MSG at point or at X Y if given."
  (let ((lines (split-string msg "\n")))
    (when (> (length lines)
             esense-max-tooltip-lines)
      (setq lines
            (append
             (subseq lines
                     0
                     (1- esense-max-tooltip-lines))
             (list
              (concat "(Further lines not shown "
                      "due to line number limit.)"))))
      (setq msg (mapconcat (lambda (x) x)
                           lines "\n")))

    (if esense-xemacs
        (esense-show-tooltip-for-point-xemacs msg lines)
      (esense-show-tooltip-for-point-gnuemacs msg lines x y))))


(defun esense-show-tooltip-for-point-xemacs (msg lines)
  "The tooltip handling of XEmacs is not very sophisticated, so we fallback
 to simple messages in the echo area."
  (let ((diff (- (length lines) (window-height (minibuffer-window))))
        (current-window (selected-window)))
    (when (/= diff 0)
      (setq esense-xemacs-saved-window-configuration
            (current-window-configuration))
      (select-window (minibuffer-window))
      (enlarge-window diff)
      (select-window current-window)
      (add-hook 'pre-command-hook 'esense-xemacs-restore-window-configuration)))

  (message msg))

(defun esense-xemacs-restore-window-configuration ()
  (set-window-configuration esense-xemacs-saved-window-configuration)
  (remove-hook 'pre-command-hook 'esense-xemacs-restore-window-configuration))


(defun esense-show-tooltip-for-point-gnuemacs (msg lines &optional x y)
  "Show tooltip MSG at point or at X Y if given.
LINES is the same as MSG split into individual lines."
  (let* ((tooltip-width (* (frame-char-width)
                           (apply 'max (mapcar 'length lines))))
         (tooltip-height (* (frame-char-height) (min (length lines)
                                                     (cdr x-max-tooltip-size))))
         (position (esense-calculate-popup-position tooltip-width
                                                    tooltip-height
                                                    'above
                                                    x y))
         (tooltip-hide-delay 600)
         (tooltip-frame-parameters (append `((left . ,(car position))
                                             (top . ,(cdr position)))
                                           tooltip-frame-parameters))

         (old-propertize (symbol-function 'propertize)))

    ;; the definition of `propertize' is substituted with a dummy
    ;; function temporarily, so that tooltip-show doesn't override the
    ;; properties of msg
    (fset 'propertize (lambda (string &rest properties)
                        string))
    (unwind-protect
        (tooltip-show msg)
      (fset 'propertize old-propertize))))


(defun esense-calculate-popup-position (width height preferred-pos &optional x y)
  "Calculate pixel position of a rectangle with size WIDTH*HEIGHT at
X;Y or point if they are not given and return a list (X . Y) containing
the calculated position.
Ensure the rectangle does not cover the position.
PREFERRED-POS can either be the symbol `above' or `below' indicating the
preferred position of the popup relative to point."
  (if (and x
           (> x (frame-width)))
      (setq x (frame-width)))

  (let* ((fx (frame-parameter nil 'left))
         (fy (frame-parameter nil 'top))
         (fw (frame-pixel-width))
         (fh (frame-pixel-height))

         ;; handles the case where (frame-parameter nil 'top) or
         ;; (frame-parameter nil 'left) return something like (+ -4).
         ;; This was the case where e.g. Emacs window is maximized, at
         ;; least on Windows XP. The handling code is "shamelessly
         ;; stolen" from cedet/speedbar/dframe.el
         ;;
         ;; (contributed by Andrey Grigoriev)
         (frame-left (if (not (consp fx))
                         fx
                       ;; If fx is a list, that means we grow
                       ;; from a specific edge of the display.
                       ;; Convert that to the distance from the
                       ;; left side of the display.
                       (if (eq (car fx) '-)
                           ;; A - means distance from the right edge
                           ;; of the display, or DW - fx - framewidth
                           (- (x-display-pixel-width) (car (cdr fx)) fw)
                         (car (cdr fx)))))

         (frame-top (if (not (consp fy))
                        fy
                      ;; If fy is a list, that means we grow
                      ;; from a specific edge of the display.
                      ;; Convert that to the distance from the
                      ;; left side of the display.
                      (if (eq (car fy) '-)
                          ;; A - means distance from the right edge
                          ;; of the display, or DW - pfx - framewidth
                          (- (x-display-pixel-height) (car (cdr fy)) fh)
                        (car (cdr fy)))))

         (point-x (car (if x
                           (esense-get-pixel-position x y)
                         (get-point-pixel-position))))
         (point-y (cdr (if y
                           (esense-get-pixel-position x y)
                         (get-point-pixel-position))))

         (corner-x (let ((x (+ point-x
                               frame-left
                               ;; a small offset is added to the x
                               ;; position, so that it's a little
                               ;; to the right from the position
                               ;; (without this offset the tooltip
                               ;; and the mouse cursor sometimes
                               ;; overlap each other and the tooltip
                               ;; is hidden immediately)
                               (* 2 (frame-char-width)))))
                     (if (< (+ x width)
                            (display-pixel-width))
                         x
                       (- (display-pixel-width) width))))

         (real-y-offset (+ point-y
                           frame-top
                           esense-titlebar-height
                           ;; menu bar height
                           (let ((n-lines (frame-parameter nil 'menu-bar-lines)))
                             ;; FIXME: It's a bit tricky. Menu font
                             ;; isn't necessarily the same as frame font
                             ;; so frame-char-height may return
                             ;; completely wrong number.
                             (* n-lines (frame-char-height)))))

         (y-above (- real-y-offset
                     (+ height
                        ;; add two rows to the height
                        ;; so that the popup does not
                        ;; cover the current line
                        (* 2 (frame-char-height)))))

         (y-below (+ real-y-offset
                     ;; add a row to the height
                     ;; so that the popup does not
                     ;; cover the current line
                     (frame-char-height)))

         (corner-y (if (eq preferred-pos 'above)
                       y-above
                     y-below)))

    (if (< corner-y 0)
        (setq corner-y y-below))

    (if (> (+ corner-y height)
           (display-pixel-height))
        (setq corner-y y-above))

    (cons corner-x corner-y)))


;; shamelessly stolen from Semantic Bovinator

(eval-and-compile
  (if (fboundp 'window-inside-edges)
      ;; Emacs devel.
      (defalias 'esense-window-edges
        'window-inside-edges)
    ;; Emacs 21
    (defalias 'esense-window-edges
      'window-edges)
    ))

(defun esense-point-position ()
  "Return the location of POINT as positioned on the selected frame.
Return a cons cell (X . Y)"
  (let* ((w (selected-window))
         (f (selected-frame))
         (edges (esense-window-edges w))
         (col (current-column))
         (row (count-lines (window-start w) (point)))
         (x (+ (car edges) col))
         (y (+ (car (cdr edges)) row)))
    (cons x y)))


(defun esense-get-time ()
  "Return current time with 1 second resolution."
  (esense-convert-time (current-time)))


(defun esense-convert-time (time)
  "Convert time (which is a list of two integers containing the high and low order 16-bits of TIME) to a floating point value to make it easier to calculate time differences."
  (+ (* 65536.0 (car time)) (cadr time)))


(defun esense-expand-file-name (file)
  "Expand file name to full format."
  (expand-file-name (substitute-in-file-name file)))


(defun esense-build-index-file-name (directory subdirectory file)
  "Return the full path of the module index FILE under SUBDIRECTORY of
DIRECTORY."
  (concat (esense-expand-file-name directory)
          "/" subdirectory
          "/" file))

(defun esense-get-module-index-file-name (module)
  "Return the full path of index file of module.
If the file does not exist return the path of the possible location
for an on-demand created module index file."
  (if (listp esense-index-directory)
      (let ((dir (some (lambda (directory)
                         (let ((full-path
                                (esense-build-index-file-name
                                 directory esense-module-index-directory
                                 module)))
                           (if (file-readable-p full-path)
                               full-path)))

                       esense-index-directory)))
        (if dir
            dir
          ;; if no suitable directory was found then fallback to the
          ;; first one in the list
          (esense-build-index-file-name (car esense-index-directory)
                                        esense-module-index-directory
                                        module)))

    (esense-build-index-file-name esense-index-directory
                                  esense-module-index-directory
                                  module)))


(defun esense-get-include-index-file-name (include)
  "Return the full path of index file of an include file.
If the file does not exist return the path of the possible location
for an on-demand created module index file."
  (let ((file (mapconcat (lambda (x) x)
                         (esense-change-drive-letter include)
                         esense-path-separator-in-file-name)))

    (if (listp esense-index-directory)
        (let ((dir
               (some (lambda (directory)
                       (let ((full-path
                              (esense-build-index-file-name
                               directory esense-include-index-directory file)))
                         (if (file-readable-p full-path)
                             full-path)))

                     esense-index-directory)))

          (if dir
              dir
            ;; if no suitable directory was found then fallback to the
            ;; first one in the list
            (esense-build-index-file-name (car esense-index-directory)
                                          esense-include-index-directory
                                          file)))

      (esense-build-index-file-name esense-index-directory
                                    esense-include-index-directory
                                    file))))


(defun esense-get-file-modification-time (file)
  (esense-convert-time (sixth (file-attributes file))))


(defun esense-get-symbol-at-point ()
  "Return information about symbol at point.
The returned information is a list of
 (SYMBOL CHAR-BEFORE-SYMBOL SYMBOL-BEGINNING_POSITION AT-END CHAR-AFTER SYMBOL-END-POSITION).
AT-END is t if point is at the end of symbol."
  (let ((orig-point (point))
        (quoted-atom (eq 'atom (erlang-in-literal)))
        symbol char-before symbol-beginning_position at-end char-after symbol-end-position)
    (save-excursion
      (save-excursion
        (if quoted-atom
            (progn (search-backward "'")
                   (forward-char))
          (skip-syntax-backward "w_"))

        (setq symbol-beginning_position (point))

        ;; possible quotation mark for quoted atoms
        (skip-chars-backward "'")
        (setq char-before (char-before)))

      (if quoted-atom
          (progn
            (search-forward "'")
            (backward-char))
        (skip-syntax-forward "w_"))

      (setq at-end (= (point) orig-point))
      (setq symbol-end-position (point))
      (setq symbol (buffer-substring-no-properties symbol-beginning_position (point)))
      ;; possible quotation mark for quoted atoms
      (skip-chars-forward "'")
      (setq char-after (char-after)))
    (list symbol char-before symbol-beginning_position at-end char-after symbol-end-position)))


(defun esense-read-index-file (file type)
  "Read and return index information from FILE. TYPE can either be the
symbol `index' which means it's a previously created index file or
`source' indicating an actual source file."
  (message (format "Reading index information from file %s" file))
  (let ((result (make-esense-erlang))
        index-buffer
        kill)

    (case type
      ('index
       (setq index-buffer (get-file-buffer file))

       (unless index-buffer
         (unless (file-readable-p file)
           (error "Cannot read index file %s" file))
         (setq index-buffer (find-file-noselect file t t))
         (setq kill t))

       (with-current-buffer index-buffer
         (goto-char (point-min))
         (setf (esense-erlang-source result) (buffer-substring-no-properties
                                              (point)
                                              (point-at-eol)))
         (forward-line 1)))                  ;skip source file

      (t
       (setf (esense-erlang-source result) file)))


    (if (or (eq type 'source)
            (with-current-buffer index-buffer
              (eobp)))                  ; stub index file

          ;; run the indexing on the fly
          (with-current-buffer (get-buffer-create esense-buffer-name)
            (erase-buffer)
            (unless (= 0 (call-process
                          esense-indexer-program nil (current-buffer) nil
                          "-stdout" "-full" (esense-erlang-source result)))
              (error (concat "Cannot index source file %s from index file %s.\n"
                             "See buffer %s for possible error messages.")
                     (esense-erlang-source result) file esense-buffer-name))

            (goto-char (point-min))
            (forward-line 1)            ;skip source file

            (esense-store-index-info result))

      ;; full index file
      (with-current-buffer index-buffer
        (esense-store-index-info result)))

    (dolist (function (esense-erlang-functions result))
      ;; for those functions where function spec is empty, set it to
      ;; name/arity, because it is used at function lookup
      ;; FIXME: is this really necessary?
      (unless (esense-function-spec function)
        (setf (esense-function-spec function)
              (concat (esense-function-name function) "/"
                      (int-to-string (esense-function-arity function)))))

      ;; reverse parameter lists, so that they are in the same order
      ;; as in the documentation-getter
      (setf (esense-function-params function)
            (nreverse (esense-function-params function))))

    ;; reverse field lists lists, so that they are in the same order
    ;; as in the documentation-getter
    (dolist (record (esense-erlang-records result))
      (setf (esense-record-fields record)
            (nreverse (esense-record-fields record))))

    (if kill
        (kill-buffer index-buffer))

    (message "Done.")

    result))


(defun esense-store-index-info (result)
  "Run indexing on the file if necessary and store the read values in
RESULT. Must be called with index info in the current buffer
Should only be called from `esense-read-index-file'."
  (let (function record field macro import imported-function)
    (while (not (eobp))
      (let* (name value)
        (forward-word 1)
        (assert (= (char-after) ?:))
        (setq name (buffer-substring-no-properties (point-at-bol) (point)))

        (forward-char)
        (if (/= (char-after) esense-value-begin-end-tag)
            (setq value (buffer-substring-no-properties (point) (point-at-eol)))

          (forward-char)
          (let ((begin (point)))
            (search-forward (char-to-string esense-value-begin-end-tag))
            (setq value (buffer-substring-no-properties begin (1- (point))))))

        (if (string= name "line")
            (setq value (string-to-int value)))

        (case (intern name)
          ('function
           (setq field nil)
           (setq record nil)
           (setq imported-function nil)
           (setq function (make-esense-function))
           (push function (esense-erlang-functions result))
           (setf (esense-function-name function) value))

          ('param
           (assert function)
           (push value (esense-function-params function)))

          ('docref
           (assert function)
           (setf (esense-function-docref function) value))

          ('exported
           (assert function)
           (setf (esense-function-exported function) t))

          ('source
           (setf (esense-erlang-source result) value))

          ('record
           (setq function nil)
           (setq field nil)
           (setq record (make-esense-record))
           (push record (esense-erlang-records result))
           (setf (esense-record-name record) value))

          ('line
           (assert (or record function))
           (if record
               (setf (esense-record-line record) value)
             (setf (esense-function-line function) value)))

          ('doc
           (assert (or function record field))
           (if function
               (setf (esense-function-doc function) value)
             (if field
                 (setf (esense-record-field-doc field) value)
               (setf (esense-record-doc record) value))))

          ('field
           (assert record)
           (setq field (make-esense-record-field))
           (setf (esense-record-field-name field) value)
           (push field (esense-record-fields record)))

          ('macro
           (setq macro (make-esense-macro))
           (setf (esense-macro-name macro) value)
           (push macro (esense-erlang-macros result)))

          ('value
           (assert macro)
           (setf (esense-macro-value macro) value)
           (setq macro nil))

          ('include
           (push (cons 'include value) (esense-erlang-includes result)))

          ('includelib
           (push (cons 'include_lib value) (esense-erlang-includes result)))

          ('import
           (setq imported-function nil)
           (setq import (make-esense-import))
           (setf (esense-import-module import) value)
           (push import (esense-erlang-imports result)))

          ('name
           (assert import)
           (setq function nil)
           (setq imported-function (make-esense-imported-function))
           (setf (esense-imported-function-name imported-function) value)
           (push imported-function (esense-import-functions import)))

          ('arity
           (assert (or function imported-function))
           (if imported-function
               (setf (esense-imported-function-arity imported-function)
                     (string-to-int value))

             (setf (esense-function-arity function) (string-to-int value))))

          ('spec
           (assert function)
           (setf (esense-function-spec function) value))

          (t
           (assert nil))))

      (forward-line 1))))


(defun esense-read-include-file (include &optional file)
  "Read include file INCLUDE and return its contents.
Note that the INCLUDE is given in the format as in `esense-include-files'.

If optional argument FILE is given then module info is read from the
specified source file, instead of looking for a previously generated
index file."
  (let ((result (esense-cache-get include)))
    (unless result
      (setq result
            (cond (file
                   (esense-read-index-file file 'source))

                  ((let ((indexfile
                          (esense-get-include-index-file-name include)))
                     (if (file-exists-p indexfile)
                         (esense-read-index-file indexfile 'index))))

                  (t
                   (let ((file (esense-search-include include)))
                     (esense-read-index-file file 'source)))))

      (esense-cache-add include result))

    result))


(defun esense-search-include (include &optional include_lib)
  "Return the path to the include source file if it is found in
`esense-include-search-directories' or nil otherwise.

If INCLUDE_LIB is t then the header is included with -include_lib.

Note that INCLUDE is given in the format as in
`esense-include-files'.
"
  (if esense-include-search-directories
      (let* ((file (mapconcat (lambda (x) x) (reverse include) "/"))
             (len (length file))
             (basename (file-name-nondirectory file)))

        (if include_lib
            (let* ((dirname (reverse (cdr include)))
                   (pattern (concat (car dirname)
                                    "\\(-[0-9]+.[0-9]+\\(.[0-9]+\\)?\\)?/"
                                    (mapconcat 'identity (cdr dirname) "/"))))
              (some (lambda (directory)
                      (message "Searching include lib %s in directory %s ..."
                               basename directory)
                      (if (string-match pattern directory)
                          (let ((fullpath (concat directory "/" basename)))
                            (if (file-readable-p fullpath)
                                fullpath))))
              (cons default-directory
                    (esense-get-cached-search-directories
                     'esense-include-search-directories))))

          (some (lambda (directory)
                  (message "Searching include %s in directory %s ..."
                           basename directory)
                  (let ((fullpath (concat directory "/" basename)))
                    (if (and (file-readable-p fullpath)
                             (let ((min (min len (length fullpath))))
                               (compare-strings fullpath
                                                (- (length fullpath) min)
                                                (length fullpath)
                                                file (- len min) len)))
                        fullpath)))
                (cons default-directory
                      (esense-get-cached-search-directories
                       'esense-include-search-directories)))))))


(defun esense-read-module-file (module &optional file)
  "Read index file of MODULE and return its contents.

If optional argument FILE is given then module info is read from the
specified source file, instead of looking for a previously generated
index file."
  (let ((result (esense-cache-get module)))
    (unless result
      (setq result
            (cond
             (file
              (esense-read-index-file file 'source))

             ((let ((indexfile (esense-get-module-index-file-name module)))
                (if (file-exists-p indexfile)
                    (esense-read-index-file indexfile 'index))))

             (t
              (let ((file (esense-search-module module)))
                (esense-read-index-file file 'source)))))

      (setf (esense-erlang-modulename result) module)
      (esense-cache-add module result))
    result))


(defun esense-search-module (module)
  "Return the path to the module source file if it is found in
`esense-module-search-directories' or nil otherwise."
  (if (or esense-module-search-directories
          esense-otp-module-search-directories
          esense-otp-html-module-search-directories)
      (let ((erlangfile (concat module ".erl")))
        (some (lambda (info)
                (destructuring-bind (filename directories) info
                  (some (lambda (directory)
                          (message "Searching module %s in directory %s ..."
                                   module directory)
                          (let ((fullpath (concat directory "/" filename)))
                            (if (file-readable-p fullpath)
                                fullpath)))
                        directories)))
                (list
                 (list erlangfile
                       (cons default-directory
                             (esense-get-cached-search-directories
                              'esense-module-search-directories)))

                 (list (concat module ".html")
                       (esense-get-cached-search-directories
                        'esense-otp-html-module-search-directories))

                 (list erlangfile
                       (esense-get-cached-search-directories
                        'esense-otp-module-search-directories)))))))


(defun esense-get-cached-search-directories (sourcevar)
  "Return cached directories for the given source variable SOURCEVAR."
  (let ((cachevar (intern (concat (symbol-name sourcevar) "-cache")))
        (prevvar (intern (concat (symbol-name sourcevar) "-prev"))))
    (unless (and (boundp cachevar)
                 (eq (symbol-value prevvar) (symbol-value sourcevar)))
      (message "Caching search directories ...")
      (set cachevar
           (apply 'append (mapcar (lambda (directory)
                                    (file-expand-wildcards directory))
                                  (symbol-value sourcevar))))
      (set prevvar (symbol-value sourcevar)))
    (symbol-value cachevar)))


(defun esense-function-name-p (name)
  "Return the position of the function indicator character if NAME is a function header,
nil otherwise."
  (or (string-match "(" name)
      (string-match "/" name)))


(defun esense-get-include-files ()
  "Return name of included files in the current module."
  (let (includes)
    (save-excursion
      (goto-char (point-min))
      (while (re-search-forward
              esense-include-regexp nil t)
        (setq includes
              (adjoin (esense-get-include-file) includes :test 'equal))))
    includes))


(defun esense-get-include-file ()
  "Return include file data from the current line.
This functions should be called when the current line is already macthed
with `esense-include-regexp'."
  (let ((type (match-string-no-properties 1))
        (file (match-string-no-properties 3)))
    (cons
     (if (equal type "include")
         'include
       (if (equal type "include_lib")
           'include_lib
         (assert nil t "Unknown include file type: %s" type)))
     file)))


(defun esense-get-include-data (&optional file-list norecursion)
  "Return included file data from the current buffer.
If FILE-LIST is given then return include information for the
given files.

Do not process included files recusively if NORECURSION is t."
  (if (and esense-cached-includes
           (eq (car esense-cached-includes) (buffer-modified-tick)))
      (let ((include-infos (cdr esense-cached-includes)))
        (if (not file-list)
          include-infos

          ;; Best effort filtering: only the basename of files are
          ;; compared to the cached information. It's not a flawless
          ;; solution, but hopefully in practice it will be enough.
          (remove-if-not (lambda (include-info)
                           (some (lambda (file-info)
                                   (equal
                                    (file-name-nondirectory (cdr file-info))
                                    (file-name-nondirectory
                                     (esense-erlang-source include-info))))
                                 file-list))
                         include-infos)))

    (let ((files (if file-list
                     file-list
                   (esense-get-include-files)))
          unknown-files
          includes)

      (setq files (mapcar (lambda (file)
                            (let ((type (car file))
                                  (filename (cdr file)))
                              (setq filename (substitute-in-file-name filename))
                              (if (esense-string-begins-with filename "../")
                                  (setq filename
                                        (expand-file-name
                                         (concat default-directory filename))))

                              (let ((split-name (split-string filename "/")))
                                (push (list (reverse split-name) filename type)
                                      unknown-files)
                                (setq filename (reverse split-name))

                                (case type
                                  ('include
                                   (list type filename))
                                  ('include_lib
                                   (list type filename (reverse (cdr split-name))
                                         (car split-name)))))))
                          files))

      (dolist (include esense-include-files)
        (dolist (file files)
          (let ((type (car file))
                (filename (second file)))

            (when (or (every (lambda (x y)
                               (equal x y))
                             include filename)
                      (when (eq type 'include_lib)
                        (let ((filename_nolib (third file))
                              (lib (fourth file)))

                          (assert filename_nolib)
                          (assert lib)

                          ;; try to find a versioned include match
                          (when (every (lambda (x y)
                                         (equal x y))
                                       include filename_nolib)
                            ;; get parent directory which may contain
                            ;; the lib
                            (let* ((parent (nthcdr (1+ (length filename_nolib))
                                                   include))
                                   (directory (mapconcat
                                               (lambda (x) x)
                                               (reverse (esense-change-drive-letter parent))
                                               "/")))

                              (unless (eq window-system 'w32)
                                (setq directory (concat "/" directory)))

                              ;; check if there is a versioned subdirectory
                              ;; for lib in the directory
                              (let* ((versioned-lib
                                      (directory-files
                                       directory
                                       nil
                                       (concat lib "-[0-9]+.[0-9]+.[0-9]+")
                                       t)))
                                (when versioned-lib
                                  (assert (= (length versioned-lib) 1))
                                  ;; check if the found directory is the same
                                  ;; as the currently checked include file
                                  (equal include
                                         (append filename_nolib
                                                 versioned-lib
                                                 parent)))))))))

              (setq unknown-files
                    (delete-if (lambda (fileinfo)
                                 (equal (car fileinfo) filename))
                               unknown-files))

              (let ((include-data (esense-read-include-file include)))

                (setq includes
                      (adjoin include-data includes
                              :test (lambda (x y)
                                      (equal
                                       (esense-erlang-source x)
                                       (esense-erlang-source y))))))))))

      ;; check the search directories for the unknown files
      ;; FIXME: include_lib
      (if esense-include-search-directories
          ;; a copy is made, because I don't know if the list can be
          ;; modified while it's being iterated over
          (let ((files (copy-sequence unknown-files)))
            (dolist (fileinfo files)
              (destructuring-bind (split-name filename type) fileinfo
                (let ((file (esense-search-include split-name
                                                   (eq type 'include_lib))))
                  (if file
                      (let* ((full-split-name (nreverse (split-string file "/")))
                             (include-data
                              (esense-read-include-file full-split-name file)))
                        (push full-split-name esense-include-files)
                        (setq includes
                              (adjoin include-data includes
                                      :test (lambda (x y)
                                              (equal
                                               (esense-erlang-source x)
                                               (esense-erlang-source y)))))

                        (setq unknown-files
                              (delete-if (lambda (fileinfo)
                                           (equal (car fileinfo) split-name))
                                         unknown-files)))))))))

      ;; process included files recursively
      (unless norecursion
        (dolist (include-data includes)
          (let ((child-includes (esense-erlang-includes include-data)))
            (if child-includes
                (setq includes
                      (nunion includes
                              (esense-get-include-data child-includes)
                              :test (lambda (x y)
                                      (equal
                                       (esense-erlang-source x)
                                       (esense-erlang-source y)))))))))


      ;; Warn the user about unknown files
      (if unknown-files
          (let ((filenames
                 (mapcar (lambda (fileinfo)
                           (destructuring-bind (dummy filename type) fileinfo
                               filename))
                         unknown-files)))
            (if (not esense-ignore-unknown-includes)
                (signal 'esense-error
                        (format "Include file %s not found."
                                (car filenames)))

              (dolist (file filenames)
                (message (format "Include file %s not found."
                                 file)))
              (message (concat "Some include files could not be found. "
                               "See the *Messages* buffer for details.")))))

      ;; Cached includes are stored only if info on all the included
      ;; files were requested. Otherwise if a single include file is
      ;; requested, only its information is stored and returned, even
      ;; if more would be needed later.
      (unless file-list
        (setq esense-cached-includes (cons (buffer-modified-tick) includes)))

      includes)))


(defun esense-get-function-data (function &optional module arity)
  "Return ((FUNCTIONS-INFO . FILE_INFO) ...) list for all functions with
name FUNCTION."
  (let ((qualified-function-names
         (mapcar (lambda (ending)
                   (concat function ending))

                 (if (esense-function-name-p function)
                     '("") ; no need to append anything to the function name
                   '("(" "/")))))

    (if module
        (let ((result (esense-get-function-data-from-file
                       (esense-lookup-module module)
                       qualified-function-names arity)))
                       ;; filtering out non-exported
                       ;; functions here would break the feature
                       ;; for jumping to a specific function
                       ;; clause from a trace buffer, because
                       ;; the trace can contain non-exported
                       ;; functions too
          (if result
              (list result)))

      ;; check if there is a matching function in the "erlang" module
      (let* ((result (esense-get-function-data-from-file
                      (esense-lookup-module "erlang")
                      qualified-function-names arity t)))

        (if result
            (list result)

          ;; check the imported modules
          (let ((module-info (esense-get-current-module-info)))
            (setq result
                  (if module-info
                       (some (lambda (import)
                               (and
                                ;; check if this import matches the function name
                                ;; and arity
                                (some (lambda (imported-function)
                                        (and (equal function
                                                    (esense-imported-function-name imported-function))
                                             (or (not arity)
                                                 (eq (esense-imported-function-arity imported-function)
                                                     arity))))
                                      (esense-import-functions import))

                                ;; if so then return the matching functions from the module
                                (esense-get-function-data-from-file
                                 (esense-lookup-module (esense-import-module import))
                                 qualified-function-names arity t)))

                             (esense-erlang-imports module-info))))

            (if result
                (list result)

              ;; check the current file and the included files
              (if arity
                  ;; if arity is given we can stop at the first exact match
                  ;; found
                  (progn
                    (setq result (esense-collect-available-data
                                  (lexical-let ((filters qualified-function-names)
                                                (arity arity))
                                    (lambda (file)
                                      (esense-get-function-data-from-file
                                       file filters arity)))))
                    (if result
                        (list result)))

                (delete-if 'null
                           (mapcar (lambda (file)
                                     (esense-get-function-data-from-file
                                      file qualified-function-names arity))
                                   (esense-collect-available-data)))))))))))


(defun esense-get-function-data-from-file (file function-filters
                                                &optional arity exported-only)
  "Return a (FUNCTIONS-INFO . FILE_INFO) list if index FILE has functions
which match FUNCTION-FILTERS and the optional arity.

This function should only be called by `esense-get-function-data'.

If EXPORTED-ONLY is t then only exported functions are returned."
  (let ((aritystr (if arity (concat "/" (int-to-string arity))))
        functions)

    (dolist (function-data (esense-erlang-functions file))
      (let ((name (esense-function-spec function-data)))
        (if (some (lambda (function-filter)
                    (esense-string-begins-with name function-filter))
                  function-filters)

            (if (and
                 ;; check if arity matches if given
                 (or (not arity)
                     (string-match aritystr name)
                     (eq arity (esense-get-arity-from-string name)))

                 ;; check if the function is exported if needed
                 (or (not exported-only)
                     (esense-function-exported function-data)))
                (push function-data functions)))))

    (if functions
        (cons functions file))))


(defun esense-get-record-data (record)
  "Return ((RECORD-INFO . FILE_INFO) ...) information about RECORD.
If RECORD is not found an error is signalled."
  (let (result)
    (dolist (records-info (esense-collect-record-data))
      (some (lambda (record-data)
              (if (equal (esense-record-name record-data) record)
                  (push (cons record-data (cdr records-info))
                        result)))
            (car records-info)))

    (if result
        result
      (signal 'esense-error
              (format "Record %s is not found." record)))))


(defun esense-collect-record-data ()
  "Collect all available record information from the current file and included
files and return it as a list ((RECORD-INFO . FILE_INFO)...)."
  (mapcar (lambda (file)
            (cons (esense-erlang-records file)
                  (esense-erlang-source file)))

          (esense-collect-available-data)))


(defun esense-get-macro-data (macro)
  "Return ((MACRO-INFO . FILE_INFO)...) information about MACRO.
An error is signalled if there is no such macro."
  (let (result)
    (dolist (macros-info (esense-collect-macro-data))
      (some (lambda (macro-info)
              (if (equal (esense-strip-function (esense-macro-name macro-info))
                         macro)
                  (push (cons macro-info (cdr macros-info))
                        result)))
            (car macros-info)))

    (if result
        result
      (error "Macro %s is not found." macro))))


(defun esense-collect-macro-data ()
  "Collect all available macro information from the current file and included
files and return it as a list ((MACRO-INFO . FILE_INFO)...)."
  (let ((macros-info-list
         (mapcar (lambda (file)
                   (cons (esense-erlang-macros file)
                         (esense-erlang-source file)))

                 (esense-collect-available-data))))

    ;; predefined macros
    (push (cons esense-predefined-macros nil) macros-info-list)

    macros-info-list))


(defun esense-collect-available-data (&optional predicate)
  "Collect all available information from the current file and included
files and return it as a list.

If optional PREDICATE function is given then it is called at each
stage of data collection and the collection process stops as soon
as PREDICATE returns a non-nil value. This value will also be the
function's return value.

PREDICATE should accept one argument, the index file to be tested."
  (let (result)
    ;; if the currently edited file is a module then retrieve its
    ;; index information
    (let ((current-module (esense-current-file-is-a-module-on-disk-p)))
      (if current-module
          (let ((module-info (esense-lookup-module current-module)))
            (if predicate
                (setq result (funcall predicate module-info))
              (push module-info result)))))

    (if (and predicate result)
        result

      ;; if the currently edited file is an include file save to disk
      ;; then retrieve its index information
      (let ((current-include
             (and (equal "hrl" (file-name-extension (buffer-file-name)))
                  (file-exists-p (buffer-file-name))
                  (nreverse (split-string
                             (expand-file-name (buffer-file-name)) "/")))))
        (when current-include
          (unless (member current-include esense-include-files)
              (push current-include esense-include-files))

          (let ((include-info (esense-read-include-file current-include)))
            (if predicate
                (setq result (funcall predicate include-info))
              (push include-info result)))))

      (if (and predicate result)
          result

        (let ((includes (esense-get-include-data)))
          (if predicate
              (some (lambda (file)
                      (funcall predicate file))
                    includes)

            (nconc result includes)))))))


(defun esense-cache-add (name value)
  "Add NAME/VALUE pair to the cache."
  (push (list name value (esense-get-time))
        esense-cache)
  (if (> (length esense-cache)
         esense-max-cache-size)
      (nbutlast esense-cache)))


(defun esense-cache-get (name)
  "Return value for NAME or nil if there is no such value.

If the value is in the cache but its timestamp is older than
the modification time of the source file belonging to value
then remove the item from the cache and return nil to force
reloading it from the disk."
  (let ((item (assoc name esense-cache)))
    (if (not item)
        nil

      (setq esense-cache (delete item esense-cache))

      (let ((value (second item))
            (timestamp (third item)))
        (when (< (esense-get-file-modification-time (esense-erlang-source value))
                 timestamp)
          (push item esense-cache)
          value)))))


(defun esense-format-documentation (entries)
  "Return formatted version of a list of documentation entries.
ENTRIES is a list of ((HEADER . DESCRIPTION) ...) entries.
The description will be indented after the header(s).
The entries will be separated by newlines.
Headers with common documentation are grouped together."

  ;; group headers with common documentation
  (let ((old-entries entries)
        unique-docs)
    (dolist (entry entries)
      (setq unique-docs
            (adjoin (cdr entry)
                    unique-docs
                    :test 'equal)))

    (setq entries nil)
    (dolist (doc unique-docs)
      (let (headers)
        (dolist (entry old-entries)
          (if (equal (cdr entry) doc)
              (push (car entry) headers)))
        (assert headers)
        (push (cons headers doc) entries))))

  ;; format documentation
  (mapconcat
   (lambda (entry)
     (let ((header (car entry))
           (doc (cdr entry))
           (padding "  ")
           (pos -1))

       ;; add padding
       (while (setq pos (string-match "\n" doc (1+ pos)))
         (setq doc (concat (substring doc 0 (1+ pos))
                           padding
                           (substring doc (1+ pos)))))

       (concat
        (propertize
         (mapconcat (lambda (line) line)
                    header "\n")
         'face 'esense-tooltip-header-face)
        (propertize (concat "\n" padding doc) 'face esense-tooltip-face))))
   entries
   (propertize "\n\n" 'face esense-tooltip-face)))


(defun esense-truncate-path (path &optional length)
  "If PATH is too long truncate some components from the beginning."
  (let ((maxlength (if length
                       length
                     70)))
    (if (<= (length path) maxlength)
        path

      (let* ((components (reverse (split-string path "/")))
             (tmppath (car components)))
        (setq components (cdr components))

        (while (and components
                    (< (length tmppath) maxlength))
          (setq path tmppath)
          (setq tmppath (concat (car components)
                                "/"
                                tmppath))
          (setq components (cdr components)))

        (concat ".../" path)))))


(defun esense-completing-read (&rest args)
  "Same as completing-read, but completes list of candidates immediately."
  (let ((unread-command-events (cons ?\t (cons ?\t unread-command-events))))
    (apply 'completing-read args)))


(defun esense-get-current-module-name ()
  "Return the module name of the current file or nil if it is not
a module."
  (save-excursion
    (goto-char (point-min))
    (if (re-search-forward esense-module-regexp nil t)
        (match-string-no-properties 1))))


(defun esense-get-current-module-info ()
  "Return the module information for the current file or nil if it is not
a module."
  (let ((current-module (esense-current-file-is-a-module-on-disk-p)))
    (if current-module
        (esense-lookup-module current-module))))


(defun esense-current-file-is-a-module-on-disk-p ()
  "Return the module name if the currently edited file is a module and
it is saved to the disk."
  (and (file-exists-p (buffer-file-name))
       (esense-get-current-module-name)))


(defun esense-include-index-file-transformer (item)
  "Transform index file name for include file"
  (let ((split-name (split-string item esense-path-separator-in-file-name)))
    (if (eq window-system 'w32)
        (let* ((rev-split-name (reverse split-name))
               (drive-part (car rev-split-name))
               (drive-letter (concat (substring drive-part 0 1)
                                     ":")))
          (reverse (cons drive-letter (cdr rev-split-name))))
      split-name)))


(defun esense-change-drive-letter (item)
  "Change drive letter if OS is Windows"
  (if (eq window-system 'w32)
      (let* ((rev-item (reverse item))
             (drive-part (car rev-item))
             (drive-letter (concat (substring drive-part 0 1)
                                   "_")))
        (reverse (cons drive-letter (cdr rev-item))))
    item))


(defun esense-get-number-of-function-arguments ()
  "Return the number of arguments of function at point
or nil if the function has no parameter list."
  (save-excursion
    (cond
     ((progn (skip-syntax-forward "w_-")
             (looking-at "/\\([0-9]+\\)"))
      (string-to-int (match-string-no-properties 1)))

     ((progn (skip-syntax-forward "w_-.")
             (looking-at "("))
      (forward-char)
      (skip-syntax-forward "-")
      (if (looking-at ")")
          0

        (condition-case nil
            (let ((numargs 0))
              (while
                  (progn
                    (while (not (or (looking-at ",")
                                    (looking-at ")")
                                    (eobp)))
                      (if (looking-at "<")
                          (forward-sexp)
                        (forward-sexp))
                      (skip-syntax-forward "-"))
                    (incf numargs)
                    (when (looking-at ",")
                      (forward-char)
                      (skip-syntax-forward "-")
                      t)))

              (if (eobp)
                  ;; reached the end of buffer while parsing
                  ;; the arguments
                  nil
                numargs))
          ;; XEmacs doesn't know scan-error, so we have to catch
          ;; 'error which is a pity, because it swallows all
          ;; kinds of errors, not just the ones we want to catch :(
          ((scan-error error) nil)))))))


(defun esense-show-number-of-function-arguments ()
  "Show number of arguments of function the point is on
in the echo area."
  (interactive)
  (when (eq major-mode 'erlang-mode)
    (let ((numargs (esense-get-number-of-function-arguments)))
      (if numargs
          (message (format "Number of arguments: %d" numargs))))))



(defun esense-resolve-macro-value (value)
  "Try to resolve macro VALUE and return a string which is appended
to the macro documentation."
  (if (not value)
      "(complex value, see the definition)"

    (concat
     value
     (cond
      ;; integer value
      ((every (lambda (c)
                (and (>= c ?0) (<= c ?9)))
              value)

       (condition-case nil
           (concat
            " "
            ;; show integer values in hex
            (format "(16#%x" (string-to-number value))
            ;; and in binary (adapted from calculator.el)
            (let ((str (format "%o" (string-to-number value)))
                  (i -1) (s ""))
              (while (< (setq i (1+ i)) (length str))
                (setq s
                      (concat s
                              (cdr (assq (aref str i)
                                         '((?0 . "000") (?1 . "001")
                                           (?2 . "010") (?3 . "011")
                                           (?4 . "100") (?5 . "101")
                                           (?6 . "110") (?7 . "111")))))))

              (concat ", 2#" s ")")))

         (range-error
          " (integer too big for conversion)")))

      ;; defined as an other macro
      ((eq (aref value 0) ??)
       ;; make sure there is no error if an include file is not found
       (let ((esense-ignore-unknown-includes t))
         (condition-case nil
             (let ((macro-infos (esense-get-macro-data (substring value 1))))
               (if (second macro-infos)
                   " (multiple candidates)"

                 (let ((name (esense-macro-name (caar macro-infos)))
                       (value (esense-macro-value (caar macro-infos))))
                 (concat " = " (esense-resolve-macro-value value)))))
        (esense-error
         " (not found)"))))

      (t
       "")))))


(defun esense-get-function-invocation (&optional openbracket closebracket)
  "Return the argument list of a function invocation at point or nil
if no invocation is found.

If OPENBRACKET is given then CLOSEBRACKET must also be given.
Bracket defaults to `(' if not given."
  (save-excursion
    (skip-syntax-forward "w_-\"")
    (if (eq (char-after) (if openbracket
                             openbracket
                           ?\())
        (condition-case nil
            (let ((begin (point)))
              (forward-sexp)
              (let ((args (buffer-substring-no-properties begin (point))))
                (if openbracket
                  (concat "(" (substring args 1 -1) ")")
                  args)))
          ;; XEmacs doesn't know scan-error, so we have to catch
          ;; 'error which is a pity, because it swallows all
          ;; kinds of errors, not just the ones we want to catch :(
          ((scan-error error) nil)))))


(defun esense-find-function-for-invocation (function invocation)
  "Find the FUNCTION matching the given INVOCATION pattern.
Point must be on the first function clause with the matching arity."
  (let ((num-of-clauses 0))

    (with-current-buffer (get-buffer-create esense-secondary-buffer-name)
      (erase-buffer))

    ;; copy the function clauses with the same arity
    (save-excursion
      (let ((parent (current-buffer))
            begin end)

        (loop do
              (setq begin (point))
              (setq end (save-excursion
                          (search-forward "->")))

              (with-current-buffer (get-buffer esense-secondary-buffer-name)
                (insert-buffer-substring parent begin end)
                (insert "\n")
                (insert (int-to-string num-of-clauses) ";\n")
                (incf num-of-clauses))

              until (progn
                      (erlang-end-of-clause)
                      (= (char-before) ?.)))))

    (when (> num-of-clauses 1)
      ;; fix ending of last clause
      (with-current-buffer (get-buffer esense-secondary-buffer-name)
        (delete-char -2)
        (insert ".\n"))

      (esense-resolve-entities-for-invocation-analysis)

      (message "Creating test program...")

      (let* ((module (make-temp-name "esenseinvocationtest"))
             (testfunc "esenseinvocationtestfunc")
             (filename (concat (esense-get-temporary-directory) module))
             (sourcefile (concat filename ".erl"))
             (beamfile (concat filename ".beam")))
        (with-current-buffer (get-buffer esense-secondary-buffer-name)
          (goto-char (point-min))
          (insert "-module('" module "').\n"
                  "-export([" testfunc "/0]).\n"
                  testfunc "() ->\n"
                  "catch(" function invocation ").\n\n")

          (goto-char (point-min))
          (while (re-search-forward "\\(#[a-zA-Z]+?\\)?<[0-9.]*?>" nil t)
            (replace-match "i_dont_know_yet_what_to_do_with_these"))

          ;; function objects are replaced with a dummy atom
          (goto-char (point-min))
          (while (re-search-forward "\\(#Fun<[^>]*?>\\)" nil t)
            (replace-match "fun_object"))

          (write-region (point-min) (point-max) sourcefile)
          (kill-buffer nil))

        (message "Compiling and running test program...")

        (let ((result (esense-interpret-string (concat "c(" module ")"))))
          (delete-file sourcefile)

          (if (not (string-match "^{ok," result))
            (message (format (concat "Cannot compile test program. "
                                     "See buffer %s for possible error messages.")
                             esense-interpreter-buffer))

            (delete-file beamfile)

            (let ((result (esense-interpret-string
                           (concat module ":" testfunc "()"))))

              (if (not (string-match "^[0-9]+" result))
                  (message (concat "Cannot find function matching the invocation. "
                                   "Choosing the first one instead."))

                (setq result (1+ (string-to-int result)))
                (dotimes (i result)
                  (erlang-end-of-clause))
                (erlang-beginning-of-clause)
                (message "Matching function found.")

                (esense-interpret-string
                 (concat "code:delete(" module ")"))))))))))


(defun esense-resolve-entities-for-invocation-analysis (&optional bound
                                                                  records
                                                                  macros)
  "Resolve macro and record refences for invocation analysis.

BOUND limits the search to the part of the buffer which is not processed yet.

RECORDS and MACROS are lists of already resolved records and macros."

  (let ((newbound
         (with-current-buffer (get-buffer esense-secondary-buffer-name)
           (point-min-marker))))

    (set-marker-insertion-type newbound t)

    (message "Resolving record references...")

    (setq records
          (nconc records
                 (esense-resolve-entity-for-invocation-analysis
                  (concat "[^0-9]#" erlang-atom-regexp
                          "\\|record([^,]+, *" erlang-atom-regexp ")")
                  'esense-get-record-data
                  (lambda (record)
                    (goto-line (esense-record-line record)))
                  bound
                  records)))

    (message "Resolving macro references...")

    (setq macros
          (nconc macros
                 (esense-resolve-entity-for-invocation-analysis
                  (concat "?" erlang-atom-regexp)
                  'esense-get-macro-data
                  (lambda (macro)
                    (goto-char (point-min))
                    (re-search-forward (format esense-macro-regexp
                                               (esense-macro-name macro))))
                  bound
                  macros)))

    ;; resolve entities recursively if necessary
    (unless (with-current-buffer (get-buffer esense-secondary-buffer-name)
              (eq (marker-position newbound) (point-min)))
      (esense-resolve-entities-for-invocation-analysis newbound records macros))
    ;; delete marker
    (set-marker newbound nil)))


(defun esense-resolve-entity-for-invocation-analysis
  (regexp lookup-func go-to-def-func bound processed)
  "Try resolving references to external entities in the test program assembled
in the ESense buffer. Return the list of resolved entities.

The entities are found by searching for REGEXP. The first match string
data is used as the name of the entity. If it is nil the second is used.

LOOKUP-FUNC is a function for getting index information about the entity.

GO-TO-DEF-FUNC is a function for jumping to definition of the entity within
the source file.

BOUND limits the search to the part of the buffer which is not processed yet.

PROCESSED is a list of already resolved entities."
  (let ((entities '())
        entity)
    (with-current-buffer (get-buffer esense-secondary-buffer-name)
      (goto-char (point-min))
      (while (re-search-forward regexp bound t)
        (setq entity (or (match-string-no-properties 1)
                         (match-string-no-properties 2)))
        (unless
            (or
             ;; check if the entity is processed already
             (member entity processed)
             ;; check if the entity is within a comment
             ;; (simplistic approach)
             (save-excursion
               (search-backward "%" (point-at-bol) t)))
          (setq entities (adjoin
                          ;; quotes around the atom are removed
                          (if (eq (aref entity 0) ?')
                              (substring entity 1 -1)
                            entity)
                          entities :test 'equal)))))

    ;; identify the files containing the definitions
    (let ((files '()))
      (dolist (entity entities)
        ;; if more than one include files found containing
        ;; an entity with this name then the first one is chosen
        (let* ((entity-data (car (funcall lookup-func entity)))
               (entity (car entity-data))
               (filename (cdr entity-data))
               (file (assoc filename files)))
          (if file
              (push entity (cdr file))
            (push (list filename entity) files))))

      ;; copy the entity definitions from the files
      (dolist (file files)
        (let* ((filename (car file))
               (entities (cdr file))
               (buffer (get-file-buffer filename))
               kill)
          (unless buffer
            (setq buffer (find-file-noselect filename t t))
            (setq kill t))

          (with-current-buffer buffer
            (save-excursion
              (dolist (entity entities)
                (funcall go-to-def-func entity)
                (beginning-of-line)
                (let ((begin (point-at-bol)))
                  (while (not (eq (char-after) ?.))
                    (forward-sexp))
                  (let ((definition
                          (buffer-substring-no-properties
                           begin (point-at-eol))))
                    (with-current-buffer (get-buffer esense-secondary-buffer-name)
                      (goto-char (point-min))
                      (insert definition "\n")))))))

          (if kill
              (kill-buffer buffer)))))

    entities))


(defun esense-get-arity-from-string (arguments)
  "Return the arity of a function argument list represented as
a string."
  (if arguments
      (with-temp-buffer
        (erase-buffer)
        (insert arguments)
        (goto-char (point-min))
        (esense-get-number-of-function-arguments))))


(defun esense-load-index-information-for-current-file ()
  "Preload index information if the current file is an Erlang file."
  (when (and (eq major-mode 'erlang-mode)
             ;; no completion in progress
             (not esense-completion-list))
    (let ((esense-ignore-unknown-includes t))
      (esense-collect-available-data))))


(defun esense-get-temporary-directory ()
  "Return the directory for temporary files."
  (if esense-xemacs
      (concat (temp-directory) "/")
    temporary-file-directory))


(defun esense-ensure-interpreter-is-running ()
  "Ensure the Erlang interpreter is running in the background,
so that queries can be submitted to it."
  (unless (and esense-interpreter-process
               (eq (process-status esense-interpreter-process) 'run))

    (message "Starting Erlang interpreter subprocess...")

    ;; make sure the current directory of the interpreter
    ;; is the temporary directory, so that generated files
    ;; (e.g. in case of compilation) go there
    (let ((default-directory (esense-get-temporary-directory)))
      (setq esense-interpreter-process
            (start-process "esense-interpreter-process"
                           esense-interpreter-buffer
                           esense-erlang-interpreter-program)))

    (unless (eq (process-status esense-interpreter-process) 'run)
      (error "Cannot start Erlang interpreter subprocess."))

    (with-current-buffer esense-interpreter-buffer
      (esense-wait-for-interpreter-results
       (lambda ()
         (equal (buffer-substring (max 1 (- (point-max) 3))
                                  (point-max))
                "1> "))))))


;; it will do until I have the time to talk to the node directly (Distel?)
(defun esense-interpret-string (str)
  "Interpret STR by submitting it to the Erlang interpreter subprocess and
return its result."
  (esense-ensure-interpreter-is-running)
  (with-current-buffer esense-interpreter-buffer
    (erase-buffer))

  (process-send-string
   esense-interpreter-buffer
   (concat "io:format(\"@OUTPUT~n\"),"
           "io:format(\"@RESULT~n~p~n@END~n\", "
           "[catch(" str ")]).\n"))

  (with-current-buffer esense-interpreter-buffer
    (esense-wait-for-interpreter-results
     (lambda ()
       (re-search-backward "^@END" nil t)))

    ;; extract result
    (let ((end (1- (point))))
      (re-search-backward "^@RESULT")
      (forward-line)
      (buffer-substring (point) end))))


(defun esense-wait-for-interpreter-results (condition)
  "Wait for Erlang interpreter results by checking CONDITION
periodically.

Signal an error if the result does not arrive within the timeout period."
  (accept-process-output)
  (let ((start (esense-get-time)))
    (while (not (funcall condition))
      ;; timeout after 5 seconds
      (if (> (- (esense-get-time) start) 5)
          (error "Timeout when waiting for Erlang interpreter results.")
        (accept-process-output nil nil 100)))))


(defun esense-find-file (file)
  "Visit FILE optionally in a new window."
  ;; currently it is used only by `esense-trace-mode', that's
  ;; why the hackish solution
  (funcall (if (and (eq major-mode 'esense-trace-mode)
                    esense-trace-show-function-in-new-window)
               'find-file-other-window
             'find-file)
           file))

;----------------------------------------------------------------------
;
; XEmacs compatibility
;

(when esense-xemacs
  (unless (fboundp 'match-string-no-properties)
    (defalias 'match-string-no-properties 'match-string))
  (defalias 'frame-char-width 'frame-width)
  (defalias 'frame-char-height 'frame-height)

  (unless (fboundp 'propertize)
    (defun propertize (string &rest props)
      string))

  (unless (fboundp 'compare-strings)
    ;; copied from an old XEmacs CVS repository
    (defun compare-strings (str1 start1 end1 str2 start2 end2 &optional ignore-case)
      "Compare the contents of two strings, converting to multibyte if needed.
In string STR1, skip the first START1 characters and stop at END1.
In string STR2, skip the first START2 characters and stop at END2.
END1 and END2 default to the full lengths of the respective strings.

Case is significant in this comparison if IGNORE-CASE is nil.
Unibyte strings are converted to multibyte for comparison.

The value is t if the strings (or specified portions) match.
If string STR1 is less, the value is a negative number N;
  - 1 - N is the number of characters that match at the beginning.
If string STR1 is greater, the value is a positive number N;
  N - 1 is the number of characters that match at the beginning."
      (if (null start1)
          (setq start1 0))
      (if (null start2)
          (setq start2 0))
      (setq end1 (if end1
                     (min end1 (length str1))
                   (length str1)))
      (setq end2 (if end2
                     (min end2 (length str2))
                   (length str2)))
      (let ((i1 start1)
            (i2 start2)
            result c1 c2)
        (while (and (not result) (< i1 end1) (< i2 end2))
          (setq c1 (aref str1 i1)
                c2 (aref str2 i2)
                i1 (1+ i1)
                i2 (1+ i2))
          (if ignore-case
              (setq c1 (upcase c1)
                    c2 (upcase c2)))
          (cond ((< c1 c2)
                 (setq result (- i1)))
                ((> c1 c2)
                 (setq result i1))))

        (if (null result)
            (setq result
                  (cond ((< i1 end1)
                         (1+ (- i1 start1)))
                        ((< i2 end2)
                         (1- (- start1 i1)))
                        (t
                         t))))
        result))))


(provide 'esense)
;;; esense.el ends here
