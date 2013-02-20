(require 'test-common)

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

(defun parse-into-lines (str)
  (split-string (fsharp-doc/format-for-minibuffer str) "[\n\r]"))

;;; Vals

(check "parsed val should be a single line"
  (let ((lines (parse-into-lines val-tooltip)))
    (should (= 1 (length lines)))))

(check "parsed val should start with 'val'"
  (should-match "^val" (fsharp-doc/format-for-minibuffer val-tooltip)))

(check "parsed val should contain type signature"
  (should-match ": x:'a -> b$" (fsharp-doc/format-for-minibuffer val-tooltip)))

(check "parsed val should not be module-qualified"
  (should-match "^val id" (fsharp-doc/format-for-minibuffer val-tooltip)))

;;; Types

(check "parsed type should be a single line"
  (let ((lines (parse-into-lines type-tooltip)))
    (should (= 1 (length lines)))))

(check "parsed type should start with 'type'"
  (should-match "^type" (fsharp-doc/format-for-minibuffer type-tooltip)))

(check "type identifier should be module-qualified"
  (should-match "^type Modid.id" (fsharp-doc/format-for-minibuffer type-tooltip)))

;;; Properties

(check "parsed property should be a single line"
  (let ((lines (parse-into-lines property-tooltip)))
    (should (= 1 (length lines)))))

(check "parsed property should start with 'property'"
  (should-match "^property" (fsharp-doc/format-for-minibuffer property-tooltip)))

(check "parsed property should end with type"
  (should-match ": a$" (fsharp-doc/format-for-minibuffer property-tooltip)))

(check "property identifier should be qualified"
  (should-match "^property Modid.id" (fsharp-doc/format-for-minibuffer property-tooltip)))

;;; Objects

(check "parsed object should be a single line"
  (let ((lines (parse-into-lines object-tooltip)))
    (should (= 1 (length lines)))))

(check "parsed object should start with 'type'"
  (should-match "^type" (fsharp-doc/format-for-minibuffer object-tooltip)))

(check "parsed object should be fully qualified"
  (should-match "^type Modid.id" (fsharp-doc/format-for-minibuffer object-tooltip)))
