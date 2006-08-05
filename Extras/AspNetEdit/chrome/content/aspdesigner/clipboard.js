 /*
 * clipboard.js - methods for manipulating the clipboard
 * 
 * Authors: 
 *  Blagovest Dachev <blago@dachev.com>
 *  
 * Copyright (C) 2005 Blagovest Dachev
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */


function clipboard(){

	/* ********************************************************************
	/ PRIVATE VARIABLES AND FUNCTIONS
	/ only priveleged methdos may view/edit/invoke
	**********************************************************************/



	/* ********************************************************************
	/ PRIVILEGED METHODS
	/ may be invoked and may access private items
	/ may not be changed; may be replaced with public flavors
	**********************************************************************/


	/* ********************************************************************
	/ PUBLIC PROPERTIES
	/ anyone may read/write
	**********************************************************************/
}

/* ****************************************************************************
/ PUBLIC METHODS
/ anyone may read/write
******************************************************************************/
clipboard.prototype.setClipboard = function(aNewContent)
{
	try {
		var str = Components.classes ['@mozilla.org/supports-string;1'].
			createInstance (Components.interfaces.nsISupportsString);
		str.data = aNewContent;

		var trans = Components.classes ['@mozilla.org/widget/transferable;1'].
			createInstance (Components.interfaces.nsITransferable);
		trans.addDataFlavor (TEXT_HTML);
		trans.addDataFlavor (TEXT_UNICODE);
		trans.setTransferData (TEXT_HTML, str, aNewContent.length * 2);
		// TODO: extract only actual text and get rid of all html
		trans.setTransferData (TEXT_UNICODE, str, aNewContent.length * 2);

		var clipid = Components.interfaces.nsIClipboard;
		var clip = Components.classes ['@mozilla.org/widget/clipboard;1'].
					getService (clipid);
		clip.setData (trans, null, clipid.kGlobalClipboard);
	} catch (e) {dump (e)}
}

clipboard.prototype.getClipboard = function()
{
	try {
		var clip = Components.classes ['@mozilla.org/widget/clipboard;1'].
				getService (Components.interfaces.nsIClipboard);
		if (!clip)
			return false;

		var trans = Components.classes ['@mozilla.org/widget/transferable;1'].
				createInstance (Components.interfaces.nsITransferable);
		if (!trans)
			return false;

		trans.addDataFlavor (TEXT_HTML);
		trans.addDataFlavor (TEXT_UNICODE);

		clip.getData (trans, clip.kGlobalClipboard);

		var dataObj = new Object();
		var bestFlavor = new Object();
		var len = new Object();
		trans.getAnyTransferData (bestFlavor, dataObj, len);
		if (bestFlavor.value == TEXT_HTML ||
		    bestFlavor.value == TEXT_UNICODE) {
			if ( dataObj )
				dataObj = dataObj.value.
					QueryInterface (Components.interfaces.nsISupportsString);
			if ( dataObj ) {
				var id = dataObj.data.substring	(0, len.value / 2);
			}
		}
		return id;
	} catch (e) {dump (e)}
}

/* ****************************************************************************
/ PROTOTYOPE PROERTIES
/ anyone may read/write (but may be overridden)
******************************************************************************/


/* ****************************************************************************
/ STATIC PROPERTIES
/ anyone may read/write
******************************************************************************/
