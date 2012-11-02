;;; tooltip-help.el --- show help as tooltip

;; Copyright (C) 2007  Tamas Patrovics

;; This file is free software; you can redistribute it and/or modify
;; it under the terms of the GNU General Public License as published by
;; the Free Software Foundation; either version 2, or (at your option)
;; any later version.

;; This file is distributed in the hope that it will be useful,
;; but WITHOUT ANY WARRANTY; without even the implied warranty of
;; MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
;; GNU General Public License for more details.

;; You should have received a copy of the GNU General Public License
;; along with GNU Emacs; see the file COPYING.  If not, write to the
;; Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
;; Boston, MA 02110-1301, USA.

;;; Commentary:

;; F1 is a useless key on my keyboard, because I don't use it in
;; Emacs, so I thought I bound some kind of help function on it.
;;
;; I thought some kind of help which is less intrusive than the
;; default one (doesn't open a new window, frame, etc.) would be
;; useful, so I made one using tooltips.
;;
;; When using in a Lisp program the tooltip is displayed without
;; osbcuring the position of the cursor and it is dismissed
;; automatically when the user continues typing, so it doesn't disrupt
;; the current window configuration like the current help does.
;;
;; How it works:
;;
;; If the cursor is ON a symbol then help is shown for that symbol.
;;
;; If the cursor is after or before a symbol then the function symbol
;; belonging to the containing sexp is used. If no such symbol is
;; found then a nearby symbol is tried.
;;
;; If the symbol has both function and variable bindings then both of
;; them are shown together in the tooltip.
;;
;; When function help is shown and the cursor is in the argument list
;; then the relevant argument is highlighted in the documentation.
;; (Using code copied from Emacs Wiki.)
;;
;; Support can be added by creating a function with a name like this:
;;
;;   th-<major-mode>-handler
;;
;; Currently only Emacs lisp mode is supported.
;;

;; Tested on Gnu Emacs 21.

;;; Code:

(require 'help)
(require 'eldoc)


(global-set-key (kbd "<f1>") 'th-show-help)

(defun th-show-help ()
  (interactive)
  (let ((handler (intern (concat "th-" (symbol-name major-mode) "-handler"))))
    (if (functionp handler)
        (let ((help (funcall handler)))
          (if (equal help "")
              (message "No help available.")

            (th-show-tooltip-for-point help)
            (message "")))
          
      (message "The current major mode is not supported."))))


(defun th-lisp-interaction-mode-handler ()
  (th-emacs-lisp-mode-handler))


(defun th-emacs-lisp-mode-handler ()
  (let* ((argument nil)
         (symbol (if (and (char-after)
                          (or (eq (char-syntax (char-after)) ?w)
                              (eq (char-syntax (char-after)) ?_)))
                     (intern-soft (current-word))

                   (or (let ((func (eldoc-fnsym-in-current-sexp)))
                         (when func
                           (setq argument t)
                           func))
                       (intern-soft (current-word)))))
        (help ""))

    (if (boundp symbol)
        (setq help (th-elisp-get-help-text 'describe-variable symbol)))

    (when (functionp symbol)
      (let ((funhelp (th-elisp-get-help-text 'describe-function symbol)))
        (unless (equal funhelp "")
          (if argument
              (setq funhelp (th-elisp-highlight-current-function-arg funhelp)))
          (unless (equal help "")
            (setq help (concat help "\n\n------------------------------\n\n")))
          (setq help (concat help funhelp)))))

    help))


(defun th-elisp-get-help-text (func symbol)
  (let ((pop-up-frames nil)
        (wincfg (current-window-configuration)))
    (if (get-buffer "*Help*")
        (kill-buffer "*Help*"))

    (funcall func symbol)
    (if (get-buffer "*Help*")
        (progn
          (set-window-configuration wincfg)
          (with-current-buffer "*Help*" (buffer-string)))
      "")))


(defun th-elisp-highlight-current-function-arg (doc)
  (let ((lines (split-string doc "\n")))
    (if (> (length lines) 1)
        (let ((first (pop lines))
              (second (pop lines)))
          (concat first "\n"
                  (eldoc-highlight-nth-arg 
                   second (eldoc-get-arg-index)) "\n"
                   (mapconcat 'identity lines "\n")))
        doc)))


;; Function arg code is copied from Eldoc page of Emacs Wiki. Written
;; by user rubikitch.

(defun eldoc-get-arg-index ()
  (save-excursion
    (let ((fn (eldoc-fnsym-in-current-sexp))
          (i 0))
      (unless (memq (char-syntax (char-before)) '(32 39)) ; ? , ?'
        (condition-case err
            (backward-sexp)             ;for safety
          (error 1)))
      (condition-case err
          (while (not (equal fn (eldoc-current-symbol)))
            (setq i (1+ i))
            (backward-sexp))
        (error 1))
      (max 0 i))))

(defun eldoc-highlight-nth-arg (doc n)
  (cond ((null doc) "")
        ((<= n 0) doc)
        (t
         (let ((i 0))
           (mapconcat
            (lambda (arg)
              (if (member arg '("&optional" "&rest"))
                  arg
                (prog2
                    (if (= i n)
                        (put-text-property 0 (length arg) 'face 'bold arg))
                    arg
                  (setq i (1+ i)))))
            (split-string doc) " ")))))

;; tooltip position code is copied from esense

(defvar th-max-tooltip-lines 25
  "The maximum number of lines shown in a tooltip.
The tooltip is truncated if necessary.")

(if (eq window-system 'w32)
    (defcustom th-titlebar-height 30
      "Height of Emacs window titlebar. It mostly depends on your window
manager settings. Correct titlebar height will help tooltip-help to display
popup windows in a proper position."
      :type 'integer
      :group 'th)

  (defconst th-titlebar-height 0
    "On Linux the title bar is not the part of the window, so we
don't have to consider its height in calculations."))

(defun th-show-tooltip-for-point (msg &optional x y)
  "Show tooltip MSG at point or at X Y if given."
  (let ((lines (split-string msg "\n")))
    (when (> (length lines) 
             th-max-tooltip-lines)
      (setq lines 
            (append
             (subseq lines
                     0 
                     (1- th-max-tooltip-lines))
             (list 
              (concat "(Further lines not shown "
                      "due to line number limit.)"))))
      (setq msg (mapconcat (lambda (x) x)
                           lines "\n")))

    (th-show-tooltip-for-point-gnuemacs msg lines x y)))


(defun th-show-tooltip-for-point-gnuemacs (msg lines &optional x y)
  "Show tooltip MSG at point or at X Y if given.
LINES is the same as MSG split into individual lines."
  (let* ((tooltip-width (* (frame-char-width)
                           (apply 'max (mapcar 'length lines))))
         (tooltip-height (* (frame-char-height) (min (length lines)
                                                     (cdr x-max-tooltip-size))))
         (position (th-calculate-popup-position tooltip-width
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



(defun th-calculate-popup-position (width height preferred-pos &optional x y)
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
                           (th-get-pixel-position x y)
                         (get-point-pixel-position))))
         (point-y (cdr (if y
                           (th-get-pixel-position x y)
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
                           th-titlebar-height
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
      (defalias 'th-window-edges
        'window-inside-edges)
    ;; Emacs 21
    (defalias 'th-window-edges
      'window-edges)
    ))
 
(defun th-point-position ()
  "Return the location of POINT as positioned on the selected frame.
Return a cons cell (X . Y)"
  (let* ((w (selected-window))
         (f (selected-frame))
         (edges (th-window-edges w))
         (col (current-column))
         (row (count-lines (window-start w) (point)))
         (x (+ (car edges) col))
         (y (+ (car (cdr edges)) row)))
    (cons x y)))


(defun get-point-pixel-position ()
  "Return the position of point in pixels within the frame."
  (let ((point-pos (th-point-position)))
    (th-get-pixel-position (car point-pos) (cdr point-pos))))
 

(defun th-get-pixel-position (x y)
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


(provide 'tooltip-help)
;;; tooltip-help.el ends here
