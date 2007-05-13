 /*
 * editor.js - The asp editor object
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

var editor                 = null;
var host                   = null;
var gDirectivePlaceholder  = '';
var clip                   = null;



//* ___________________________________________________________________________
// This class is responsible for communication with the host system and
// implements part of the AspNetDesigner interface
//_____________________________________________________________________________

function aspNetHost()
{

}
aspNetHost.prototype =
{
	mSerializedContent                     : '',
	mDeserializedContent                   : '',

	initialize: function()
	{
		//tell the host we're ready for business
		JSCallPlaceClrCall ('Activate', '', '');
	},

	click: function(aType, aControlId)
	{
		var clickType;

		if(aType == SINGLE_CLICK) {
			clickType = 'Single';
		}
		else if(aType == DOUBLE_CLICK) {
			clickType = 'Double';
		}
		else if(aType == RIGHT_CLICK) {
			clickType = 'Right';
		}
		
 		if(!aControlId) {
			dump (clickType +
				' click over no control; deselecting all controls');
		}
		else {
			dump (clickType + " click over aspcontrol \"" +
				aControlId + "\"");
		}
		
		JSCallPlaceClrCall ('Click', '', new Array(clickType, aControlId));
		dump ('Outbound call to Click() ' + aControlId);
	},

	resizeControl: function(aControlId, aWidth, aHeight)
	{
		dump ('Outbound call to ResizeControl() id=' + aControlId +
			', new size ' + aWidth + 'x' + aHeight);
		JSCallPlaceClrCall ('ResizeControl', '',
			new Array(aControlId, aWidth, aHeight));
	},
	
	removeControl: function (aControlId)
	{
		dump ('Outbound call to removeControl() id=' + aControlId);
		JSCallPlaceClrCall ('RemoveControl', '', new Array(aControlId));
	},
	
	serialize: function (aClipboardContent, aReturn)
	{
		// On first invocation we call the host. It then returns to
		// SerializeReturn which in turn invokes this method again
		// and sets the member var with the new clipboard content
		// ("else" block). First-invocation execution picks up after
		// the host call, and we are able to return the new content
		// (mSerializedContent) to the caller.
		if (!aReturn) {
			dump ('Outbound call to Serialize() content=' +
				aClipboardContent);
			JSCallPlaceClrCall ('Serialize', 'SerializeReturn',
				new Array(aClipboardContent));
			return this.mSerializedContent;
		}
		else {
			this.mSerializedContent = aClipboardContent;
		}
	},
	
	deserializeAndAdd: function (aClipboardContent, aReturn)
	{
		// On first invocation we call the host. It then returns to
		// SerializeReturn which in turn invokes this method again
		// and sets the member var with the new clipboard content
		// ("else" block). First-invocation execution picks up after
		// the host call, and we are able to return the new content
		// (mDeserializedContent) to the caller.
		if (!aReturn) {
			dump ('Outbound call to DeserializeAndAdd() content=' +
				aClipboardContent);
			JSCallPlaceClrCall ('DeserializeAndAdd',
					'DeserializeAndAddReturn',
					new Array(aClipboardContent));
			return this.mDeserializedContent;
		}
		else {
			this.mDeserializedContent = aClipboardContent;
		}
	},

	throwException: function (location, msg)
	{
		JSCallPlaceClrCall ('ThrowException', '', new Array(location, msg));
	}
}


/* Directly hooking up the editor's methods as JSCall handler functions
 * means that their access to 'this' is broken so we use these
 * surrogate functions instead
 */

function JSCall_SelectControl (aControlId) {	aAdd = true;
	aPrimary = true;
	return editor.selectControl (aControlId, aAdd, aPrimary);
}

function JSCall_UpdateControl (aControlId, aNewDesignTimeHtml) {
	return editor.updateControl (aControlId, aNewDesignTimeHtml);
}

function JSCall_RemoveControl (aControlId) {
	return editor.removeControl (aControlId);
}

function JSCall_RenameControl (arg1, arg2) {
	return editor.renameControl (arg1, arg2);
}

function JSCall_AddControl (aControlId, aControlHtml) {	return editor.addControl (aControlHtml, aControlId);
}

function JSCall_GetPage () {
	return editor.getPage ();
}

function JSCall_LoadPage (pageHtml) {
	return editor.loadPage (pageHtml);
}

function JSCall_DoCommand (command) {
	editor.doCommand (command);
	return "";
}

function JSCall_IsCommandEnabled (command) {
	return editor.isCommandEnabled (command);
}

function JSCall_InsertFragment (fragment) {
	return editor.insertFragment (fragment);
}

function JSCall_SerializeReturn (arg) {
	host.serialize (arg, true);
}

function JSCall_DeserializeAndAddReturn (arg) {
	host.deserializeAndAdd (arg, true);
}

//* ___________________________________________________________________________
// A rather strange data structure to store current controls in the page.
// Insertion is O(1), removal is O(n), memory usage is O(2n), but query by
// index and id are both O(1). We have to keep track of controls on the
// Mozilla side, so it's easier to handle special cases like undo() that
// restores a deleted control
// TODO: store deleted controls' html (maybe more) in a deleted controls array.
// This will help reinstating controls on the host side.
// TODO: keep a counter of the undo redo operation that involve control
// deletion and insertion and peg it to the editor's counter
// !!! Do we really need it?
//_____________________________________________________________________________
var controlTable = {
	hash                      : new Array (),
	array                     : new Array (),
	length                    : 0,

	add: function(aControlId, aControlRef)
	{
		if(this.hash [aControlId]) {
			dump ('Panic: atempt to add an already existing control with id=' +
				aControlId +
				'. Remove first.');
		}
		else {
			this.hash [aControlId] = aControlRef;
			this.array.push (aControlRef);
			this.length++;
		}
	},

	remove: function(aControlId)
	{
		if(this.hash [aControlId]) {
			var i = 0;
			while(this.array [i] != this.hash[aControlId]) {
				i++;
			}
			this.array.splice (i, 1);
			this.hash [aControlId] = null;
			this.length--;
		}
		else {
			dump ('Panic: atempt to remove control with unexisting id=' +
				aControlId);
		}
	},

	update: function(aControlId, aControlRef)
	{
		this.remove (aControlId);
		this.add (aControlId, aControlRef);
	},
 
	getById: function(aControlId)
	{
		return this.hash [aControlId];
	},

	getByIndex: function(aIndex)
	{
		return this.array [aIndex];
	},

	getCount: function()
	{
		return this.length;
	}
}

//* ___________________________________________________________________________
// The editor class and initialization
//_____________________________________________________________________________
function aspNetEditor_initialize()
{
	//host XUL doc's onload event fires twice for some reason
	if (editor != null)
		return;
	
	dump ("Initialising...");
	editor = new aspNetEditor ();
	dump ("\tCreated editor, initialising...");
	editor.initialize ();
	dump ("\tEditor initialised, creating host...");
	host = new aspNetHost ();
	dump ("\tHost created, initialising...");
	host.initialize ();
	dump ("\tHost initialised, creating clipboard...");
	clip = new clipboard ();
	dump ("\tClipboard created.");
	dump ("Initialised.");
}

function aspNetEditor()
{

}

aspNetEditor.prototype = 
{
	mNsIHtmlEditor                : null,
	mNsIEditor                    : null,
	mNsIHtmlObjectResizer         : null,
	mNsIHTMLInlineTableEditor     : null,
	mNsIEditorStyleSheets         : null,
	mNsICommandManager            : null,

	mEditorElement                : null,
	mEditorWindow                 : null,
	mDropInElement                : null,
	mControlTable                 : null,
	mLastDeletedControls          : null,
	mLastSelectedControls         : null,
	mCancelClick                  : false,
	mInResize                     : false,
	mInCommandExec                : false,
	mInUpdate                     : false,
	mInDrag                       : false,

	mLastClipboardUpdateCommand   : '',

	initialize: function()
	{
		this.mEditorElement = document.getElementById ('aspeditor');
		this.mEditorElement.makeEditable ('html', false);

		this.mEditorWindow = this.mEditorElement.contentWindow;
		this.mNsIHtmlEditor =
			this.mEditorElement.getHTMLEditor(this.mEditorWindow);
		this.mNsICommandManager =
			this.mEditorElement.commandManager;
		this.mNsIEditor =
			this.mNsIHtmlEditor.QueryInterface(EDITOR);
		this.mNsIHTMLInlineTableEditor =
			this.mNsIHtmlEditor.QueryInterface(INLINE_TABLE_EDITOR);
		this.mNsIHtmlObjectResizer =
			this.mNsIHtmlEditor.QueryInterface(OBJECT_RESIZER);

		var selectionPrivate =
			this.base.selection.QueryInterface (SELECTION_PRIVATE);
		selectionPrivate.addSelectionListener (gNsISelectionListenerImplementation);
		this.mNsIHtmlEditor.addObjectResizeEventListener (gNsIHTMLObjectResizeListenerImplementation);
		this.mNsIHtmlEditor.addEditActionListener (gNsIEditActionListenerImplementation);
		this.mNsICommandManager.addCommandObserver (gNsIObserverImplementation, 'cmd_bold');
		this.mNsICommandManager.addCommandObserver (gNsIObserverImplementation, 'cmd_italics');
		this.mNsICommandManager.addCommandObserver (gNsIObserverImplementation, 'cmd_underline');
		this.mNsICommandManager.addCommandObserver (gNsIObserverImplementation, 'cmd_indent');
		this.mNsICommandManager.addCommandObserver (gNsIObserverImplementation, 'cmd_outdent');
		// ?????????????????????????????????????????????????????????????????????????
		// Bug in Mozilla's InsertHTMLWithContext?
		//this.mNsIHtmlEditor.addInsertionListener (gNsIContentFilterImplementation);

		this.mLastDeletedControls  = new Array();
		this.mLastSelectedControls = new Array();
		this.mControlTable         = controlTable;
	},

	get inResize()           { return this.mInResize },
	set inResize(aBool)      { this.mInResize = aBool },

	get inUpdate()           { return this.mInUpdate },
	set inUpdate(aBool)      { this.mInUpdate = aBool },

	get dragState()          { return this.mInDrag; },
	set dragState(aBool)     { this.mInDrag = aBool },

	get inCommandExec()      { return this.mInCommandExec },
	set inCommandExec(aBool) { this.mInCommandExec = aBool },

	get cancelClick()        { return this.mCancelClick },
	set cancelClick(aBool)   { this.mCancelClick = aBool },

	get controlCount()       { return this.mControlTable.getCount (); },
	get editorWindow()       { return this.mEditorWindow },

	get base()
	{
		var editor;
		try {
			editor = this.mEditorElement.getEditor (this.mEditorWindow);

			editor instanceof Components.interfaces.nsIPlaintextEditor;
			editor instanceof Components.interfaces.nsIHTMLEditor;
		} catch (e) {
			dump("Could not obtain nsIHTMLEditor: " + e);
		}

		return editor;
	},

	beginBatch: function()
	{
		//this.mNsIHtmlEditor.transactionManager.beginBatch ();
	},

	endBatch: function()
	{
		//this.mNsIHtmlEditor.transactionManager.endBatch ();
	},

	removeFromControlTable: function(aControlId)
	{
		this.mControlTable.remove (aControlId);
	},

	insertInControlTable: function(aControlId, aControlRef)
	{
		this.mControlTable.add (aControlId, aControlRef);
	},

	getControlFromTableById: function(aControlId)
	{
		return this.mControlTable.getById (aControlId);
	},

	getControlFromTableByIndex: function(aIndex)
	{
		return this.mControlTable.getByIndex (aIndex);
	},
  
	updateControlInTable: function(aControlId, aControlref)
	{
		this.mControlTable.update (aControlId, aControlref);
	},

	getControlTable: function()
	{
		return this.mControlTable;
	},

	addLastDeletedControl: function(aControl)
	{
		this.mLastDeletedControls.push (aControl);
	},

	removeLastDeletedControl: function()
	{
		return this.mLastDeletedControls.pop ();
	},

	nextSiblingIsControl: function()
	{
		var next        = null;
		var focusNode   = this.base.selection.focusNode;
		var focusOffset = this.base.selection.focusOffset;
		// Are we at the end offset of a text node?
		if(this.atEndOfTextNode ()) {
			next = focusNode.nextSibling;
			if(next && this.nodeIsControl (next))
				return next;
		}
		// If not at the end offset of a text node, focus offset is our
		// current element; use it to get next
		else {
			next = focusNode.childNodes [focusOffset];
			if(next && this.nodeIsControl (next))
				return next;
		}
		return false;
	},

	previousSiblingIsControl: function()
	{
		var prev        = null;
		var focusNode   = this.base.selection.focusNode;
		var focusOffset = this.base.selection.focusOffset;
		// Are we at the beginning offset of a text node?
		if(this.atBeginningOfTextNode ()) {
			prev = focusNode.previousSibling;
			if(prev && this.nodeIsControl (prev))
				return prev;
		}
		// If not at the beginning offset of a text node, focus offset
		// minus 1 is our current element; use it to get next
		else {
			prev = focusNode.childNodes [focusOffset - 1];
			if(prev && this.nodeIsControl (prev))
				return prev;
		}
		return false;
	},

	atBeginningOfTextNode: function()
	{
		if(this.base.selection.focusNode) {
			var focusNode =
				this.base.selection.focusNode;
			var focusOffset =
				this.base.selection.focusOffset;
			// If we are at offset zero of a text node
			if(focusNode.nodeType == 3 && focusOffset == 0) {
				return true;
			}
			return false;
		}
		return false;
	},

	atEndOfTextNode: function()
	{
		if(this.base.selection.focusNode) {
			var focusNode =
				this.base.selection.focusNode;
			var focusOffset
				= this.base.selection.focusOffset;
			// Are we in a text node?
			if (focusNode.nodeType == 3) {
				var focusNodeLength = focusNode.nodeValue.length;
				// If we are at the end offset of a text node
				if(focusNodeLength == focusOffset)
					return true;
				else
					return false;
			}
			return false;
		}
		return false;
	},

	nodeIsControl: function(aNode)
	{
		var name = aNode.nodeName;
		name = name.toLowerCase ();
		if(name == CONTROL_TAG_NAME)
			return true;
		return false;
	},

	collapseBeforeInsertion: function(aPoint)
	{
		switch(aPoint) {
		case "start":
			this.base.selection.collapseToStart ();
			break;
		case   "end":
			this.base.selection.collapseToEnd ();
			break;
		}
	},

	transformBeforeInput: function(aHTML, aPageload)
	{
		// Give controls a default value
		var emptyControl =
			aHTML.match(EMPTY_CONTROL_EXP);
		var controlBegin = "$&" + APPEND_TO_CONTROL_BEGIN;
		controlBegin = (emptyControl) ?
					controlBegin + EMPTY_CONTROL_MSG :
					controlBegin;

		// Add the aux control spans and divs
		var htmlOut = aHTML.replace (BEGIN_CONTROL_TAG_EXP, controlBegin);
		htmlOut = htmlOut.replace (END_CONTROL_TAG_EXP,
					APPEND_TO_CONTROL_END + "$&");

		// Put comments around any script placeholders that we may have
		// in the HTML
		if (aPageload)
			htmlOut = htmlOut.replace (SCRIPT_PLACE_HOLDER_EXP,
						 '<!--' + "$&" + '-->');

		// Save any directive placeholders that we may have in the HTML
		if (aPageload) {
			gDirectivePlaceholder =
				htmlOut.match (DIRECTIVE_PLACE_HOLDER_EXP);
			if (!gDirectivePlaceholder)
				return htmlOut;
			htmlOut =
				htmlOut.replace (DIRECTIVE_PLACE_HOLDER_EXP, '');
		}
		return (htmlOut);
	},

	transformBeforeOutput: function(aHTML, aPageSave)
	{
		//alert (aHTML);
		// Strip any aux spans and divs from the controls
		var htmlOut = aHTML.replace(STRIP_CONTROL_EXP, '');

		if (aPageSave) {
			// Add back any directive placeholders
			if(gDirectivePlaceholder) {
				htmlOut = gDirectivePlaceholder + htmlOut;
			}

			// Strip the comments from all script placeholders
	 		htmlOut = htmlOut.replace (STRIP_SCRIPT_PLACE_HOLDER_EXP,
						"$1");
		}

		//alert (htmlOut);
		return (htmlOut);
	},

	//  Loading/Saving/ControlState
	loadPage: function(aHtml)
	{
		if(aHtml) {
			try {
				this.base.selectAll ();
				this.base.deleteSelection (1);
				var html = this.transformBeforeInput(aHtml, true);
				dump ("Loading page: " + html);
				this.mNsIHtmlEditor.rebuildDocumentFromSource (html);

				// Show caret
				if (this.base.document.forms.length > 0) {
					var firstForm =
						this.base.document.forms [0];
					this.base.selection.collapse (firstForm,
										0);
				}
				else {
					var rootElement =
						this.mNsIHtmlEditor.rootElement;
					this.base.selection.collapse (rootElement,
										0);
				}

				// All of our event listeners are added to the
				// document here
				this.base.document.addEventListener ('mousedown',
						selectFromClick,
						true);
				this.base.document.addEventListener ('click',
						detectSingleClick,
						true);
				this.base.document.addEventListener ('dblclick',
						detectDoubleClick,
						true);
				this.base.document.addEventListener ('contextmenu',
						handleContextMenu,
						true);
				this.base.document.addEventListener ('draggesture',
						handleDragStart,
						true);
				this.base.document.addEventListener ('dragdrop',
						handleDrop,
						true);
				this.base.document.addEventListener ('keypress',
						handleKeyPress,
						true);

				// Load editing stylesheet
				var baseEditor = this.base;
				baseEditor.QueryInterface(STYLE_SHEETS);
				baseEditor.addOverrideStyleSheet(EDITOR_CONTENT_STYLE);
			} catch (e) {host.throwException ('loadPage()', e);}
		}
	},

	getPage: function()
	{
		var htmlOut = this.serializePage ();
		htmlOut = this.transformBeforeOutput(htmlOut, true);
		dump (htmlOut);
		return htmlOut;
	},

	addControl: function(aControlHtml, aControlId)
	{
		if(aControlHtml && aControlId) {
			dump ('Will add control:' + aControlId);
			this.hideResizers ();
			var insertionPoint =
				{insertIn: null, destinationOffset: 0};
			this.findInsertionPoint (insertionPoint);
			var controlHTML =
				this.transformBeforeInput (aControlHtml, false);

			this.base.insertHTMLWithContext (controlHTML, '',
					'', 'text/html', null,
					insertionPoint.insertIn,
					insertionPoint.destinationOffset, false);

			var newControl =
				this.base.document.getElementById (aControlId);
			this.setSelectNone (newControl);
			this.selectControl (aControlId);
			dump ('Did add control:' + controlHTML);
		}
	},

	removeControl: function(aControlId)
	{
		var control =
			this.base.document.getElementById (aControlId);
		if(control) {
			dump ('Will remove control:' + aControlId);
			this.base.selectElement (control);
			this.base.deleteSelection (0);
			dump ('Did remove control:' + aControlId);
		}
	},

	updateControl: function(aControlId, aNewDesignTimeHtml)
	{
		if(aControlId && aNewDesignTimeHtml &&
		   this.base.document.getElementById (aControlId)) {
			this.inUpdate = true;
			dump ('Will update control:' + aControlId);
			this.hideResizers ();
			var newDesignTimeHtml =
				this.transformBeforeInput (aNewDesignTimeHtml, false);
			try {
				var oldControl =
					this.base.document.getElementById (aControlId);
				this.collapseBeforeInsertion ("start");
				this.base.selectElement (oldControl);
				this.base.insertHTML (newDesignTimeHtml);
				dump ('Updated control ' + aControlId +
					'; newDesignTimeHtml is ' +
					newDesignTimeHtml);
				} catch (e) { }
			this.inUpdate = false;
			this.endBatch ();
			this.updateControlInTable(aControlId,
			this.base.document.getElementById (aControlId));
			dump ('Did update control:' + aControlId);
		}
	},

	renameControl: function(aOldControlId, aNewControlId)
	{
		var control =
			this.base.document.getElementById (aOldControlId);
		if (!aOldControlId || !aNewControlId) {
			host.throwException ('renameControl () ',
					'Too few or no arguments');
			return;
		}
		else if (!control) {
			host.throwException ('renameControl () ',
					'Invalid control name');
			return;
		}
		else
			control.setAttribute (ID, aNewControlId);
	},

	// Control selection
	selectControl: function(aControlId, aAdd, aPrimary)
	{
		// TODO: talk to Michael about selecting controls. Why do we
		// need to have multiple controls selected and what is primary?
		if (aControlId == '') {
			if(this.base.resizedObject && this.nodeIsControl (this.base.resizedObject)){
				this.hideResizers ();
			}
			dump ("Deselecting all controls");
			return;
		}

		dump ("Selecting control " + aControlId);
		this.clearSelection ();
		var controlRef =
			this.base.document.getElementById (aControlId);
		this.base.selectElement (controlRef);
		this.showResizers (controlRef);
	},

	// Misc
	// TODO: Handle commands on controls independently
	doCommand: function (aCommand)
	{
		if(aCommand != CUT)
			this.inCommandExec = true;
		if (this.mNsICommandManager.isCommandSupported (aCommand, this.mEditorWindow)) {
			switch (aCommand) {
			case 'cmd_bold':
				this.mNsICommandManager.doCommand (aCommand,
							null,
							this.mEditorWindow);
				break;
			case 'cmd_italics':
				this.mNsICommandManager.doCommand (aCommand,
							null,
							this.mEditorWindow);
				break;
			case 'cmd_underline':
				this.mNsICommandManager.doCommand (aCommand,
							null,
							this.mEditorWindow);
				break;
			case 'cmd_indent':
				this.mNsICommandManager.doCommand (aCommand,
							null,
							this.mEditorWindow);
				break;
			case 'cmd_outdent':
				this.mNsICommandManager.doCommand (aCommand,
							null,
							this.mEditorWindow);
				break;
			case 'cmd_cut':
				this.mNsICommandManager.doCommand (aCommand,
							null,
							this.mEditorWindow);
				this.mLastClipboardUpdateCommand = CUT;
				break;
			case 'cmd_copy':
				this.mNsICommandManager.doCommand (aCommand,
							null,
							this.mEditorWindow);
				this.mLastClipboardUpdateCommand = COPY;
				break;
			case 'cmd_paste':
				var focusNode = this.base.selection.focusNode;
				var control =
					this.base.getElementOrParentByTagName (CONTROL_TAG_NAME,
							focusNode);
				if(control) {
					var controlId = control.getAttribute (ID);
					this.selectControl (controlId);
				}
				this.collapseBeforeInsertion ("end");

				// We shouldn't paste directlly since there
				// might be ASP controls in the clipboard.
				// Get the design-time HTML with proper, new
				// control ID's from the host, then paste it
				// and restore the original clipboard content.
				var content = clip.getClipboard ();
				var newContent = host.deserializeAndAdd (content);
				//clip.setClipboard (newContent);
				//this.mNsICommandManager.doCommand (PASTE,
							//null,
							//this.mEditorWindow);
				//clip.setClipboard (content);

				break;
			default:
				this.mNsICommandManager.doCommand (aCommand,
							null,
							this.mEditorWindow);
				break;
			}
		}
		else
			host.throwException ('doCommand (' + aCommand + ')',
					'Command not supported');
		dump ("Executed command: " + aCommand);
		this.inCommandExec = false;
	},

	isCommandEnabled: function (aCommand)
	{
		var commandManager = this.mNsICommandManager;
		if (commandManager.isCommandSupported (aCommand, this.mEditorWindow))
			if (commandManager.isCommandEnabled (aCommand, this.mEditorWindow))
				return true;
			else
				return false;
		else
			host.throwException ('doCommand (' + aCommand + ')',
					'Command not supported');
			
	},

	insertFragment: function (aHtml)
	{
		if(aHtml) {
			this.hideResizers ();
			var insertionPoint =
				{insertIn: null, destinationOffset: 0};
			this.findInsertionPoint (insertionPoint);
			var HTML = this.transformBeforeInput (aHtml, false);

			this.base.insertHTMLWithContext (HTML, '', '',
				'text/html', null, insertionPoint.insertIn,
				insertionPoint.destinationOffset, false);
		}
	},

	getChildControls: function (aNode, aRetArray)
	{
		var i = 0;
		while(aNode.childNodes [i]) {
			if(this.nodeIsControl (aNode.childNodes [i]))
				aRetArray.arr.push (aNode.childNodes [i]);
			this.getChildControls (aNode.childNodes [i], aRetArray);
			i++;
		}
	},

	isControlChildOf: function (aNode, aControl)
	{
		var i = 0;
		var ret = false;
		while(aNode.childNodes [i]) {
			if(aNode.childNodes [i] == aControl) {
				ret = true;
				break;
			}
			else {
				ret = this.isControlChildOf (aNode.childNodes [i],
						aControl);
				if(ret)
					break;
			}
			i++;
		}
		return ret;
	},

	findInsertionPoint: function(aInsertionPoint)
	{
		aInsertionPoint.insertIn = null;
		aInsertionPoint.destinationOffset = 0;
		var selectedElement = this.base.getSelectedElement ('');
		var focusNode = this.base.selection.focusNode;
		var parentControl =
			this.base.getElementOrParentByTagName (CONTROL_TAG_NAME,
						focusNode);

		// If we have a single-element selection and the element
		// happens to be a control
		if (selectedElement &&
		    this.nodeIsControl (selectedElement)) {
			aInsertionPoint.insertIn = selectedElement.parentNode;
			while(selectedElement != aInsertionPoint.insertIn.childNodes [aInsertionPoint.destinationOffset])
				aInsertionPoint.destinationOffset++;
			aInsertionPoint.destinationOffset++;
		}

		else if(focusNode) {
			// If selection is somewhere inside a control
			if(parentControl){
				aInsertionPoint.insertIn = parentControl.parentNode;
				while(parentControl != aInsertionPoint.insertIn.childNodes [aInsertionPoint.destinationOffset])
					aInsertionPoint.destinationOffset++;
				aInsertionPoint.destinationOffset++;
			}
		}
	},

	clearSelection: function()
	{
		this.collapseBeforeInsertion ("end");
	},

	getSelectAll: function(aElement)
	{
		var style = aElement.style;
		if(style.getPropertyValue ('MozUserSelect') == 'all' ||
		   style.getPropertyValue ('-moz-user-select') == 'all')
			return true;
		return false;
	},

	getSelectNone: function(aElement)
	{
		var style = aElement.style;
		if(style.getPropertyValue ('MozUserSelect') == 'none' ||
		   style.getPropertyValue ('-moz-user-select') == 'none')
			return true;
		return false;
	},

	setSelectAll: function(aElement)
	{
		aElement.style.setProperty ('MozUserSelect', 'all', '');
		aElement.style.setProperty ('-moz-user-select', 'all', '');
	},

	setSelectNone: function(aElement)
	{
		aElement.style.setProperty ('MozUserSelect', 'none', '');
		aElement.style.setProperty ('-moz-user-select', 'none', '');
	},

	serializePage: function()
	{
		var xml =
			this.base.outputToString (this.base.contentsMIMEType,
						256);
		return xml;
	},

	hideTableUI: function()
	{
		this.mNsIHTMLInlineTableEditor.hideInlineTableEditingUI ();
	},

	showResizers: function(aElement)
	{
		this.mNsIHtmlEditor.hideResizers ();
		if(this.nodeIsControl (aElement) &&
		   aElement.getAttribute ('-md-can-resize') == 'true') {
			this.mNsIHtmlEditor.showResizers (aElement);
		}
	},

	hideResizers: function()
	{
		this.mNsIHtmlEditor.hideResizers ();
	},

	getSelectedControl: function()
	{
		if(this.mNsIHtmlEditor) {
			var selectedElement =
				this.base.getSelectedElement ('');
			if(selectedElement && this.nodeIsControl (selectedElement))
				return selectedElement;
			else
				return null;
		}
	},

	getElementOrParentByAttribute: function(aNode, aAttribute, aValue) {
		// Change the entire function to the (probably) more efficient
		// XULElement.getElementsByAttribute ( attrib , value )
		// It will return all the children with the specified
		// attribute
		if(aNode.nodeType == 1)
			var attrbiute = aNode.getAttribute (aAttribute);
		if(attrbiute == aValue)
			return aNode;

		aNode = aNode.parentNode;
		while(aNode) {
			if(aNode.nodeType == 1)
				attrbiute = aNode.getAttribute (aAttribute);
			if(attrbiute == aValue)
				return aNode;
			aNode = aNode.parent;
		}
		return null;
	},

	clone: function (aObj)
	{
		var clone = {};
		for (var i in aObj) {
			if(typeof aObj [i] == OBJECT)
				clone [i] = this.clone (aObj [i]);
			else
				clone [i] = aObj [i];
		}
		return clone;
	},

	cancelTableOverrideStyle: function (aTables)
	{
		var table;
		while((table = aTables.nextNode ()) != null) {
			table.setAttribute ('cancelUI', 'true');
		}
	}
};

//* ___________________________________________________________________________
// Event handlers
//_____________________________________________________________________________
function selectFromClick(aEvent)
{
	control = editor.base.getElementOrParentByTagName (CONTROL_TAG_NAME,
					aEvent.target);

	if(control) {
		if(editor.getSelectAll (control)) {
			editor.dragState = false;
			editor.setSelectNone (control);
		}

		if(editor.base.resizedObject) {
			editor.hideResizers ();
			editor.hideTableUI ();
		}
		var controlId = control.getAttribute (ID);
		editor.selectControl (controlId);
	}
}

function handleDragStart(aEvent) {
	// If we are resizing, do nothing - false call
	if(editor.inResize)
		return;

	// Controls are "-moz-user-select: none" by default. Here we switch to
	// "-moz-user-select: all" in preparation for insertFromDrop(). We
	// revert back to -moz-user-select: none later in DidInsertNode(),
	// which is the real end of a Drag&Drop operation
	editor.hideResizers ();
	var selectedControl = editor.getSelectedControl ();
	var controls =
		editor.base.document.getElementsByTagName (CONTROL_TAG_NAME);
	if(controls.length > 0) {
		var i = 0;
		while(controls [i]) {
			if(selectedControl != controls [i])
				editor.setSelectAll (controls [i]);
			i++;
		}
	}

	editor.dragState = true;
	dump ('Begin drag.');
}

function handleDrop(aEvent) {
	try {
		//var tmp = aEvent.
		//var evt = document.createEvent('UIEvents');
		//evt.initMouseEvent("dragdrop", true, true, editor.editorWindow,
			//1, 10, 50, 10, 50, false, false, false, false, 0, editor.base.rootElement);
		aEvent.stopPropagation ();
		aEvent.preventDefault ();
		editor.base.insertFromDrop (aEvent);
		editor.base.setShouldTxnSetSelection (false);
	} catch (e) {alert (e)}
}

function handleSingleClick(aButton, aTarget) {
	if(!editor.cancelClick) {
		control =
			editor.base.getElementOrParentByTagName (CONTROL_TAG_NAME,
						aTarget);
		var controlId =
			(control) ? control.getAttribute (ID) : '';

		switch (aButton) {
		case 0:
			host.click (SINGLE_CLICK, controlId);
			break;
		case 2:
			host.click (RIGHT_CLICK, controlId);
			break;
		}
	}
}

function detectSingleClick(aEvent)
{
	editor.cancelClick = false;
	var button = aEvent.button;
	var target = aEvent.target;
	setTimeout (function() { handleSingleClick(button, target); }, 100);
}

function detectDoubleClick(aEvent)
{
	editor.cancelClick = true;
	control = editor.base.getElementOrParentByTagName (CONTROL_TAG_NAME,
					aEvent.target);
	var controlId =
		(control) ? control.getAttribute (ID) : '';


	host.click (DOUBLE_CLICK, controlId);
	//alert (editor.getPage ());
}

function handleContextMenu(aEvent)
{
	aEvent.stopPropagation ();
	aEvent.preventDefault ();
} 

// We have to detect cut, copy and paste, for they may involve controls
// If we copy a control and then past it, the host must be notified so
// it can create a new instance and assign the pasted control a new id
// TODO: Make those key really work
function handleKeyPress(aEvent) {
	// Handle cut
	if(aEvent.ctrlKey && aEvent.charCode == 120) {
		editor.doCommand (CUT);
		aEvent.stopPropagation ();
		aEvent.preventDefault ();
	}
	// Handle copy
	else if(aEvent.ctrlKey && aEvent.charCode == 99) {
		editor.doCommand (COPY);
		aEvent.stopPropagation ();
		aEvent.preventDefault ();
	}
	// Handle paste
	else if(aEvent.ctrlKey && aEvent.charCode == 118) {
		editor.doCommand (PASTE);
		aEvent.stopPropagation ();
		aEvent.preventDefault ();
	}
	// Handle delete
	else if(aEvent.keyCode == aEvent.DOM_VK_DELETE) {
		var control = editor.getSelectedControl ();
		var resizedObject = editor.base.resizedObject;

		// Special case: if we have resizers shown, but no single control
		// is selected we should reselect the control with resizers so it
		// gets deleted entirely. We get here when selecting ajasent
		// controls with the arrow keys
		//if(resizedObject && !control && editor.nextSiblingIsControl ()) {
			//editor.selectControl(editor.nextSiblingIsControl ());
			//editor.hideResizers ();
			//editor.setSelectAll (editor.getResizedObject ());
		//}

		// If we have a single element selected and it happens to be a
		// control
		if(control) {
			editor.hideResizers ();
			editor.setSelectAll (control);
		}

		// If selection is collapsed, caret is shown, and it's at the
		// of a text node
		else if (editor.atEndOfTextNode ()) {
			// If next sibling is a control, we should select it so
			// it gets entirely deleted
			if(editor.nextSiblingIsControl ()) {
				var focusNode =
					editor.base.selection.focusNode;
				control = focusNode.nextSibling;
				var controlId = control.getAttribute (ID);
				editor.selectControl (controlId);
				editor.hideResizers ();
			}
		}
	}
	// Backspace
	else if (aEvent.keyCode == aEvent.DOM_VK_BACK_SPACE) {
		var control = editor.getSelectedControl ();
		var resizedObject = editor.base.resizedObject;

		// Special case: if we have resizers shown, but no single control
		// is selected we should reselect the control with resizers so it
		// gets deleted entirely. We get here when selecting ajasent
		// controls with the arrow keys
		if(resizedObject && !control) {
			editor.selectControl(resizedObject.getAttribute(ID));
			editor.hideResizers ();
			editor.setSelectAll (resizedObject);
		}

		// If we have a single element selected and it happens to be a
		// control
		else if(control) {
			editor.hideResizers ();
			editor.setSelectAll (control);
		}

		// If selection is collapsed, caret is shown, and it's at the
		// beginning of a text node
		else if (editor.atBeginningOfTextNode ()) {
			// If previous sibling is a control, we should select
			// it so it gets entirely deleted
			if(editor.previousSiblingIsControl ()) {
				var focusNode =
					editor.base.selection.focusNode;
				var control = focusNode.previousSibling;
				var controlId = control.getAttribute (ID);
				editor.selectControl (controlId);
				editor.hideResizers ();
			}
		}
	}
	// Arrow up
	else if(aEvent.keyCode == aEvent.DOM_VK_UP) {
		
	}
	// Arrow down
	else if(aEvent.keyCode == aEvent.DOM_VK_DOWN) {
		
	}
	// Arrow left
	else if(aEvent.keyCode == aEvent.DOM_VK_LEFT) {
		var control = editor.previousSiblingIsControl ();
		var controlId = '';
		// If previous sibling is control and we don't have a single
		// control selected (in which case we only need to collapse and
		// show caret)
		if(control && !editor.getSelectedControl ()) {
			controlId = control.getAttribute (ID);
			editor.selectControl (controlId);
			// Hack. We should change the way selection works
			host.click (SINGLE_CLICK, controlId);
			aEvent.stopPropagation ();
			aEvent.preventDefault ();
		}
	}
	// Arrow right
	else if(aEvent.keyCode == aEvent.DOM_VK_RIGHT) {
		var control = editor.nextSiblingIsControl ();
		var controlId = '';
		// If next sibling is control and we don't have a single control
		// selected (in which case we only need to collapse and show
		// caret)
		if(control && !editor.getSelectedControl ()) {
			controlId = control.getAttribute (ID);
			editor.selectControl (controlId);
			// Hack. We should change the way selection works
			host.click (SINGLE_CLICK, controlId);
			aEvent.stopPropagation ();
			aEvent.preventDefault ();
		}
	}
}

function handleClipboardUpdate() {
	var content = clip.getClipboard ();
	content = editor.transformBeforeOutput (content, false);

	alert (clip.getClipboard ());
	var newContent = host.serialize (content);
	clip.setClipboard (newContent);
	alert (clip.getClipboard ());
}

// Define a NodeFilter function to accept only <table> elements
function tableFilter(aNode) {
	if (aNode.tagName.toLowerCase () == TABLE)
		return NodeFilter.FILTER_ACCEPT;
	else
		return NodeFilter.FILTER_SKIP;
}

function dump(aTxtAppend) {
	if(DEBUG) {
		JSCallPlaceClrCall ('DebugStatement', '', new Array(aTxtAppend));
	}
}
