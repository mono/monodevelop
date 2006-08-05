 /*
 * constants.js - Global application constants
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


const DEBUG                            = true;
const ID                               = 'id';
const WIDTH                            = 'width';
const HEIGHT                           = 'height';
const MIN_WIDTH                        = 'min-width';
const MIN_HEIGHT                       = 'min-height';
const DISPLAY                          = 'display';
const BORDER                           = 'border';
const VERTICAL_ALIGN                   = 'vertical-align';
const POSITION                         = 'position';
const Z_INDEX                          = 'z-index';
const BORDER_CAN_DROP_COLOR            = '#ee0000';
const BORDER_CAN_DROP_THICK            = '2';
const BORDER_CAN_DROP_INVERT           = false;
const DIRECTIVE_PLACE_HOLDER_EXP       = /(<directiveplaceholder.[^(><.)]+\/>)/g;
const SCRIPT_PLACE_HOLDER_EXP          = /(<scriptblockplaceholder.[^(><.)]+\/>)/g;
const STRIP_SCRIPT_PLACE_HOLDER_EXP    = /<!(?:--(<scriptblockplaceholder[\s\S]*?)--\s*)?>\s*/g;
const CONTROL_TAG_NAME                 = 'aspcontrol';
const TABLE                            = 'table';
const EMPTY_CONTROL_EXP                = /(<aspcontrol.[^(><.)]+><\/aspcontrol>)/g;
const CONTROL_ID_EXP                   = /(<aspcontrol[\s\S]*?id=")([\D]*?)([\d]*?)(")/g;
const BEGIN_CONTROL_TAG_EXP            = /(<aspcontrol.[^(><.)]+>)/g;
const END_CONTROL_TAG_EXP              = /<\/aspcontrol>/g;
const STRIP_CONTROL_EXP                = /(<span class="ballast".*?><span.*?><div>.*?[\s\S]*?.*?<\/div><\/span><\/span>)/g;
const APPEND_TO_CONTROL_END            = '</span></span>';
const APPEND_TO_CONTROL_BEGIN          = "<span class=\"ballast\" style=\"display: block; position: relative\"><span style=\"position: absolute; display: block; z-index: -1;\">";
const EMPTY_CONTROL_MSG                = '<span style=\"color: #bb0000;\">This control has no HTML<br/>representation associated.</span>';
const SINGLE_CLICK                     = 'single';
const DOUBLE_CLICK                     = 'double';
const RIGHT_CLICK                      = 'right';
const OBJECT_RESIZER                   = Components.interfaces.nsIHTMLObjectResizer;
const INLINE_TABLE_EDITOR              = Components.interfaces.nsIHTMLInlineTableEditor;
const TABLE_EDITOR                     = Components.interfaces.nsITableEditor;
const EDITOR                           = Components.interfaces.nsIEditor;
const SELECTION_PRIVATE                = Components.interfaces.nsISelectionPrivate;
const STYLE_SHEETS                     = Components.interfaces.nsIEditorStyleSheets;
const EDITOR_CONTENT_STYLE             = 'chrome://aspdesigner/content/editorContent.css';
const OBJECT                           = 'object';
const CUT                              = 'cmd_cut';
const COPY                             = 'cmd_copy';
const PASTE                            = 'cmd_paste';
const UNDO                             = 'cmd_undo';
const REDO                             = 'cmd_redo';
const TEXT_HTML                        = 'text/html';
const TEXT_UNICODE                     = 'text/unicode';