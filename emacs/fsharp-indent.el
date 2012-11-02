;; Indentation rules for F#
;; Based on python-mode

(require 'comint)
(require 'custom)
(require 'cl)
(require 'compile)


;; user definable variables
;; vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv

(defgroup fsharp nil
  "Support for the Fsharp programming language, <http://www.fsharp.net/>"
  :group 'languages
  :prefix "fsharp-")

(defcustom fsharp-tab-always-indent t
  "*Non-nil means TAB in Fsharp mode should always reindent the current line,
regardless of where in the line point is when the TAB command is used."
  :type 'boolean
  :group 'fsharp)

(defcustom fsharp-indent-offset 4
  "*Amount of offset per level of indentation.
`\\[fsharp-guess-indent-offset]' can usually guess a good value when
you're editing someone else's Fsharp code."
  :type 'integer
  :group 'fsharp)

(defcustom fsharp-continuation-offset 2
  "*Additional amount of offset to give for some continuation lines.
Continuation lines are those that immediately follow a backslash
terminated line.  Only those continuation lines for a block opening
statement are given this extra offset."
  :type 'integer
  :group 'fsharp)

(defcustom fsharp-smart-indentation t
  "*Should `fsharp-mode' try to automagically set some indentation variables?
When this variable is non-nil, two things happen when a buffer is set
to `fsharp-mode':

    1. `fsharp-indent-offset' is guessed from existing code in the buffer.
       Only guessed values between 2 and 8 are considered.  If a valid
       guess can't be made (perhaps because you are visiting a new
       file), then the value in `fsharp-indent-offset' is used.

    2. `indent-tabs-mode' is turned off if `fsharp-indent-offset' does not
       equal `tab-width' (`indent-tabs-mode' is never turned on by
       Fsharp mode).  This means that for newly written code, tabs are
       only inserted in indentation if one tab is one indentation
       level, otherwise only spaces are used.

Note that both these settings occur *after* `fsharp-mode-hook' is run,
so if you want to defeat the automagic configuration, you must also
set `fsharp-smart-indentation' to nil in your `fsharp-mode-hook'."
  :type 'boolean
  :group 'fsharp)

(defcustom fsharp-honor-comment-indentation t
  "*Controls how comment lines influence subsequent indentation.

When nil, all comment lines are skipped for indentation purposes, and
if possible, a faster algorithm is used (i.e. X/Emacs 19 and beyond).

When t, lines that begin with a single `//' are a hint to subsequent
line indentation.  If the previous line is such a comment line (as
opposed to one that starts with `fsharp-block-comment-prefix'), then its
indentation is used as a hint for this line's indentation.  Lines that
begin with `fsharp-block-comment-prefix' are ignored for indentation
purposes.

When not nil or t, comment lines that begin with a single `//' are used
as indentation hints, unless the comment character is in column zero."
  :type '(choice
	  (const :tag "Skip all comment lines (fast)" nil)
	  (const :tag "Single // `sets' indentation for next line" t)
	  (const :tag "Single // `sets' indentation except at column zero"
		 other)
	  )
  :group 'fsharp)

(defcustom fsharp-backspace-function 'backward-delete-char-untabify
  "*Function called by `fsharp-electric-backspace' when deleting backwards."
  :type 'function
  :group 'fsharp)

(defcustom fsharp-delete-function 'delete-char
  "*Function called by `fsharp-electric-delete' when deleting forwards."
  :type 'function
  :group 'fsharp)


;; Constants

(defconst fsharp-stringlit-re
  (concat
   ;; These fail if backslash-quote ends the string (not worth
   ;; fixing?).  They precede the short versions so that the first two
   ;; quotes don't look like an empty short string.
   ;;
   ;; (maybe raw), long single quoted triple quoted strings (SQTQ),
   ;; with potential embedded single quotes
   "[rR]?'''[^']*\\(\\('[^']\\|''[^']\\)[^']*\\)*'''"
   "\\|"
   ;; (maybe raw), long double quoted triple quoted strings (DQTQ),
   ;; with potential embedded double quotes
   "[rR]?\"\"\"[^\"]*\\(\\(\"[^\"]\\|\"\"[^\"]\\)[^\"]*\\)*\"\"\""
   "\\|"
   "[rR]?'\\([^'\n\\]\\|\\\\.\\)*'"	; single-quoted
   "\\|"				; or
   "[rR]?\"\\([^\"\n\\]\\|\\\\.\\)*\""	; double-quoted
   )
  "Regular expression matching a Fsharp string literal.")

(defconst fsharp-continued-re
  ;; This is tricky because a trailing backslash does not mean
  ;; continuation if it's in a comment
;;   (concat
;;    "\\(" "[^#'\"\n\\]" "\\|" fsharp-stringlit-re "\\)*"
;;    "\\\\$")
;;   "Regular expression matching Fsharp backslash continuation lines.")
  (concat ".*\\(" (mapconcat 'identity
			   '("+" "-" "*" "/")
			   "\\|")
	  "\\)$")
  "Regular expression matching unterminated expressions.")


;(defconst fsharp-blank-or-comment-re "[ \t]*\\($\\|#\\)"
(defconst fsharp-blank-or-comment-re "[ \t]*\\(//.*\\)?"
  "Regular expression matching a blank or comment line.")

(defconst fsharp-outdent-re
  (concat "\\(" (mapconcat 'identity
			   '("else"
			     "with"
                             "finally"
                             "end"
                             "done"
			     "elif")
			   "\\|")
	  "\\)")
  "Regular expression matching statements to be dedented one level.")

(defconst fsharp-block-closing-keywords-re
;  "\\(return\\|raise\\|break\\|continue\\|pass\\)"
  "\\(end\\|done\\|raise\\|failwith\\|failwithf\\|rethrow\\|exit\\)"
  "Regular expression matching keywords which typically close a block.")


(defconst fsharp-no-outdent-re
  (concat
   "\\("
   (mapconcat 'identity
	      (list "try"
		    "while\\s +.*"
		    "for\\s +.*"
		    "then"
		    (concat fsharp-block-closing-keywords-re "[ \t\n]")
		    )
	      "\\|")
	  "\\)")
  "Regular expression matching lines not to dedent after.")


(defconst fsharp-block-opening-re
  (concat "\\(" (mapconcat 'identity
			   '("then"
                          "else"
			     "with"
                          "finally"
                          "class"
                          "struct"
			     "="        ; for example: let f x =
                          "->"
                          "do"
                          "try"
                          "function")
			   "\\|")
	  "\\)")
  "Regular expression matching expressions which begin a block")


;; Major mode boilerplate

;; define a mode-specific abbrev table for those who use such things
(defvar fsharp-mode-abbrev-table nil
  "Abbrev table in use in `fsharp-mode' buffers.")
(define-abbrev-table 'fsharp-mode-abbrev-table nil)



;; Utilities
(defmacro fsharp-safe (&rest body)
  "Safely execute BODY, return nil if an error occurred."
  `(condition-case nil
       (progn ,@ body)
     (error nil)))


(defsubst fsharp-keep-region-active ()
  "Keep the region active in XEmacs."
  ;; Ignore byte-compiler warnings you might see.  Also note that
  ;; FSF's Emacs 19 does it differently; its policy doesn't require us
  ;; to take explicit action.
  (and (boundp 'zmacs-region-stays)
       (setq zmacs-region-stays t)))

(defsubst fsharp-point (position)
  "Returns the value of point at certain commonly referenced POSITIONs.
POSITION can be one of the following symbols:

  bol  -- beginning of line
  eol  -- end of line
  bod  -- beginning of def or class
  eod  -- end of def or class
  bob  -- beginning of buffer
  eob  -- end of buffer
  boi  -- back to indentation
  bos  -- beginning of statement

This function does not modify point or mark."
  (let ((here (point)))
    (cond
     ((eq position 'bol) (beginning-of-line))
     ((eq position 'eol) (end-of-line))
     ((eq position 'bod) (fsharp-beginning-of-def-or-class 'either))
     ((eq position 'eod) (fsharp-end-of-def-or-class 'either))
     ;; Kind of funny, I know, but useful for fsharp-up-exception.
     ((eq position 'bob) (beginning-of-buffer))
     ((eq position 'eob) (end-of-buffer))
     ((eq position 'boi) (back-to-indentation))
     ((eq position 'bos) (fsharp-goto-initial-line))
     (t (error "Unknown buffer position requested: %s" position))
     )
    (prog1
	(point)
      (goto-char here))))

(defun fsharp-in-literal (&optional lim)
  "Return non-nil if point is in a Fsharp literal (a comment or string).
Optional argument LIM indicates the beginning of the containing form,
i.e. the limit on how far back to scan."
  ;; This is the version used for non-XEmacs, which has a nicer
  ;; interface.
  ;;
  ;; WARNING: Watch out for infinite recursion.
  (let* ((lim (or lim (fsharp-point 'bod)))
	 (state (parse-partial-sexp lim (point))))
    (cond
     ((nth 3 state) 'string)
     ((nth 4 state) 'comment)
     (t nil))))

;; XEmacs has a built-in function that should make this much quicker.
;; In this case, lim is ignored
(defun fsharp-fast-in-literal (&optional lim)
  "Fast version of `fsharp-in-literal', used only by XEmacs.
Optional LIM is ignored."
  ;; don't have to worry about context == 'block-comment
  (buffer-syntactic-context))

(if (fboundp 'buffer-syntactic-context)
    (defalias 'fsharp-in-literal 'fsharp-fast-in-literal))




;; electric characters
(defun fsharp-outdent-p ()
  "Returns non-nil if the current line should dedent one level."
  (save-excursion
    (progn (back-to-indentation)
           (looking-at fsharp-outdent-re))
))

;;     (and (progn (back-to-indentation)
;; 		(looking-at fsharp-outdent-re))
;; 	 ;; short circuit infloop on illegal construct
;; 	 (not (bobp))
;; 	 (progn (forward-line -1)
;; 		(fsharp-goto-initial-line)
;; 		(back-to-indentation)
;; 		(while (or (looking-at fsharp-blank-or-comment-re)
;; 			   (bobp))
;; 		  (backward-to-indentation 1))
;; 		(not (looking-at fsharp-no-outdent-re)))
;; 	 )))

(defun fsharp-electric-colon (arg)
  "Insert a colon.
In certain cases the line is dedented appropriately.  If a numeric
argument ARG is provided, that many colons are inserted
non-electrically.  Electric behavior is inhibited inside a string or
comment."
  (interactive "*P")
  (self-insert-command (prefix-numeric-value arg))
  ;; are we in a string or comment?
  (if (save-excursion
	(let ((pps (parse-partial-sexp (save-excursion
					 (fsharp-beginning-of-def-or-class)
					 (point))
				       (point))))
	  (not (or (nth 3 pps) (nth 4 pps)))))
      (save-excursion
	(let ((here (point))
	      (outdent 0)
	      (indent (fsharp-compute-indentation t)))
	  (if (and (not arg)
		   (fsharp-outdent-p)
		   (= indent (save-excursion
			       (fsharp-next-statement -1)
			       (fsharp-compute-indentation t)))
		   )
	      (setq outdent fsharp-indent-offset))
	  ;; Don't indent, only dedent.  This assumes that any lines
	  ;; that are already dedented relative to
	  ;; fsharp-compute-indentation were put there on purpose.  It's
	  ;; highly annoying to have `:' indent for you.  Use TAB, C-c
	  ;; C-l or C-c C-r to adjust.  TBD: Is there a better way to
	  ;; determine this???
	  (if (< (current-indentation) indent) nil
	    (goto-char here)
	    (beginning-of-line)
	    (delete-horizontal-space)
	    (indent-to (- indent outdent))
	    )))))



;; Electric deletion
(defun fsharp-electric-backspace (arg)
  "Delete preceding character or levels of indentation.
Deletion is performed by calling the function in `fsharp-backspace-function'
with a single argument (the number of characters to delete).

If point is at the leftmost column, delete the preceding newline.

Otherwise, if point is at the leftmost non-whitespace character of a
line that is neither a continuation line nor a non-indenting comment
line, or if point is at the end of a blank line, this command reduces
the indentation to match that of the line that opened the current
block of code.  The line that opened the block is displayed in the
echo area to help you keep track of where you are.  With
\\[universal-argument] dedents that many blocks (but not past column
zero).

Otherwise the preceding character is deleted, converting a tab to
spaces if needed so that only a single column position is deleted.
\\[universal-argument] specifies how many characters to delete;
default is 1.

When used programmatically, argument ARG specifies the number of
blocks to dedent, or the number of characters to delete, as indicated
above."
  (interactive "*p")
  (if (or (/= (current-indentation) (current-column))
	  (bolp)
	  (fsharp-continuation-line-p)
;	  (not fsharp-honor-comment-indentation)
;	  (looking-at "#[^ \t\n]")	; non-indenting #
	  )
      (funcall fsharp-backspace-function arg)
    ;; else indent the same as the colon line that opened the block
    ;; force non-blank so fsharp-goto-block-up doesn't ignore it
    (insert-char ?* 1)
    (backward-char)
    (let ((base-indent 0)		; indentation of base line
	  (base-text "")		; and text of base line
	  (base-found-p nil))
      (save-excursion
	(while (< 0 arg)
	  (condition-case nil		; in case no enclosing block
	      (progn
		(fsharp-goto-block-up 'no-mark)
		(setq base-indent (current-indentation)
		      base-text   (fsharp-suck-up-leading-text)
		      base-found-p t))
	    (error nil))
	  (setq arg (1- arg))))
      (delete-char 1)			; toss the dummy character
      (delete-horizontal-space)
      (indent-to base-indent)
      (if base-found-p
	  (message "Closes block: %s" base-text)))))


(defun fsharp-electric-delete (arg)
  "Delete preceding or following character or levels of whitespace.

The behavior of this function depends on the variable
`delete-key-deletes-forward'.  If this variable is nil (or does not
exist, as in older Emacsen and non-XEmacs versions), then this
function behaves identically to \\[c-electric-backspace].

If `delete-key-deletes-forward' is non-nil and is supported in your
Emacs, then deletion occurs in the forward direction, by calling the
function in `fsharp-delete-function'.

\\[universal-argument] (programmatically, argument ARG) specifies the
number of characters to delete (default is 1)."
  (interactive "*p")
 (funcall fsharp-delete-function arg))
;;   (if (or (and (fboundp 'delete-forward-p) ;XEmacs 21
;; 	       (delete-forward-p))
;; 	  (and (boundp 'delete-key-deletes-forward) ;XEmacs 20
;; 	       delete-key-deletes-forward))
;;       (funcall fsharp-delete-function arg)
;;     (fsharp-electric-backspace arg)))

;; required for pending-del and delsel modes
(put 'fsharp-electric-colon 'delete-selection t) ;delsel
(put 'fsharp-electric-colon 'pending-delete   t) ;pending-del
(put 'fsharp-electric-backspace 'delete-selection 'supersede) ;delsel
(put 'fsharp-electric-backspace 'pending-delete   'supersede) ;pending-del
(put 'fsharp-electric-delete    'delete-selection 'supersede) ;delsel
(put 'fsharp-electric-delete    'pending-delete   'supersede) ;pending-del



(defun fsharp-indent-line (&optional arg)
  "Fix the indentation of the current line according to Fsharp rules.
With \\[universal-argument] (programmatically, the optional argument
ARG non-nil), ignore dedenting rules for block closing statements
(e.g. return, raise, break, continue, pass)

This function is normally bound to `indent-line-function' so
\\[indent-for-tab-command] will call it."
  (interactive "P")
  (let* ((ci (current-indentation))
	 (move-to-indentation-p (<= (current-column) ci))
	 (need (fsharp-compute-indentation (not arg)))
         (cc (current-column)))
    ;; dedent out a level if previous command was the same unless we're in
    ;; column 1
    (if (and (equal last-command this-command)
             (/= cc 0))
        (progn
          (beginning-of-line)
          (delete-horizontal-space)
          (indent-to (* (/ (- cc 1) fsharp-indent-offset) fsharp-indent-offset)))
      (progn
	;; see if we need to dedent
	(if (fsharp-outdent-p)
	    (setq need (- need fsharp-indent-offset)))
	(if (or fsharp-tab-always-indent
		move-to-indentation-p)
	    (progn (if (/= ci need)
		       (save-excursion
		       (beginning-of-line)
		       (delete-horizontal-space)
		       (indent-to need)))
		   (if move-to-indentation-p (back-to-indentation)))
	    (insert-tab))))))

(defun fsharp-newline-and-indent ()
  "Strives to act like the Emacs `newline-and-indent'.
This is just `strives to' because correct indentation can't be computed
from scratch for Fsharp code.  In general, deletes the whitespace before
point, inserts a newline, and takes an educated guess as to how you want
the new line indented."
  (interactive)
  (let ((ci (current-indentation)))
    (if (< ci (current-column))		; if point beyond indentation
	(newline-and-indent)
      ;; else try to act like newline-and-indent "normally" acts
      (beginning-of-line)
      (insert-char ?\n 1)
      (move-to-column ci))))

(defun fsharp-compute-indentation (honor-block-close-p)
  "Compute Fsharp indentation.
When HONOR-BLOCK-CLOSE-P is non-nil, statements such as `return',
`raise', `break', `continue', and `pass' force one level of
dedenting."
  (save-excursion
    (beginning-of-line)
    (let* ((bod (fsharp-point 'bod))
	   (pps (parse-partial-sexp bod (point)))
	   (boipps (parse-partial-sexp bod (fsharp-point 'boi)))
	   placeholder)
      (cond
       ;; are we on a continuation line?
       ((fsharp-continuation-line-p)
	(let ((startpos (point))
	      (open-bracket-pos (fsharp-nesting-level))
	      endpos searching found state)
	  (if open-bracket-pos
	      (progn
		;; align with first item in list; else a normal
		;; indent beyond the line with the open bracket
		(goto-char (1+ open-bracket-pos)) ; just beyond bracket
		;; is the first list item on the same line?
		(skip-chars-forward " \t")
		(if (null (memq (following-char) '(?\n ?# ?\\)))
					; yes, so line up with it
		    (current-column)
		  ;; first list item on another line, or doesn't exist yet
		  (forward-line 1)
		  (while (and (< (point) startpos)
			      (looking-at "[ \t]*\\(//\\|[\n\\\\]\\)")) ; skip noise
		    (forward-line 1))
		  (if (and (< (point) startpos)
			   (/= startpos
			       (save-excursion
				 (goto-char (1+ open-bracket-pos))
				 (forward-comment (point-max))
				 (point))))
		      ;; again mimic the first list item
		      (current-indentation)
		    ;; else they're about to enter the first item
		    (goto-char open-bracket-pos)
		    (setq placeholder (point))
		    (fsharp-goto-initial-line)
		    (fsharp-goto-beginning-of-tqs
		     (save-excursion (nth 3 (parse-partial-sexp
					     placeholder (point)))))
		    (+ (current-indentation) fsharp-indent-offset))))

	    ;; else on backslash continuation line
	    (forward-line -1)
	    (if (fsharp-continuation-line-p) ; on at least 3rd line in block
		(current-indentation)	; so just continue the pattern
	      ;; else started on 2nd line in block, so indent more.
	      ;; if base line is an assignment with a start on a RHS,
	      ;; indent to 2 beyond the leftmost "="; else skip first
	      ;; chunk of non-whitespace characters on base line, + 1 more
	      ;; column
	      (end-of-line)
	      (setq endpos (point)
		    searching t)
	      (back-to-indentation)
	      (setq startpos (point))
	      ;; look at all "=" from left to right, stopping at first
	      ;; one not nested in a list or string
	      (while searching
		(skip-chars-forward "^=" endpos)
		(if (= (point) endpos)
		    (setq searching nil)
		  (forward-char 1)
		  (setq state (parse-partial-sexp startpos (point)))
		  (if (and (zerop (car state)) ; not in a bracket
			   (null (nth 3 state))) ; & not in a string
		      (progn
			(setq searching nil) ; done searching in any case
			(setq found
			      (not (or
				    (eq (following-char) ?=)
				    (memq (char-after (- (point) 2))
					  '(?< ?> ?!)))))))))
	      (if (or (not found)	; not an assignment
		      (looking-at "[ \t]*\\\\")) ; <=><spaces><backslash>
		  (progn
		    (goto-char startpos)
		    (skip-chars-forward "^ \t\n")))
	      ;; if this is a continuation for a block opening
	      ;; statement, add some extra offset.
	      (+ (current-column) (if (fsharp-statement-opens-block-p)
				      fsharp-continuation-offset 0)
		 1)
	      ))))

       ;; not on a continuation line
       ((bobp) (current-indentation))

       ;; Dfn: "Indenting comment line".  A line containing only a
       ;; comment, but which is treated like a statement for
       ;; indentation calculation purposes.  Such lines are only
       ;; treated specially by the mode; they are not treated
       ;; specially by the Fsharp interpreter.

       ;; The first non-blank line following an indenting comment
       ;; line is given the same amount of indentation as the
       ;; indenting comment line.

       ;; All other comment-only lines are ignored for indentation
       ;; purposes.

       ;; Are we looking at a comment-only line which is *not* an
       ;; indenting comment line?  If so, we assume that it's been
       ;; placed at the desired indentation, so leave it alone.
       ;; Indenting comment lines are aligned as statements down
       ;; below.
       ((and (looking-at "[ \t]*//[^ \t\n]")
	     ;; NOTE: this test will not be performed in older Emacsen
	     (fboundp 'forward-comment)
	     (<= (current-indentation)
		 (save-excursion
		   (forward-comment (- (point-max)))
		   (current-indentation))))
	(current-indentation))

       ;; else indentation based on that of the statement that
       ;; precedes us; use the first line of that statement to
       ;; establish the base, in case the user forced a non-std
       ;; indentation for the continuation lines (if any)
       (t
	;; skip back over blank & non-indenting comment lines note:
	;; will skip a blank or non-indenting comment line that
	;; happens to be a continuation line too.  use fast Emacs 19
	;; function if it's there.
	(if (and (eq fsharp-honor-comment-indentation nil)
		 (fboundp 'forward-comment))
	    (forward-comment (- (point-max)))
	  (let ((prefix-re "//[ \t]*")
		done)
	    (while (not done)
	      (re-search-backward "^[ \t]*\\([^ \t\n]\\|//\\)" nil 'move)
	      (setq done (or (bobp)
			     (and (eq fsharp-honor-comment-indentation t)
				  (save-excursion
				    (back-to-indentation)
				    (not (looking-at prefix-re))
				    ))
			     (and (not (eq fsharp-honor-comment-indentation t))
				  (save-excursion
				    (back-to-indentation)
				    (and (not (looking-at prefix-re))
					 (or (looking-at "[^/]")
					     (not (zerop (current-column)))
					     ))
				    ))
			     ))
	      )))
	;; if we landed inside a string, go to the beginning of that
	;; string. this handles triple quoted, multi-line spanning
	;; strings.
	(fsharp-goto-beginning-of-tqs (nth 3 (parse-partial-sexp bod (point))))
	;; now skip backward over continued lines
	(setq placeholder (point))
	(fsharp-goto-initial-line)
	;; we may *now* have landed in a TQS, so find the beginning of
	;; this string.
	(fsharp-goto-beginning-of-tqs
	 (save-excursion (nth 3 (parse-partial-sexp
				 placeholder (point)))))
	(+ (current-indentation)
	   (if (fsharp-statement-opens-block-p)
	       fsharp-indent-offset
	     (if (and honor-block-close-p (fsharp-statement-closes-block-p))
		 (- fsharp-indent-offset)
	       0)))
	)))))

(defun fsharp-guess-indent-offset (&optional global)
  "Guess a good value for, and change, `fsharp-indent-offset'.

By default, make a buffer-local copy of `fsharp-indent-offset' with the
new value, so that other Fsharp buffers are not affected.  With
\\[universal-argument] (programmatically, optional argument GLOBAL),
change the global value of `fsharp-indent-offset'.  This affects all
Fsharp buffers (that don't have their own buffer-local copy), both
those currently existing and those created later in the Emacs session.

Some people use a different value for `fsharp-indent-offset' than you use.
There's no excuse for such foolishness, but sometimes you have to deal
with their ugly code anyway.  This function examines the file and sets
`fsharp-indent-offset' to what it thinks it was when they created the
mess.

Specifically, it searches forward from the statement containing point,
looking for a line that opens a block of code.  `fsharp-indent-offset' is
set to the difference in indentation between that line and the Fsharp
statement following it.  If the search doesn't succeed going forward,
it's tried again going backward."
  (interactive "P")			; raw prefix arg
  (let (new-value
	(start (point))
	(restart (point))
	(found nil)
	colon-indent)
    (fsharp-goto-initial-line)
    (while (not (or found (eobp)))
      (when (and (re-search-forward fsharp-block-opening-re nil 'move)
		 (not (fsharp-in-literal restart)))
	(setq restart (point))
	(fsharp-goto-initial-line)
	(if (fsharp-statement-opens-block-p)
	    (setq found t)
	  (goto-char restart))))
    (unless found
      (goto-char start)
      (fsharp-goto-initial-line)
      (while (not (or found (bobp)))
	(setq found (and
		     (re-search-backward fsharp-block-opening-re nil 'move)
		     (or (fsharp-goto-initial-line) t) ; always true -- side effect
		     (fsharp-statement-opens-block-p)))))
    (setq colon-indent (current-indentation)
	  found (and found (zerop (fsharp-next-statement 1)))
	  new-value (- (current-indentation) colon-indent))
    (goto-char start)
    (if (not found)
	(error "Sorry, couldn't guess a value for fsharp-indent-offset")
      (funcall (if global 'kill-local-variable 'make-local-variable)
	       'fsharp-indent-offset)
      (setq fsharp-indent-offset new-value)
      (or noninteractive
	  (message "%s value of fsharp-indent-offset set to %d"
		   (if global "Global" "Local")
		   fsharp-indent-offset)))
    ))

(defun fsharp-comment-indent-function ()
  "Fsharp version of `comment-indent-function'."
  ;; This is required when filladapt is turned off.  Without it, when
  ;; filladapt is not used, comments which start in column zero
  ;; cascade one character to the right
  (save-excursion
    (beginning-of-line)
    (let ((eol (fsharp-point 'eol)))
      (and comment-start-skip
	   (re-search-forward comment-start-skip eol t)
	   (setq eol (match-beginning 0)))
      (goto-char eol)
      (skip-chars-backward " \t")
      (max comment-column (+ (current-column) (if (bolp) 0 1)))
      )))

(defun fsharp-narrow-to-defun (&optional class)
  "Make text outside current defun invisible.
The defun visible is the one that contains point or follows point.
Optional CLASS is passed directly to `fsharp-beginning-of-def-or-class'."
  (interactive "P")
  (save-excursion
    (widen)
    (fsharp-end-of-def-or-class class)
    (let ((end (point)))
      (fsharp-beginning-of-def-or-class class)
      (narrow-to-region (point) end))))


(defun fsharp-shift-region (start end count)
  "Indent lines from START to END by COUNT spaces."
  (save-excursion
    (goto-char end)
    (beginning-of-line)
    (setq end (point))
    (goto-char start)
    (beginning-of-line)
    (setq start (point))
    (indent-rigidly start end count)))

(defun fsharp-shift-region-left (start end &optional count)
  "Shift region of Fsharp code to the left.
The lines from the line containing the start of the current region up
to (but not including) the line containing the end of the region are
shifted to the left, by `fsharp-indent-offset' columns.

If a prefix argument is given, the region is instead shifted by that
many columns.  With no active region, dedent only the current line.
You cannot dedent the region if any line is already at column zero."
  (interactive
   (let ((p (point))
	 (m (mark))
	 (arg current-prefix-arg))
     (if m
	 (list (min p m) (max p m) arg)
       (list p (save-excursion (forward-line 1) (point)) arg))))
  ;; if any line is at column zero, don't shift the region
  (save-excursion
    (goto-char start)
    (while (< (point) end)
      (back-to-indentation)
      (if (and (zerop (current-column))
	       (not (looking-at "\\s *$")))
	  (error "Region is at left edge"))
      (forward-line 1)))
  (fsharp-shift-region start end (- (prefix-numeric-value
				 (or count fsharp-indent-offset))))
  (fsharp-keep-region-active))

(defun fsharp-shift-region-right (start end &optional count)
  "Shift region of Fsharp code to the right.
The lines from the line containing the start of the current region up
to (but not including) the line containing the end of the region are
shifted to the right, by `fsharp-indent-offset' columns.

If a prefix argument is given, the region is instead shifted by that
many columns.  With no active region, indent only the current line."
  (interactive
   (let ((p (point))
	 (m (mark))
	 (arg current-prefix-arg))
     (if m
	 (list (min p m) (max p m) arg)
       (list p (save-excursion (forward-line 1) (point)) arg))))
  (fsharp-shift-region start end (prefix-numeric-value
			      (or count fsharp-indent-offset)))
  (fsharp-keep-region-active))

(defun fsharp-indent-region (start end &optional indent-offset)
  "Reindent a region of Fsharp code.

The lines from the line containing the start of the current region up
to (but not including) the line containing the end of the region are
reindented.  If the first line of the region has a non-whitespace
character in the first column, the first line is left alone and the
rest of the region is reindented with respect to it.  Else the entire
region is reindented with respect to the (closest code or indenting
comment) statement immediately preceding the region.

This is useful when code blocks are moved or yanked, when enclosing
control structures are introduced or removed, or to reformat code
using a new value for the indentation offset.

If a numeric prefix argument is given, it will be used as the value of
the indentation offset.  Else the value of `fsharp-indent-offset' will be
used.

Warning: The region must be consistently indented before this function
is called!  This function does not compute proper indentation from
scratch (that's impossible in Fsharp), it merely adjusts the existing
indentation to be correct in context.

Warning: This function really has no idea what to do with
non-indenting comment lines, and shifts them as if they were indenting
comment lines.  Fixing this appears to require telepathy.

Special cases: whitespace is deleted from blank lines; continuation
lines are shifted by the same amount their initial line was shifted,
in order to preserve their relative indentation with respect to their
initial line; and comment lines beginning in column 1 are ignored."
  (interactive "*r\nP")			; region; raw prefix arg
  (save-excursion
    (goto-char end)   (beginning-of-line) (setq end (point-marker))
    (goto-char start) (beginning-of-line)
    (let ((fsharp-indent-offset (prefix-numeric-value
			     (or indent-offset fsharp-indent-offset)))
	  (indents '(-1))		; stack of active indent levels
	  (target-column 0)		; column to which to indent
	  (base-shifted-by 0)		; amount last base line was shifted
	  (indent-base (if (looking-at "[ \t\n]")
			   (fsharp-compute-indentation t)
			 0))
	  ci)
      (while (< (point) end)
	(setq ci (current-indentation))
	;; figure out appropriate target column
	(cond
	 ((or (looking-at "//")	; comment in column 1
	      (looking-at "[ \t]*$"))	; entirely blank
	  (setq target-column 0))
	 ((fsharp-continuation-line-p)	; shift relative to base line
	  (setq target-column (+ ci base-shifted-by)))
	 (t				; new base line
	  (if (> ci (car indents))	; going deeper; push it
	      (setq indents (cons ci indents))
	    ;; else we should have seen this indent before
	    (setq indents (memq ci indents)) ; pop deeper indents
	    (if (null indents)
		(error "Bad indentation in region, at line %d"
		       (save-restriction
			 (widen)
			 (1+ (count-lines 1 (point)))))))
	  (setq target-column (+ indent-base
				 (* fsharp-indent-offset
				    (- (length indents) 2))))
	  (setq base-shifted-by (- target-column ci))))
	;; shift as needed
	(if (/= ci target-column)
	    (progn
	      (delete-horizontal-space)
	      (indent-to target-column)))
	(forward-line 1))))
  (set-marker end nil))


;; Functions for moving point
(defun fsharp-previous-statement (count)
  "Go to the start of the COUNTth preceding Fsharp statement.
By default, goes to the previous statement.  If there is no such
statement, goes to the first statement.  Return count of statements
left to move.  `Statements' do not include blank, comment, or
continuation lines."
  (interactive "p")			; numeric prefix arg
  (if (< count 0) (fsharp-next-statement (- count))
    (fsharp-goto-initial-line)
    (let (start)
      (while (and
	      (setq start (point))	; always true -- side effect
	      (> count 0)
	      (zerop (forward-line -1))
	      (fsharp-goto-statement-at-or-above))
	(setq count (1- count)))
      (if (> count 0) (goto-char start)))
    count))

(defun fsharp-next-statement (count)
  "Go to the start of next Fsharp statement.
If the statement at point is the i'th Fsharp statement, goes to the
start of statement i+COUNT.  If there is no such statement, goes to the
last statement.  Returns count of statements left to move.  `Statements'
do not include blank, comment, or continuation lines."
  (interactive "p")			; numeric prefix arg
  (if (< count 0) (fsharp-previous-statement (- count))
    (beginning-of-line)
    (let (start)
      (while (and
	      (setq start (point))	; always true -- side effect
	      (> count 0)
	      (fsharp-goto-statement-below))
	(setq count (1- count)))
      (if (> count 0) (goto-char start)))
    count))

(defun fsharp-goto-block-up (&optional nomark)
  "Move up to start of current block.
Go to the statement that starts the smallest enclosing block; roughly
speaking, this will be the closest preceding statement that ends with a
colon and is indented less than the statement you started on.  If
successful, also sets the mark to the starting point.

`\\[fsharp-mark-block]' can be used afterward to mark the whole code
block, if desired.

If called from a program, the mark will not be set if optional argument
NOMARK is not nil."
  (interactive)
  (let ((start (point))
	(found nil)
	initial-indent)
    (fsharp-goto-initial-line)
    ;; if on blank or non-indenting comment line, use the preceding stmt
    (if (looking-at "[ \t]*\\($\\|//[^ \t\n]\\)")
	(progn
	  (fsharp-goto-statement-at-or-above)
	  (setq found (fsharp-statement-opens-block-p))))
    ;; search back for colon line indented less
    (setq initial-indent (current-indentation))
    (if (zerop initial-indent)
	;; force fast exit
	(goto-char (point-min)))
    (while (not (or found (bobp)))
      (setq found
	    (and
	     (re-search-backward fsharp-block-opening-re nil 'move)
	     (or (fsharp-goto-initial-line) t) ; always true -- side effect
	     (< (current-indentation) initial-indent)
	     (fsharp-statement-opens-block-p))))
    (if found
	(progn
	  (or nomark (push-mark start))
	  (back-to-indentation))
      (goto-char start)
      (error "Enclosing block not found"))))

;;FIXME
(defun fsharp-beginning-of-def-or-class (&optional class count)
  "Move point to start of `def' or `class'.

Searches back for the closest preceding `def'.  If you supply a prefix
arg, looks for a `class' instead.  The docs below assume the `def'
case; just substitute `class' for `def' for the other case.
Programmatically, if CLASS is `either', then moves to either `class'
or `def'.

When second optional argument is given programmatically, move to the
COUNTth start of `def'.

If point is in a `def' statement already, and after the `d', simply
moves point to the start of the statement.

Otherwise (i.e. when point is not in a `def' statement, or at or
before the `d' of a `def' statement), searches for the closest
preceding `def' statement, and leaves point at its start.  If no such
statement can be found, leaves point at the start of the buffer.

Returns t iff a `def' statement is found by these rules.

Note that doing this command repeatedly will take you closer to the
start of the buffer each time.

To mark the current `def', see `\\[fsharp-mark-def-or-class]'."
  (interactive "P")			; raw prefix arg
  (setq count (or count 1))
  (let ((at-or-before-p (<= (current-column) (current-indentation)))
	(start-of-line (goto-char (fsharp-point 'bol)))
	(start-of-stmt (goto-char (fsharp-point 'bos)))
	(start-re (cond ((eq class 'either) "^[ \t]*\\(type\\|let\\)\\>")
			(class "^[ \t]*type\\>")
			(t "^[ \t]*let\\>")))
	)
    ;; searching backward
    (if (and (< 0 count)
	     (or (/= start-of-stmt start-of-line)
		 (not at-or-before-p)))
	(end-of-line))
    ;; search forward
    (if (and (> 0 count)
	     (zerop (current-column))
	     (looking-at start-re))
	(end-of-line))
    (if (re-search-backward start-re nil 'move count)
	(goto-char (match-beginning 0)))))

;; Backwards compatibility
(defalias 'beginning-of-fsharp-def-or-class 'fsharp-beginning-of-def-or-class)

(defun fsharp-end-of-def-or-class (&optional class count)
  "Move point beyond end of `def' or `class' body.

By default, looks for an appropriate `def'.  If you supply a prefix
arg, looks for a `class' instead.  The docs below assume the `def'
case; just substitute `class' for `def' for the other case.
Programmatically, if CLASS is `either', then moves to either `class'
or `def'.

When second optional argument is given programmatically, move to the
COUNTth end of `def'.

If point is in a `def' statement already, this is the `def' we use.

Else, if the `def' found by `\\[fsharp-beginning-of-def-or-class]'
contains the statement you started on, that's the `def' we use.

Otherwise, we search forward for the closest following `def', and use that.

If a `def' can be found by these rules, point is moved to the start of
the line immediately following the `def' block, and the position of the
start of the `def' is returned.

Else point is moved to the end of the buffer, and nil is returned.

Note that doing this command repeatedly will take you closer to the
end of the buffer each time.

To mark the current `def', see `\\[fsharp-mark-def-or-class]'."
  (interactive "P")			; raw prefix arg
  (if (and count (/= count 1))
      (fsharp-beginning-of-def-or-class (- 1 count)))
  (let ((start (progn (fsharp-goto-initial-line) (point)))
	(which (cond ((eq class 'either) "\\(type\\|let\\)")
		     (class "type")
		     (t "let")))
	(state 'not-found))
    ;; move point to start of appropriate def/class
    (if (looking-at (concat "[ \t]*" which "\\>")) ; already on one
	(setq state 'at-beginning)
      ;; else see if fsharp-beginning-of-def-or-class hits container
      (if (and (fsharp-beginning-of-def-or-class class)
	       (progn (fsharp-goto-beyond-block)
		      (> (point) start)))
	  (setq state 'at-end)
	;; else search forward
	(goto-char start)
	(if (re-search-forward (concat "^[ \t]*" which "\\>") nil 'move)
	    (progn (setq state 'at-beginning)
		   (beginning-of-line)))))
    (cond
     ((eq state 'at-beginning) (fsharp-goto-beyond-block) t)
     ((eq state 'at-end) t)
     ((eq state 'not-found) nil)
     (t (error "Internal error in `fsharp-end-of-def-or-class'")))))


;; Functions for marking regions
(defun fsharp-mark-block (&optional extend just-move)
  "Mark following block of lines.  With prefix arg, mark structure.
Easier to use than explain.  It sets the region to an `interesting'
block of succeeding lines.  If point is on a blank line, it goes down to
the next non-blank line.  That will be the start of the region.  The end
of the region depends on the kind of line at the start:

 - If a comment, the region will include all succeeding comment lines up
   to (but not including) the next non-comment line (if any).

 - Else if a prefix arg is given, and the line begins one of these
   structures:

     if elif else try except finally for while def class

   the region will be set to the body of the structure, including
   following blocks that `belong' to it, but excluding trailing blank
   and comment lines.  E.g., if on a `try' statement, the `try' block
   and all (if any) of the following `except' and `finally' blocks
   that belong to the `try' structure will be in the region.  Ditto
   for if/elif/else, for/else and while/else structures, and (a bit
   degenerate, since they're always one-block structures) def and
   class blocks.

 - Else if no prefix argument is given, and the line begins a Fsharp
   block (see list above), and the block is not a `one-liner' (i.e.,
   the statement ends with a colon, not with code), the region will
   include all succeeding lines up to (but not including) the next
   code statement (if any) that's indented no more than the starting
   line, except that trailing blank and comment lines are excluded.
   E.g., if the starting line begins a multi-statement `def'
   structure, the region will be set to the full function definition,
   but without any trailing `noise' lines.

 - Else the region will include all succeeding lines up to (but not
   including) the next blank line, or code or indenting-comment line
   indented strictly less than the starting line.  Trailing indenting
   comment lines are included in this case, but not trailing blank
   lines.

A msg identifying the location of the mark is displayed in the echo
area; or do `\\[exchange-point-and-mark]' to flip down to the end.

If called from a program, optional argument EXTEND plays the role of
the prefix arg, and if optional argument JUST-MOVE is not nil, just
moves to the end of the block (& does not set mark or display a msg)."
  (interactive "P")			; raw prefix arg
  (fsharp-goto-initial-line)
  ;; skip over blank lines
  (while (and
	  (looking-at "[ \t]*$")	; while blank line
	  (not (eobp)))			; & somewhere to go
    (forward-line 1))
  (if (eobp)
      (error "Hit end of buffer without finding a non-blank stmt"))
  (let ((initial-pos (point))
	(initial-indent (current-indentation))
	last-pos			; position of last stmt in region
	(followers
	 '((if elif else) (elif elif else) (else)
	   (try except finally) (except except) (finally)
	   (for else) (while else)
	   (def) (class) ) )
	first-symbol next-symbol)

    (cond
     ;; if comment line, suck up the following comment lines
     ((looking-at "[ \t]*//")
      (re-search-forward "^[ \t]*\\([^ \t]\\|//\\)" nil 'move) ; look for non-comment
      (re-search-backward "^[ \t]*//")	; and back to last comment in block
      (setq last-pos (point)))

     ;; else if line is a block line and EXTEND given, suck up
     ;; the whole structure
     ((and extend
	   (setq first-symbol (fsharp-suck-up-first-keyword) )
	   (assq first-symbol followers))
      (while (and
	      (or (fsharp-goto-beyond-block) t) ; side effect
	      (forward-line -1)		; side effect
	      (setq last-pos (point))	; side effect
	      (fsharp-goto-statement-below)
	      (= (current-indentation) initial-indent)
	      (setq next-symbol (fsharp-suck-up-first-keyword))
	      (memq next-symbol (cdr (assq first-symbol followers))))
	(setq first-symbol next-symbol)))

     ;; else if line *opens* a block, search for next stmt indented <=
     ((fsharp-statement-opens-block-p)
      (while (and
	      (setq last-pos (point))	; always true -- side effect
	      (fsharp-goto-statement-below)
	      (> (current-indentation) initial-indent)
	      )))

     ;; else plain code line; stop at next blank line, or stmt or
     ;; indenting comment line indented <
     (t
      (while (and
	      (setq last-pos (point))	; always true -- side effect
	      (or (fsharp-goto-beyond-final-line) t)
	      (not (looking-at "[ \t]*$")) ; stop at blank line
	      (or
	       (>= (current-indentation) initial-indent)
	       (looking-at "[ \t]*//[^ \t\n]"))) ; ignore non-indenting //
	nil)))

    ;; skip to end of last stmt
    (goto-char last-pos)
    (fsharp-goto-beyond-final-line)

    ;; set mark & display
    (if just-move
	()				; just return
      (push-mark (point) 'no-msg)
      (forward-line -1)
      (message "Mark set after: %s" (fsharp-suck-up-leading-text))
      (goto-char initial-pos))))

(defun fsharp-mark-def-or-class (&optional class)
  "Set region to body of def (or class, with prefix arg) enclosing point.
Pushes the current mark, then point, on the mark ring (all language
modes do this, but although it's handy it's never documented ...).

In most Emacs language modes, this function bears at least a
hallucinogenic resemblance to `\\[fsharp-end-of-def-or-class]' and
`\\[fsharp-beginning-of-def-or-class]'.

And in earlier versions of Fsharp mode, all 3 were tightly connected.
Turned out that was more confusing than useful: the `goto start' and
`goto end' commands are usually used to search through a file, and
people expect them to act a lot like `search backward' and `search
forward' string-search commands.  But because Fsharp `def' and `class'
can nest to arbitrary levels, finding the smallest def containing
point cannot be done via a simple backward search: the def containing
point may not be the closest preceding def, or even the closest
preceding def that's indented less.  The fancy algorithm required is
appropriate for the usual uses of this `mark' command, but not for the
`goto' variations.

So the def marked by this command may not be the one either of the
`goto' commands find: If point is on a blank or non-indenting comment
line, moves back to start of the closest preceding code statement or
indenting comment line.  If this is a `def' statement, that's the def
we use.  Else searches for the smallest enclosing `def' block and uses
that.  Else signals an error.

When an enclosing def is found: The mark is left immediately beyond
the last line of the def block.  Point is left at the start of the
def, except that: if the def is preceded by a number of comment lines
followed by (at most) one optional blank line, point is left at the
start of the comments; else if the def is preceded by a blank line,
point is left at its start.

The intent is to mark the containing def/class and its associated
documentation, to make moving and duplicating functions and classes
pleasant."
  (interactive "P")			; raw prefix arg
  (let ((start (point))
	(which (cond ((eq class 'either) "\\(type\\|let\\)")
		     (class "type")
		     (t "let"))))
    (push-mark start)
    (if (not (fsharp-go-up-tree-to-keyword which))
	(progn (goto-char start)
	       (error "Enclosing %s not found"
		      (if (eq class 'either)
			  "def or class"
			which)))
      ;; else enclosing def/class found
      (setq start (point))
      (fsharp-goto-beyond-block)
      (push-mark (point))
      (goto-char start)
      (if (zerop (forward-line -1))	; if there is a preceding line
	  (progn
	    (if (looking-at "[ \t]*$")	; it's blank
		(setq start (point))	; so reset start point
	      (goto-char start))	; else try again
	    (if (zerop (forward-line -1))
		(if (looking-at "[ \t]*//") ; a comment
		    ;; look back for non-comment line
		    ;; tricky: note that the regexp matches a blank
		    ;; line, cuz \n is in the 2nd character class
		    (and
		     (re-search-backward "^[ \t]*\\([^ \t]\\|//\\)" nil 'move)
		     (forward-line 1))
		  ;; no comment, so go back
		  (goto-char start)))))))
  (exchange-point-and-mark)
  (fsharp-keep-region-active))



(defun fsharp-describe-mode ()
  "Dump long form of Fsharp-mode docs."
  (interactive)
  (fsharp-dump-help-string "Major mode for editing Fsharp files.
Knows about Fsharp indentation, tokens, comments and continuation lines.
Paragraphs are separated by blank lines only.

FIXME"))

(require 'info-look)
;; The info-look package does not always provide this function (it
;; appears this is the case with XEmacs 21.1)
(when (fboundp 'info-lookup-maybe-add-help)
  (info-lookup-maybe-add-help
   :mode 'fsharp-mode
   :regexp "[a-zA-Z0-9_]+"
   :doc-spec '(("(fsharp-lib)Module Index")
	       ("(fsharp-lib)Class-Exception-Object Index")
	       ("(fsharp-lib)Function-Method-Variable Index")
	       ("(fsharp-lib)Miscellaneous Index")))
  )


;; Helper functions
(defvar fsharp-parse-state-re
  (concat
   "^[ \t]*\\(elif\\|else\\|while\\|def\\|class\\)\\>"
   "\\|"
   "^[^ /\t\n]"))

(defun fsharp-parse-state ()
  "Return the parse state at point (see `parse-partial-sexp' docs)."
  (save-excursion
    (let ((here (point))
	  pps done)
      (while (not done)
	;; back up to the first preceding line (if any; else start of
	;; buffer) that begins with a popular Fsharp keyword, or a
	;; non- whitespace and non-comment character.  These are good
	;; places to start parsing to see whether where we started is
	;; at a non-zero nesting level.  It may be slow for people who
	;; write huge code blocks or huge lists ... tough beans.
	(re-search-backward fsharp-parse-state-re nil 'move)
	(beginning-of-line)
	;; In XEmacs, we have a much better way to test for whether
	;; we're in a triple-quoted string or not.  Emacs does not
	;; have this built-in function, which is its loss because
	;; without scanning from the beginning of the buffer, there's
	;; no accurate way to determine this otherwise.
	(save-excursion (setq pps (parse-partial-sexp (point) here)))
	;; make sure we don't land inside a triple-quoted string
	(setq done (or (not (nth 3 pps))
		       (bobp)))
	;; Just go ahead and short circuit the test back to the
	;; beginning of the buffer.  This will be slow, but not
	;; nearly as slow as looping through many
	;; re-search-backwards.
	(if (not done)
	    (goto-char (point-min))))
      pps)))

(defun fsharp-nesting-level ()
  "Return the buffer position of the last unclosed enclosing list.
If nesting level is zero, return nil."
  (let ((status (fsharp-parse-state)))
    (if (zerop (car status))
	nil				; not in a nest
      (car (cdr status)))))		; char of open bracket

(defun fsharp-backslash-continuation-line-p ()
  "Return t iff preceding line ends with backslash that is not in a comment."
  (save-excursion
    (beginning-of-line)
    (and
     ;; use a cheap test first to avoid the regexp if possible
     ;; use 'eq' because char-after may return nil
     (not (eq (char-after (- (point) 2)) nil))

;     (eq (char-after (- (point) 2)) ?\\ )
     ;; make sure; since eq test passed, there is a preceding line
     (forward-line -1)			; always true -- side effect
     (looking-at fsharp-continued-re))))

(defun fsharp-continuation-line-p ()
  "Return t iff current line is a continuation line."
  (save-excursion
    (beginning-of-line)
    (or (fsharp-backslash-continuation-line-p)
	(fsharp-nesting-level))))

(defun fsharp-goto-beginning-of-tqs (delim)
  "Go to the beginning of the triple quoted string we find ourselves in.
DELIM is the TQS string delimiter character we're searching backwards
for."
  (let ((skip (and delim (make-string 1 delim)))
	(continue t))
    (when skip
      (save-excursion
	(while continue
	  (fsharp-safe (search-backward skip))
	  (setq continue (and (not (bobp))
			      (= (char-before) ?\\))))
	(if (and (= (char-before) delim)
		 (= (char-before (1- (point))) delim))
	    (setq skip (make-string 3 delim))))
      ;; we're looking at a triple-quoted string
      (fsharp-safe (search-backward skip)))))

(defun fsharp-goto-initial-line ()
  "Go to the initial line of the current statement.
Usually this is the line we're on, but if we're on the 2nd or
following lines of a continuation block, we need to go up to the first
line of the block."
  ;; Tricky: We want to avoid quadratic-time behavior for long
  ;; continued blocks, whether of the backslash or open-bracket
  ;; varieties, or a mix of the two.  The following manages to do that
  ;; in the usual cases.
  ;;
  ;; Also, if we're sitting inside a triple quoted string, this will
  ;; drop us at the line that begins the string.
  (let (open-bracket-pos)
    (while (fsharp-continuation-line-p)
      (beginning-of-line)
      (if (fsharp-backslash-continuation-line-p)
	  (while (fsharp-backslash-continuation-line-p)
	    (forward-line -1))
	;; else zip out of nested brackets/braces/parens
	(while (setq open-bracket-pos (fsharp-nesting-level))
	  (goto-char open-bracket-pos)))))
  (beginning-of-line))

(defun fsharp-goto-beyond-final-line ()
  "Go to the point just beyond the fine line of the current statement.
Usually this is the start of the next line, but if this is a
multi-line statement we need to skip over the continuation lines."
  ;; Tricky: Again we need to be clever to avoid quadratic time
  ;; behavior.
  ;;
  ;; XXX: Not quite the right solution, but deals with multi-line doc
  ;; strings
  (if (looking-at (concat "[ \t]*\\(" fsharp-stringlit-re "\\)"))
      (goto-char (match-end 0)))
  ;;
  (forward-line 1)
  (let (state)
    (while (and (fsharp-continuation-line-p)
		(not (eobp)))
      ;; skip over the backslash flavor
      (while (and (fsharp-backslash-continuation-line-p)
		  (not (eobp)))
	(forward-line 1))
      ;; if in nest, zip to the end of the nest
      (setq state (fsharp-parse-state))
      (if (and (not (zerop (car state)))
	       (not (eobp)))
	  (progn
	    (parse-partial-sexp (point) (point-max) 0 nil state)
	    (forward-line 1))))))

(defun fsharp-statement-opens-block-p ()
  "Return t iff the current statement opens a block.
I.e., iff it ends with a colon that is not in a comment.  Point should
be at the start of a statement."
  (save-excursion
    (let ((start (point))
	  (finish (progn (fsharp-goto-beyond-final-line) (1- (point))))
	  (searching t)
	  (answer nil)
	  state)
      (goto-char start)
      (while searching
	;; look for a colon with nothing after it except whitespace, and
	;; maybe a comment

	(if (re-search-forward fsharp-block-opening-re finish t)
	    (if (eq (point) finish)	; note: no `else' clause; just
					; keep searching if we're not at
					; the end yet
		;; sure looks like it opens a block -- but it might
		;; be in a comment
		(progn
		  (setq searching nil)	; search is done either way
		  (setq state (parse-partial-sexp start
						  (match-beginning 0)))
		  (setq answer (not (nth 4 state)))))
	  ;; search failed: couldn't find another interesting colon
	  (setq searching nil)))
      answer)))

(defun fsharp-statement-closes-block-p ()
  "Return t iff the current statement closes a block.
I.e., if the line starts with `return', `raise', `break', `continue',
and `pass'.  This doesn't catch embedded statements."
  (let ((here (point)))
    (fsharp-goto-initial-line)
    (back-to-indentation)
    (prog1
	(looking-at (concat fsharp-block-closing-keywords-re "\\>"))
      (goto-char here))))

(defun fsharp-goto-beyond-block ()
  "Go to point just beyond the final line of block begun by the current line.
This is the same as where `fsharp-goto-beyond-final-line' goes unless
we're on colon line, in which case we go to the end of the block.
Assumes point is at the beginning of the line."
  (if (fsharp-statement-opens-block-p)
      (fsharp-mark-block nil 'just-move)
    (fsharp-goto-beyond-final-line)))

(defun fsharp-goto-statement-at-or-above ()
  "Go to the start of the first statement at or preceding point.
Return t if there is such a statement, otherwise nil.  `Statement'
does not include blank lines, comments, or continuation lines."
  (fsharp-goto-initial-line)
  (if (looking-at fsharp-blank-or-comment-re)
      ;; skip back over blank & comment lines
      ;; note:  will skip a blank or comment line that happens to be
      ;; a continuation line too
      (if (re-search-backward "^[ \t]*\\([^ \t\n]\\|//\\)" nil t)
	  (progn (fsharp-goto-initial-line) t)
	nil)
    t))

(defun fsharp-goto-statement-below ()
  "Go to start of the first statement following the statement containing point.
Return t if there is such a statement, otherwise nil.  `Statement'
does not include blank lines, comments, or continuation lines."
  (beginning-of-line)
  (let ((start (point)))
    (fsharp-goto-beyond-final-line)
    (while (and
	    (or (looking-at fsharp-blank-or-comment-re)
		(fsharp-in-literal))
	    (not (eobp)))
      (forward-line 1))
    (if (eobp)
	(progn (goto-char start) nil)
      t)))

(defun fsharp-go-up-tree-to-keyword (key)
  "Go to begining of statement starting with KEY, at or preceding point.

KEY is a regular expression describing a Fsharp keyword.  Skip blank
lines and non-indenting comments.  If the statement found starts with
KEY, then stop, otherwise go back to first enclosing block starting
with KEY.  If successful, leave point at the start of the KEY line and
return t.  Otherwise, leave point at an undefined place and return nil."
  ;; skip blanks and non-indenting //
  (fsharp-goto-initial-line)
  (while (and
	  (looking-at "[ \t]*\\($\\|//[^ \t\n]\\)")
	  (zerop (forward-line -1)))	; go back
    nil)
  (fsharp-goto-initial-line)
  (let* ((re (concat "[ \t]*" key "\\>"))
	 (case-fold-search nil)		; let* so looking-at sees this
	 (found (looking-at re))
	 (dead nil))
    (while (not (or found dead))
      (condition-case nil		; in case no enclosing block
	  (fsharp-goto-block-up 'no-mark)
	(error (setq dead t)))
      (or dead (setq found (looking-at re))))
    (beginning-of-line)
    found))

(defun fsharp-suck-up-leading-text ()
  "Return string in buffer from start of indentation to end of line.
Prefix with \"...\" if leading whitespace was skipped."
  (save-excursion
    (back-to-indentation)
    (concat
     (if (bolp) "" "...")
     (buffer-substring (point) (progn (end-of-line) (point))))))

(defun fsharp-suck-up-first-keyword ()
  "Return first keyword on the line as a Lisp symbol.
`Keyword' is defined (essentially) as the regular expression
([a-z]+).  Returns nil if none was found."
  (let ((case-fold-search nil))
    (if (looking-at "[ \t]*\\([a-z]+\\)\\>")
	(intern (buffer-substring (match-beginning 1) (match-end 1)))
      nil)))

(defun fsharp-current-defun ()
  "Fsharp value for `add-log-current-defun-function'.
This tells add-log.el how to find the current function/method/variable."
  (save-excursion

    ;; Move back to start of the current statement.

    (fsharp-goto-initial-line)
    (back-to-indentation)
    (while (and (or (looking-at fsharp-blank-or-comment-re)
		    (fsharp-in-literal))
		(not (bobp)))
      (backward-to-indentation 1))
    (fsharp-goto-initial-line)

    (let ((scopes "")
	  (sep "")
	  dead assignment)

      ;; Check for an assignment.  If this assignment exists inside a
      ;; def, it will be overwritten inside the while loop.  If it
      ;; exists at top lever or inside a class, it will be preserved.

      (when (looking-at "[ \t]*\\([a-zA-Z0-9_]+\\)[ \t]*=")
	(setq scopes (buffer-substring (match-beginning 1) (match-end 1)))
	(setq assignment t)
	(setq sep "."))

      ;; Prepend the name of each outer socpe (def or class).

      (while (not dead)
	(if (and (fsharp-go-up-tree-to-keyword "\\(class\\|def\\)")
		 (looking-at
		  "[ \t]*\\(class\\|def\\)[ \t]*\\([a-zA-Z0-9_]+\\)[ \t]*"))
	    (let ((name (buffer-substring (match-beginning 2) (match-end 2))))
	      (if (and assignment (looking-at "[ \t]*def"))
		  (setq scopes name)
		(setq scopes (concat name sep scopes))
		(setq sep "."))))
	(setq assignment nil)
	(condition-case nil		; Terminate nicely at top level.
	    (fsharp-goto-block-up 'no-mark)
	  (error (setq dead t))))
      (if (string= scopes "")
	  nil
	scopes))))


;;; fsharp-mode.el ends here
(defun fsharp-eval-phrase ()
  "Send current (top-level) phrase to the interactive mode"
  (interactive)
  (save-excursion
    (fsharp-beginning-of-block)
    (setq p1 (point))
    (fsharp-end-of-block)
    (setq p2 (point))
    (fsharp-eval-region p1 p2)))

(defun fsharp-mark-phrase ()
  "Send current (top-level) phrase to the interactive mode"
  (interactive)
  (fsharp-beginning-of-block)
  (push-mark (point))
  (fsharp-end-of-block)
  (exchange-point-and-mark)
  (fsharp-keep-region-active))

(defun continuation-p ()
  "Return t iff preceding line is not a finished expression (ends with an operator)"
  (save-excursion
    (beginning-of-line)
    (forward-line -1)
    (looking-at fsharp-continued-re)))

(defun fsharp-beginning-of-block ()
  "Move point to the beginning of the current top-level block"
  (interactive)
  (condition-case nil
      (while (fsharp-goto-block-up 'no-mark))
    (error (while (continuation-p)
             (forward-line -1))))
  (beginning-of-line))

(defun fsharp-end-of-block ()
  "Move point to the end of the current top-level block"
  (interactive)
  (forward-line 1)
  (beginning-of-line)
  (condition-case nil
      (progn (re-search-forward "^[a-zA-Z#0-9(]")
             (while (continuation-p)
               (forward-line 1))
             (forward-line -1))
    (error
     (progn (goto-char (point-max)))))
  (end-of-line))

(provide 'fsharp-indent)
