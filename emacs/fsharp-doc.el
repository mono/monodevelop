;;; fsharp-doc.el -- show information for F# symbol at point.
;;
;; Filename: fsharp-doc.el
;; Author: Chris Barrett <chris.d.barrett@me.com>
;; Maintainer: Chris Barrett <chris.d.barrett@me.com>
;; Keywords: fsharp, languages
;;
;; This file is not part of GNU Emacs.
;;
;; This program is free software; you can redistribute it and/or
;; modify it under the terms of the GNU General Public License as
;; published by the Free Software Foundation; either version 3, or
;; (at your option) any later version.
;;
;; This program is distributed in the hope that it will be useful,
;; but WITHOUT ANY WARRANTY; without even the implied warranty of
;; MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
;; General Public License for more details.
;;
;; You should have received a copy of the GNU General Public License
;; along with this program; see the file COPYING.  If not, write to
;; the Free Software Foundation, Inc., 51 Franklin Street, Fifth
;; Floor, Boston, MA 02110-1301, USA.
;;
;;; Commentary:
;;
;; This is part of fsharp-mode for Emacs. It communicates with the F#
;; completion process to provide information for the symbol at point.
;;
;; This should be loaded automatically by fsharp-mode. Otherwise, add
;; this file to your load path, then call
;;
;;   (autoload 'turn-on-fsharp-doc-mode "fsharp-doc.el")
;;   (add-hook 'fsharp-mode-hook 'turn-on-fsharp-doc-mode)
;;
;; This file requires `namespaces`. It is available from MELPA, or from
;; GitHub at
;; https://raw.github.com/chrisbarrett/elisp-namespaces/master/namespaces.el
;;
;;; Code:

(require 'namespaces)

(namespace fsharp-doc
  :export
  [format-for-minibuffer]
  :import
  [fsharp-mode-completion])

(defvar fsharp-doc-idle-delay 0.5
  "The number of seconds to wait for input idle before showing a tooltip.")

(define-minor-mode fsharp-doc-mode
  "Display F# documentation in the minibuffer."
  nil
  ""
  nil
  ;; Body
  (in-ns fsharp-doc
    (_ reset-timer)
    (when fsharp-doc-mode
      (_ start-timer)
      (run-hooks 'fsharp-doc-mode-hook))
    fsharp-doc-mode))

(defun turn-on-fsharp-doc-mode ()
  (fsharp-doc-mode t))

(defun turn-off-fsharp-doc-mode ()
  (fsharp-doc-mode nil))

;;; -----------------------------------------------------------------------------

(defmutable timer nil)

(defn start-timer ()
  (unless (@ timer)
    (@set timer (run-with-idle-timer fsharp-doc-idle-delay t (~ show-tooltip)))))

(defn reset-timer ()
  (when (@ timer)
    (cancel-timer (@ timer))
    (@set timer nil)))

;;; ----------------------------------------------------------------------------

(defn format-for-minibuffer (str)
  "Parse the result from the F# process."
  (destructuring-bind (x &rest xs) (split-string str "[\r\n]")
    (let ((line (if (string-match-p "^Multiple" x) (car-safe xs) x))
          (name (_ extract-full-name str)))
      (_ tidy-result
         (cond
          ;; Don't fully-qualify let-bindings.
          ((string-match-p "^val" line)
           line)

          ;; Extract type identifier.
          (name
           (_ replace-identifier line name))

          (t
           line))))))

(defn extract-full-name (str)
  (when (string-match "Full name: \\(.*\\)$" str)
    (match-string 1 str)))

(defn replace-identifier (str fullname)
  (replace-regexp-in-string
   "^\\w+ \\(public \\|private \\|internal \\)?\\(.*?\\) "
   fullname str 'fixcase "\2" 2))

(defn tidy-result (str)
  (replace-regexp-in-string "[ ]*=[ ]*" "" str))

;;; ----------------------------------------------------------------------------

(defmutable prevpoint nil)

(defn show-tooltip ()
  "Show tooltip info in the minibuffer.
If there is an error or warning at point, show the error text.
Otherwise, request a tooltip from the completion process."
  (interactive)
  (when (and fsharp-doc-mode (thing-at-point 'symbol))
    (unless (or (equal (point) (@ prevpoint))
                executing-kbd-macro
                (_ fsharp-overlay-at (point))
                (active-minibuffer-window)
                cursor-in-echo-area)
      (@set prevpoint (point))
      (_ show-typesig-at-point))))

;;; fsharp-doc.el ends here
