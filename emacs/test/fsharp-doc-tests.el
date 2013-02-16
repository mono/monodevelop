(provide 'fsharp-doc-tests)
(require 'ert)

(defmacro check (desc &rest body)
  "Wrap ert-deftest with a simpler interface."
  (declare (indent 1))
  `(ert-deftest
       ,(intern (replace-regexp-in-string "[ .]" "-" desc)) ()
     (in-ns fsharp-doc
       ,@body)))

;;; ----------------------------------------------------------------------------
;;; Test data

(defconst val-tooltip
  "val id : x:'a -> b

Full name: Modid.id")


(defconst property-tooltip
  "property id : a

Full name: Modid.id")


(defconst type-tooltip
  "type id = string

Full name: Modid.id")


(defconst object-tooltip
  "Multiple items
type id =
  new : unit -> Has
  static member All : ConstraintExpression
  static member Attribute<'T> : unit -> ResolvableConstraintExpression + 1 overload
  static member Count : ResolvableConstraintExpression
  static member Exactly : expectedCount:int -> ConstraintExpression
  static member InnerException : ResolvableConstraintExpression
  static member Length : ResolvableConstraintExpression
  static member Member : expected:obj -> CollectionContainsConstraint
  static member Message : ResolvableConstraintExpression
  static member No : ConstraintExpression
  ...

Full name: Modid.id
")

;;; ----------------------------------------------------------------------------
;;; Tests

(check "parsed binding should be a single line"
  (let ((lines (split-string (_ format-for-minibuffer val-tooltip) "[\n\r]")))
    (should (= 1 (length lines)))))

;;; vals

(check "parsed val should start with 'val'"
  (should (string-match-p "^val" (_ format-for-minibuffer val-tooltip))))

(check "parsed val should contain type signature"
  (should (string-match-p ": x:'a -> b$" (_ format-for-minibuffer val-tooltip))))

(check "parsed val should be module-qualified"
  (should (string-match-p "^val Modid.id" (_ format-for-minibuffer val-tooltip))))

;;; types

(check "parsed type should start with 'type'"
  (should (string-match-p "^type" (_ format-for-minibuffer type-tooltip))))

(check "type identifier should be module-qualified"
  (should (string-match-p "^type Modid.id" (_ format-for-minibuffer type-tooltip))))

;;; properties

(check "parsed property should start with 'property'"
  (should (string-match-p "^property" (_ format-for-minibuffer property-tooltip))))

(check "parsed property should end with type"
  (should (string-match-p ": a$" (_ format-for-minibuffer property-tooltip))))

(check "property identifier should be qualified"
  (should (string-match-p "^property Modid.id" (_ format-for-minibuffer property-tooltip))))

;;; Objects

(check "parsed object should start with 'type'"
  (should (string-match-p "^type" (_ format-for-minibuffer object-tooltip))))

(check "parsed object should be fully qualified"
  (should (string-match-p "^type Modid.id" (_ format-for-minibuffer object-tooltip))))
