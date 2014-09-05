namespace ${Namespace}
open System
open Foundation
open ObjCRuntime
open UIKit
open Conversions

type KeyboardViewController (handle : nativeint) = 
    inherit UIInputViewController (handle)
    
    let mutable nextKeyboardButton = null

    override x.DidReceiveMemoryWarning () =
        // Releases the view if it doesn't have a superview.
        base.DidReceiveMemoryWarning ()
        // Release any cached data, images, etc that aren't in use.

    override x.UpdateViewConstraints () =
        base.UpdateViewConstraints ()
        // Add custom view sizing constraints here

    override x.ViewDidLoad () =
        base.ViewDidLoad ()

        // Perform custom UI setup here
        nextKeyboardButton <- new UIButton (UIButtonType.System)
        nextKeyboardButton.SetTitle ("Next Keyboard", UIControlState.Normal)
        nextKeyboardButton.SizeToFit ()
        nextKeyboardButton.TranslatesAutoresizingMaskIntoConstraints <- false
        nextKeyboardButton.AddTarget (x, new Selector ("advanceToNextInputMode"), UIControlEvent.TouchUpInside)

        x.View.AddSubview (nextKeyboardButton)

        let leftSideConstraint = NSLayoutConstraint.Create (nextKeyboardButton, NSLayoutAttribute.Left, NSLayoutRelation.Equal, x.View, NSLayoutAttribute.Left, nfloat 1.0f, nfloat 0.0f)
        let bottomConstraint = NSLayoutConstraint.Create (nextKeyboardButton, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, x.View, NSLayoutAttribute.Bottom, nfloat 1.0f, nfloat 0.0f)
        x.View.AddConstraints [|leftSideConstraint; bottomConstraint|]

    override x.TextWillChange (textInput) = ()
        // The app is about to change the document's contents. Perform any preparation here.

    override x.TextDidChange (textInput) =
        // The app has just changed the document's contents, the document context has been updated.
        let textColor =
            if x.TextDocumentProxy.KeyboardAppearance = UIKeyboardAppearance.Dark then UIColor.White
            else UIColor.Black

        nextKeyboardButton.SetTitleColor (textColor, UIControlState.Normal)