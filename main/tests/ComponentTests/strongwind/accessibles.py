# -*- coding: utf-8 -*-
#
# Strongwind
# Copyright (C) 2007 Medsphere Systems Corporation
# 
# This program is free software; you can redistribute it and/or modify it under
# the terms of the GNU General Public License version 2 as published by the
# Free Software Foundation.
# 
# This program is distributed in the hope that it will be useful, but WITHOUT
# ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
# FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more
# details.
# 
# You should have received a copy of the GNU General Public License along with
# this program; if not, write to the Free Software Foundation, Inc.,
# 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
# 

'Define the base Accessible and widget-specific classes'

import re
from time import sleep

import pyatspi

import config
import errors
import procedurelogger
import utils
import cache

class Accessible(object):
    '''
    A generic Accessible

    Subclasses of this class in this module provide additional functionality
    specific to certain ATK roles. (e.g., the MenuBar provides a select()
    method that this generic Accessible does not provide)
    '''

    def __init__(self, accessible):
        '''
        Constructor for strongwind.accessibles.Accessible

        Takes a single argument, accessible.  accessible can be either a
        pyatspi.Accessibility.Accessible or another strongwind.accessibles.Accessible.

        The constructor's main job is to set self._accessible, to the
        pyatspi.Accessibility.Accessible that this object is wrapping.
        '''

        if isinstance(accessible, pyatspi.Accessibility.Accessible):
            self._accessible = accessible
        elif isinstance(accessible, Accessible):
            self._accessible = accessible._accessible
        else:
            raise TypeError

        # make sure our _accessible is valid
        assert isinstance(self._accessible, pyatspi.Accessibility.Accessible)

    def _promote(self, accessible):
        '''
        Promote a pyatspi.Accessibility.Accessible to a specific strongwind class 
        if possible (e.g., a strongwind.accessibles.Dialog), or to a generic
        strongwind.accessibles.Accessible otherwise.
        '''

        # look in the application/widget cache first
        if accessible.getRole() == pyatspi.ROLE_APPLICATION:
            try:
                return cache.getApplicationById(accessible.queryApplication().id)
            except KeyError:
                pass # not in cache
        else:
            try:
                return cache.getWidget(accessible)
            except KeyError:
                pass # not in cache

        # try looking for a specific strongwind class, e.g. strongwind.accessibles.MenuBar
        try:
            className = utils.toClassName(accessible.getRoleName()) # convert the accessible's role name to a class name, e.g. "menu bar" => "MenuBar"
            module = __import__(__name__)   # import the current module (strongwind.accessibles) so we can inspect its vars
            klass = vars(module)[className] # turn the className (a string) into the class of that name (e.g., a MenuBar class)
            return klass(accessible)        # return an instance of the class (e.g., a MenuBar object)
        except KeyError:
            return Accessible(accessible)

    # attribute wrappers
    def __getattr__(self, attr):
        if attr == 'childCount':
            return self._accessible.childCount
        elif attr == 'name':
            return self._accessible.name
        elif attr == 'logName':
            if not self.__dict__.has_key('logName'):
                self.__dict__['logName'] = self._accessible.name
            return self.__dict__['logName']
        elif attr == 'description':
            return self._accessible.description
        elif attr == 'parent':
            return self._promote(self._accessible.parent)
        elif attr == 'role':
            return self._accessible.getRole()
        elif attr == 'roleName':
            return self._accessible.getRoleName()
        elif attr == 'extents':
            return self._accessible.queryComponent().getExtents(pyatspi.DESKTOP_COORDS)
        elif attr == 'layer':
            return self._accessible.queryComponent().getLayer()
        elif attr == 'text':
            itext = self._accessible.queryText()
            return itext.getText(0, itext.characterCount)
        elif attr == 'value':
            return self._accessible.queryValue().currentValue
        elif attr == 'caretOffset':
            return self._accessible.queryText().caretOffset
        elif attr == 'characterCount':
            return self._accessible.queryText().characterCount
        elif attr == 'app':
            try:
                return cache.getApplicationById(self._accessible.getApplication().id)
            except Exception:
                try:
                    return self._promote(self._accessible.getApplication())
                except Exception:
                    return None
        else:
            # bind states, e.g., showing, focusable, sensitive, etc.
            a = 'STATE_' + utils.toConstantName(attr)
            v = vars(pyatspi)
            if v.has_key(a):
                try:
                    return self._accessible.getState().contains(v[a])
                except AttributeError:
                    # when the app goes down, we sometimes get "AttributeError: 'CORBA.Object' object has no attribute 'contains'"
                    return False

            # FIXME: bind relations
            # FIXME: bind attributes

            # add find methods
            if attr.startswith('findAll'):
                a = None

                translations = {
                    'findAllCanvases': 'ROLE_CANVAS',
                    'findAllCheckBoxes': 'ROLE_CHECK_BOX',
                    'findAllComboBoxes': 'ROLE_COMBO_BOX',
                    'findAllEmbedded': 'ROLE_EMBEDDED',
                    'findAllEntries': 'ROLE_ENTRY',
                    'findAllFillers': 'ROLE_FILLER',
                    'findAllInvalid': 'ROLE_INVALID',
                    'findAllLastDefined': 'ROLE_LAST_DEFINED'}

                try:
                    a = translations[attr]
                except KeyError:
                    a = 'ROLE' + utils.toConstantName(attr[7:-1])

                v = vars(pyatspi)
                if v.has_key(a):
                    # generate a function on-the-fly and return it
                    def findMethod(name, checkShowing=True, recursive=True):
                        dontCheckShowing = not checkShowing
                        return utils.findAllDescendants(self, lambda x: x.role == v[a] and utils.equalsOrMatches(x.name, name) and (dontCheckShowing or x.showing), recursive)

                    return findMethod

            if attr.startswith('find'):
                a = 'ROLE' + utils.toConstantName(attr[4:])
                v = vars(pyatspi)
                if v.has_key(a):
                    # generate a function on-the-fly and return it
                    def findMethod(name, logName=None, checkShowing=True, retry=True, recursive=True, breadthFirst=True, raiseException=True, setReference=False):
                        dontCheckShowing = not checkShowing
                        y = utils.findDescendant(self, lambda x: x.role == v[a] and utils.equalsOrMatches(x.name, name) and (dontCheckShowing or x.showing), \
                            retry=retry, recursive=recursive, breadthFirst=breadthFirst, raiseException=raiseException)

                        # don't try promoting y if it's None
                        if not raiseException and y is None:
                            return y

                        # look in the widget cache first
                        try:
                            return cache.getWidget(y)
                        except KeyError:
                            pass # not in cache

                        # if the logName isn't given, set it to the name (unless the name is a regex)
                        if logName is None and type(name) is not type(re.compile('r')):
                            logName = name

                        # if we still don't have a logname, just grab the name of the accessible
                        if logName is None:
                            logName = y.name

                        # at this point, we have a logName.  next, we look for a class named 
                        # logName + roleName in moduleName.  if such a class can be found, we promote 
                        # the widget to that class, cache the instance, and return it.  
                        #
                        # if no such class can be found, and the logName is different than the name 
                        # of the widget, set the widget's log name and cache the widget.  if the logName 
                        # is the same as the widget's logName, there's no need to cache the widget.  

                        # if the logName is 'Preferences' and roleName of the widget is 'Dialog', 
                        # the name of the class to look for is 'PreferencesDialog'
                        className = utils.toClassName(logName) + utils.toClassName(y.roleName)

                        # the module prefix is the module of this class.  so if we had a widget that had 
                        # a class medsphere.openvistacis.OpenVistaCIS, and we call findDialog('Preferences') 
                        # on it, the module prefix would be medsphere.openvistacis.  we append the name of 
                        # the class we're looking for to the module prefix to get the name of the module.
                        # so continuing with the example, the full module name would be 
                        # medsphere.openvistacis.preferencesdialog.  (In the medsphere.openvistacis.preferencesdialog 
                        # module, we would look for 'PreferencesDialog' - that code is a few lines down)
                        moduleName = self.__class__.__module__ + '.' + className.lower()

                        try:
                            # import the module, grab the class, and make an instance of it, then cache the instance
                            module = __import__(moduleName, globals(), None, [className])
                            klass = vars(module)[className]
                            z = klass(y)
                            cache.addWidget(z)
                        except (ImportError, KeyError):
                            # if the found widget's logName isn't the same as the logName 
                            # we were given, set the widget's logName and cache the widget
                            if y.name != logName:
                                y.logName = logName
                                cache.addWidget(y) # make the log name stick by caching the object with the logName property set on it
                            z = y

                        # set self.preferencesDialog = the widget we just found/promoted/cached
                        if setReference:
                            self.__setattr__(utils.toVarName(logName) + utils.toClassName(z.roleName), z)

                        return z

                    return findMethod

            # add assert methods
            if attr.startswith('assertNo'):
                a = 'ROLE' + utils.toConstantName(attr[8:])
                v = vars(pyatspi)
                if v.has_key(a):
                    # generate a function on-the-fly and return it
                    def assertMethod(name, checkShowing=True, retry=False, recursive=True, breadthFirst=False, raiseException=False):
                        def descendantNotFound():
                            dontCheckShowing = not checkShowing
                            return utils.findDescendant(self, lambda x: x.role == v[a] and utils.equalsOrMatches(x.name, name) and (dontCheckShowing or x.showing), \
                                retry=retry, recursive=recursive, breadthFirst=breadthFirst, raiseException=raiseException) == None

                        assert utils.retryUntilTrue(descendantNotFound)

                    return assertMethod

            if attr.startswith('assert'):
                a = 'ROLE' + utils.toConstantName(attr[6:])
                v = vars(pyatspi)
                if v.has_key(a):
                    # generate a function on-the-fly and return it
                    def assertMethod(name, checkShowing=True, retry=False, recursive=True, breadthFirst=False, raiseException=False):
                        def descendantFound():
                            dontCheckShowing = not checkShowing
                            return utils.findDescendant(self, lambda x: x.role == v[a] and utils.equalsOrMatches(x.name, name) and (dontCheckShowing or x.showing), \
                                retry=retry, recursive=recursive, breadthFirst=breadthFirst, raiseException=raiseException) != None

                        assert utils.retryUntilTrue(descendantFound)

                    return assertMethod

            if self.__dict__.has_key(attr):
                return self.__dict__[attr]

            raise AttributeError, "%s instance has no attribute '%s'" % (self.__class__.__name__, attr)

    def __setattr__(self, attr, value):
        if attr == 'name':
            self._accessible.name = value
        elif attr == 'description':
            self._accessible.description = value
        elif attr == 'text':
            self._accessible.queryEditableText().setTextContents(value)
        elif attr == 'value':
            self._accessible.queryValue().currentValue = value
        else:
            self.__dict__[attr] = value

    # thin method wrappers
    def __iter__(self):
        for i in self._accessible:
            if i is None:
                continue
            yield self._promote(i)

    def __str__(self):
        'Creates a logName representation of this Accessible (ie. Zoom In button)'

        translations = {
            pyatspi.ROLE_FRAME: 'window',
            pyatspi.ROLE_MENU_ITEM: 'menu option',
            pyatspi.ROLE_PAGE_TAB: 'tab',
            pyatspi.ROLE_PANEL: 'section',
            pyatspi.ROLE_PUSH_BUTTON: 'button',
            pyatspi.ROLE_TABLE: 'list',
            pyatspi.ROLE_TABLE_CELL: '',
            pyatspi.ROLE_TEXT: 'text field',
            pyatspi.ROLE_TOOL_BAR: 'toolbar',
            pyatspi.ROLE_WINDOW: 'context menu'}

        try:
            name = self.logName
        except AttributeError:
            name = self.name

        # get rid of the ... at the end of the names (eg. Edit...)
        if re.search('\.\.\.$', name):
            name = name[:-3]

        # remove/change newlines in names to a space
        name = name.replace('/\n','/').replace('\n',' ')

        try:
            roleName = translations[self.role]
        except KeyError:
            roleName = self.roleName

        if re.search(roleName + '$', name, flags=re.IGNORECASE):
            return name

        if name is '' or name is None:
            return roleName

        return '%s %s' % (name, roleName)

    def __nonzero__(self):
        return self._accessible.__nonzero__()

    def __getitem__(self, index):
        return self._promote(self._accessible.__getitem__(index))

    def __len__(self):
        return self._accessible.__len__()

    def getIndexInParent(self):
        return self._accessible.getIndexInParent()

    def getApplication(self):
        return self._promote(self._accessible.getApplication())

    def getChildAtIndex(self, index):
        return self._promote(self._accessible.getChildAtIndex(index))

    # methods to synthesize raw user input

    # adapted from script_playback.py
    def _charToKeySym(self, key):
        import gtk.gdk

        try:
            rv = gtk.gdk.unicode_to_keyval(ord(key))
        except:
            rv = getattr(gtk.keysyms, key)
        return rv

    # adapted from script_playback.py, originally named type
    def typeText(self, text):
        'Turns text (a string) into a series of keyboard events'

        text_syms = map(self._charToKeySym, text)

        for key in text_syms:
            sleep(config.KEYCOMBO_DELAY)
            pyatspi.Registry.generateKeyboardEvent(key, None, pyatspi.KEY_SYM)

    # adapted from script_playback.py
    def keyCombo(self, combo):
        'Focus this Accessible and press a combination of keys simultaneously'

        import gtk.gdk

        _keymap = gtk.gdk.keymap_get_default()

        keySymAliases = {
            'enter' : 'Return',
            'esc' : 'Escape',
            'alt' : 'Alt_L',
            'control' : 'Control_L',
            'ctrl' : 'Control_L',
            'shift' : 'Shift_L',
            'del' : 'Delete',
            'ins' : 'Insert',
            'pageup' : 'Page_Up',
            'pagedown' : 'Page_Down',
            ' ' : 'space',
            '\t' : 'Tab',
            '\n' : 'Return'
        }

        ModifierKeyCodes = {
            'Control_L' : _keymap.get_entries_for_keyval(gtk.keysyms.Control_L)[0][0],
            'Alt_L' : _keymap.get_entries_for_keyval(gtk.keysyms.Alt_L)[0][0],
            'Shift_L' : _keymap.get_entries_for_keyval(gtk.keysyms.Shift_L)[0][0]
        }

        keys = []
        for key in re.split('[<>]', combo):
            if key:
                key = keySymAliases.get(key.lower(), key)
                keys.append(key)

        modifiers = map(ModifierKeyCodes.get, keys[:-1])

        try:
            self.grabFocus()
        except:
            pass

        sleep(config.SHORT_DELAY)

        for key_code in modifiers:
            sleep(config.KEYCOMBO_DELAY)
            pyatspi.Registry.generateKeyboardEvent(key_code, None, pyatspi.KEY_PRESS)

        sleep(config.KEYCOMBO_DELAY)
        pyatspi.Registry.generateKeyboardEvent(self._charToKeySym(keys[-1]), None, pyatspi.KEY_SYM)

        for key_code in modifiers:
            sleep(config.KEYCOMBO_DELAY)
            pyatspi.Registry.generateKeyboardEvent(key_code, None, pyatspi.KEY_RELEASE)

    def mouseClick(self, button=1, xOffset=0, yOffset=0):
        'Synthesize a mouse click on this Accessible'

        bbox = self.extents
        x = bbox.x + (bbox.width / 2) + xOffset
        #x = bbox.x + xOffset
        y = bbox.y + (bbox.height / 2) + yOffset
        #y = bbox.y + yOffset
        pyatspi.Registry.generateMouseEvent(x, y, 'b%dc' % button)

    # interface methods
    def _doAction(self, action):
        'Wrapper for doAction method in IAction interface'

        iaction = self._accessible.queryAction()

        for i in xrange(iaction.nActions):
            if iaction.getName(i) == action:
                def sensitive():
                    return self.sensitive

                if not utils.retryUntilTrue(sensitive):
                    raise errors.NotSensitiveError

                iaction.doAction(i)

    def click(self):
        "Convenience wrapper for _doAction('click')"

        self._doAction('click')

    def activate(self):
        "Convenience wrapper for _doAction('activate')"

        self._doAction('activate')

    def toggle(self):
        "Convenience wrapper for _doAction('toggle')"

        self._doAction('toggle')

    def grabFocus(self):
        'Wrapper for grabFocus method in IComponent interface'

        self._accessible.queryComponent().grabFocus()

    # MAYBE: any methods from ICollection?
    # no clue... doesn't show up in the class list

    # MAYBE: any methods from IDocument?
    # there are attributes...

    # MAYBE: any methods from IHyperlink?
    # MAYBE: any methods from IHypertext?
    # some methods exist to get URIs

    # MAYBE: any methods from IImage?
    # getImageExtents is the only interesting one

    # MAYBE: any methods from ILoginHelper?
    # not sure what this does

    def selectChild(self, childIndex):
        'Wrapper for selectChild in ISelection interface'

        self._accessible.querySelection().selectChild(childIndex)

    def selectAll(self):
        'Wrapper for selectAll in ISelection interface'

        self._accessible.querySelection().selectAll()

    def clearSelection(self):
        'Wrapper for selectAll in ISelection interface'

        self._accessible.querySelection().clearSelection()

    def getSelectedChild(self, selectedChildIndex):
        'Wrapper for getSelectedChild in ISelection interface'

        return self._promote(self._accessible.querySelection().getSelectedChild(selectedChildIndex))

    def getSelectedChildren(self):
        selectedChildren = []
        for i in xrange(self._accessible.querySelection().nSelectedChildren):
            selectedChildren.append(self._promote(self._accessible.querySelection().getSelectedChild(i)))

        return selectedChildren

    # MAYBE: any more methods from ISelection?  here are the other methods that ISelection
    # provides: deselectChild, deselectSelectedChild, isChildSelected
    #
    # for deselectChild and deselectSelectedChild, we really only use
    # ISelection for page tab lists, and they can't be deselected
    #
    # for isChildSelected we can just use the .selected
    # property.  So I think we're good for now...

    # MAYBE: any methods from IStreamableContent?
    # not sure what this does above and beyond the text interface

    # FIXME: any (more) methods from ITable?
    # inside Table class

    # FIXME: any (more) methods from IText?
    # yes, attribute query methods for color, etc.

    def getTextAttributes(self, charPos):
        sep = re.compile(':')
        itext = self._accessible.queryText()
        attrdict = {}
        for x in itext.getDefaultAttributeSet():
            (a,v) = sep.split(x)
            attrdict[a] = v
        for x in itext.getAttributeRun(charPos, False)[0]:
            (a,v) = sep.split(x)
            attrdict[a] = v
        return attrdict

    def assertTextAttribute(self, attr, value, charPos):
        'Assert attr:value string on text at position charPos (i.e. attr="fg-color", value="0,0,0")'

        itext = self._accessible.queryText()
        def checkAttribute():
            if attr in self.getTextAttributes(charPos):
                if self.getTextAttributes(charPos)[attr] == value:
                    return True
            return False
        assert utils.retryUntilTrue(checkAttribute)

    def removeTextSelection(self, index=0):
        'Remove selection from text at index'

        self._accessible.queryText().removeSelection(index)

    def getSelectedText(self, index=0):
        return self._accessible.queryText().getText(*itext.getSelection(index))

    def assertSelectedText(self, text, index=0):
        assert utils.retryUntilTrue(lambda: self.getSelectedText(index) == text)

class Desktop(Accessible):
    pass

class Application(Accessible):
    def __init__(self, accessible, subproc=None):
        super(Application, self).__init__(accessible)
        self.subproc = subproc # this is used later to determine if the application is closed

        # FIXME: assert that subproc is the right type?

    def __getattr__(self, attr):
        if attr == 'id': return self._accessible.id
        else: return super(Application, self).__getattr__(attr)

    def findFrame(self, name, logName=None, retry=True, raiseException=True, setReference=True, log=True):
        'Search for a window'

        func = self.__getattr__('findFrame')
        frame = func(name, logName=logName, retry=retry, recursive=False, raiseException=raiseException, setReference=setReference)

        if log: # we need to log after the find() because the frame might be promoted and have a different logName
            procedurelogger.expectedResult('The %s appears.' % frame)

        return frame

    def findDialog(self, name, logName=None, retry=True, raiseException=True, setReference=True, log=True):
        'Search for a dialog'

        func = self.__getattr__('findDialog')
        dialog = func(name, logName=logName, retry=retry, recursive=False, raiseException=raiseException, setReference=setReference)

        if log: # we need to log after the find() because the dialog might be promoted and have a different logName
            procedurelogger.expectedResult('The %s appears.' % dialog)

        return dialog

    def findAlert(self, text, logText=None, retry=True, raiseException=True):
        '''
        Search for an alert

        Alerts typically have no name, so we have to search for them by their content. 
        We look for alerts that are showing and have within them a label that contains 
        the given text.
        '''

        if logText is None:
            logText = text

        if raiseException:
            procedurelogger.expectedResult('An alert appears: %s' % logText)

        def alertHasLabelContainingText(alert):
            return alert.role == pyatspi.ROLE_ALERT and alert.showing and \
                alert.findLabel(text, retry=False, recursive=True, raiseException=False) is not None

        return utils.findDescendant(self, alertHasLabelContainingText, retry=retry, recursive=False, raiseException=raiseException)

    def assertClosed(self):
        'Raise an exception if the application is still open'

        procedurelogger.expectedResult('The application closes.')

        if self.subproc is not None:
            def closed():
                return self.subproc.poll() == 0

            assert utils.retryUntilTrue(closed)

class Frame(Accessible):
    # often, when a window is closed, the application closes.  the assertClosed
    # method needs the role and roleName properties to do its logging, so
    # ensure that those properties are available, even if the underlying
    # _accessible object goes away
    role = pyatspi.ROLE_FRAME
    roleName = 'Frame'

    def altF4(self, assertClosed=True):
        'Press <Alt>F4'

        procedurelogger.action('Press <Alt>F4.', self)
        self.keyCombo('<Alt>F4')

        if assertClosed: self.assertClosed()

    def assertClosed(self):
        'Raise an exception if the window is still open'

        procedurelogger.expectedResult('The %s disappears.' % self)

        def closed():
            try:
                return not self.showing
            except (LookupError, KeyError, pyatspi.ORBit.CORBA.COMM_FAILURE):
                return True

        assert utils.retryUntilTrue(closed)

class Dialog(Accessible):
    def ok(self, assertClosed=True):
        'Click the OK button'

        self.clickPushButton('OK', assertClosed=assertClosed)

    def cancel(self, assertClosed=True):
        'Click the Cancel button'

        self.clickPushButton('Cancel', assertClosed=assertClosed)

    def close(self, assertClosed=True):
        'Click the Close button'

        self.clickPushButton('Close', assertClosed=assertClosed)

    def clickPushButton(self, name, assertClosed=True):
        'Click a button and optionally assert that the dialog closes'

        self.findPushButton(name).click()

        if assertClosed: self.assertClosed()

    def altF4(self, assertClosed=True):
        'Press <Alt>F4'

        procedurelogger.action('Press <Alt>F4.', self)
        self.keyCombo('<Alt>F4')

        if assertClosed: self.assertClosed()

    def assertClosed(self):
        'Raise an exception if the dialog is still open'

        procedurelogger.expectedResult('The %s disappears.' % self)

        def closed():
            try:
                return not self.showing
            except (LookupError, KeyError, pyatspi.ORBit.CORBA.COMM_FAILURE):
                return True

        assert utils.retryUntilTrue(closed)

class Alert(Accessible):
    def __init__(self, accessible):
        super(Alert, self).__init__(accessible)
        self.message = self.findLabel(None, raiseException=False)

    def ok(self, assertClosed=True):
        'Click the OK button'

        self.clickPushButton('OK', assertClosed=assertClosed)

    def cancel(self, assertClosed=True):
        'Click the Cancel button'
        
        self.clickPushButton('Cancel', assertClosed=assertClosed)

    def yes(self, assertClosed=True):
        'Click the Yes button'

        self.clickPushButton('Yes', assertClosed=assertClosed)

    def no(self, assertClosed=True):
        'Click the No button'

        self.clickPushButton('No', assertClosed=assertClosed)

    def clickPushButton(self, name, assertClosed=True):
        'Click a button and optionally assert that the alert closes'

        self.findPushButton(name).click()

        if assertClosed: self.assertClosed()

    def assertClosed(self):
        'Raise an exception if the alert is still open'

        procedurelogger.expectedResult('The %s disappears.' % self)

        def closed():
            try:
                return not self.showing
            except (LookupError, KeyError, pyatspi.ORBit.CORBA.COMM_FAILURE):
                return True

        assert utils.retryUntilTrue(closed)

class PageTabList(Accessible):
    def getPageTabNames(self):
        'Returns the string name of all the page tabs'
    
        names = []
        for child in self:
            names.append(child.name)
        return names

    def getCurrentPage(self):
        'Get the current page tab'

        for child in self:
            if child.selected:
                return child

    def findPageTab(self, name, logName=None, retry=True, raiseException=True, setReference=True):
        'Search for a page tab'

        # for performance, don't search for pageTabs recursively; set a reference to the page tab (if/when found) by default
        return self.__getattr__('findPageTab')(name, logName=logName, retry=retry, recursive=False, raiseException=raiseException, setReference=setReference)

    def select(self, name, logName=None, log=True):
        'Select a tab'

        # we don't use self.findPageTab or self.__getattr__('findPageTab') 
        # here because findPageTab tries to promote tabs to specific classes 
        # which may have constructors that look for widgets that are 
        # lazy-loaded, causing bogus searchErrors.
        tab = utils.findDescendant(self, lambda x: x.role == pyatspi.ROLE_PAGE_TAB and utils.equalsOrMatches(x.name, name) and x.showing, \
            recursive=False)

        # do the work of actually selecting the tab.  this should cause
        # lazy-loaded widgets to be loaded.
        self.selectChild(tab.getIndexInParent())

        # now search for the tab as if we haven't done any of the above, but 
        # don't do any logging
        tab = self.findPageTab(name, logName=logName)

        # now that we have the (possibly promoted) tab, do the logging
        if log: # we need to log after the find() because the tab might be promoted and have a different logName
            procedurelogger.action('Select the %s.' % tab, self)

        sleep(config.MEDIUM_DELAY)

        return tab

class PageTab(Accessible): # application wrappers around tabs should extend from this class
    def select(self, log=True):
        '''
        Select the page tab

        This method should not be used to initially select a tab; PageTabList.select()
        should be used instead.  Using PageTabList.findPageTab() may cause bogus search
        errors if the tab's class's constructor looks for sub-widgets and that are
        lazy-loaded.
        '''

        if log:
            procedurelogger.action('Select the %s.' % self, self.parent)

        self.parent.selectChild(self.getIndexInParent())

    def assertSelected(self, log=True):
        'Raise an exception if this tab is not selected'

        if log:
            procedurelogger.expectedResult('The %s is selected.' % self)

        def selected():
            return self.selected

        assert utils.retryUntilTrue(selected)

class Table(Accessible):
    def __getattr__(self, attr):
        itable = self._accessible.queryTable()
        if attr == 'nRows': return itable.nRows
        elif attr == 'nColumns': return itable.nColumns
        elif attr == 'summary': return itable.summary
        elif attr == 'caption': return itable.caption
        elif attr == 'nSelectedRows': return itable.nSelectedRows
        elif attr == 'nSelectedColumns': return itable.nSelectedColumns
        elif attr == 'getRowAtIndex': return itable.getRowAtIndex
        elif attr == 'getColumnAtIndex': return itable.getColumnAtIndex
        elif attr == 'getSelectedRows': return itable.getSelectedRows
        elif attr == 'getSelectedColumns': return itable.getSelectedColumns
        elif attr == 'isRowSelected': return itable.isRowSelected
        elif attr == 'isColumnSelected': return itable.isColumnSelected
        elif attr == 'addRowSelection': return itable.addRowSelection
        elif attr == 'addColumnSelection': return itable.addColumnSelection
        elif attr == 'removeRowSelection': return itable.removeRowSelection
        elif attr == 'removeColumnSelection': return itable.removeColumnSelection
        else: return super(Table, self).__getattr__(attr)

    def getAccessibleAt(self, row, col):
        return self._promote(self._accessible.queryTable().getAccessibleAt(row, col))

    def assertTableCellAt(self, row, col, text):
        assert utils.retryUntilTrue(lambda : self.getAccessibleAt(row, col).name == text)

    def isCellSelected(self, row, col):
        return self._accessible.queryTable().isSelected(row, col)

    def select(self, name, logName=None, log=True):
        'Select a table cell'

        sleep(config.MEDIUM_DELAY)

        # don't checkShowing because we want to allow selecting table cells that are out of the viewport 
        cell = self.findTableCell(name, logName=logName, checkShowing=False, breadthFirst=False)
        cell.select(log=log)

        return cell

    def activate(self, name, logName=None, log=True):
        'Activate (double-click) a table cell'

        sleep(config.MEDIUM_DELAY)

        # don't checkShowing because we want to allow selecting table cells that are out of the viewport 
        cell = self.findTableCell(name, logName=logName, checkShowing=False, breadthFirst=False)
        cell.activate(log=log)

        return cell

    def assertTableCell(self, name, checkShowing=True, retry=False, recursive=True, breadthFirst=False, raiseException=False):

        sleep(config.MEDIUM_DELAY)

        self.__getattr__('assertTableCell')(name, checkShowing=checkShowing, retry=retry, recursive=recursive, breadthFirst=breadthFirst, raiseException=raiseException)

    def assertNoTableCell(self, name, checkShowing=True, retry=False, recursive=True, breadthFirst=False, raiseException=False):

        sleep(config.MEDIUM_DELAY)

        self.__getattr__('assertNoTableCell')(name, checkShowing=checkShowing, retry=retry, recursive=recursive, breadthFirst=breadthFirst, raiseException=raiseException)

class TableCell(Accessible):
    def select(self, log=True):
        'Select the table cell'

        if log:
            procedurelogger.action('Select %s.' % self, self)

        self.grabFocus()

    def assertSelected(self, log=True):
        'Raise an exception if this tab is not selected'

        if log:
            procedurelogger.expectedResult('%s is selected.' % self)

        def selected():
            return self.selected

        assert utils.retryUntilTrue(selected)

    def activate(self, log=True):
        'Activate (double-click) the table cell'

        if log:
            procedurelogger.action('Double-click %s.' % self, self)

        self.grabFocus()
        super(TableCell, self).activate()

    def typeText(self, text, log=True):
        'Type text into the table cell'

        self.mouseClick()

        if log:
            procedurelogger.action('Enter "%s" into %s.' % (text, self), self)

        sleep(config.SHORT_DELAY)
        super(TableCell, self).typeText(text)
        pyatspi.Registry.generateKeyboardEvent(self._charToKeySym('Return'), None, pyatspi.KEY_SYM)

    def mouseClick(self, button=1, xOffset=0, yOffset=0, log=True):
        '''
        Click the table cell

        If the table cell is editable, this should trigger the "edit mode".  If
        you just want to select the table cell, use select() instead.
        '''

        if log:
            procedurelogger.action('Click %s.' % self, self)

        super(TableCell, self).mouseClick(button=button, xOffset=xOffset, yOffset=yOffset)

class TreeTable(Table):
    pass

class Button(Accessible): # ROLE_BUTTON doesn't actually exist, this is just used as a base class for the following classes
    def click(self, log=True):
        'Click the button'

        if log:
            procedurelogger.action('Click the %s.' % self, self)

        super(Button, self).click()

class PushButton(Button):
    pass

class RadioButton(Button):
    pass

class CheckBox(Button):
    pass

class Text(Accessible):
    def enterText(self, text, log=True):
        'Enter text'

        if log:
            procedurelogger.action('Enter "%s" into the %s.' % (text, self), self)

        self.text = text # since we don't absolutely need to use typeText here, lets do it this way since its a lot faster

class PasswordText(Text):
    pass

class MenuBar(Accessible):
    def _open(self, path):
        'Open a menu without any logging'

        parent = self
        for menu in path:
            parent = parent.findMenu(menu, recursive=False)
            parent.click() # open the menu so that the children are showing

        return parent # return the last menu

    def select(self, path, log=True):
        '''
        Select a menu item

        Path must be an array of strings; regular expressions are not supported.
        '''

        if log:
            procedurelogger.action('Under the %s menu, select %s.' % (' => '.join(path[0:-1]), path[-1].replace('...', '')), self)

        parent = self._open(path[0:-1]) # the last item in the path is excluded because we're going to click that item

        item = utils.findDescendant(parent, lambda x: (x.role == pyatspi.ROLE_MENU_ITEM or x.role == pyatspi.ROLE_CHECK_MENU_ITEM) \
            and utils.equalsOrMatches(x.name, path[-1]) and x.showing, recursive=False)
        item.click()

        return item

    def open(self, path):
        '''
        Open a menu

        Path must be an array of strings; regular expressions are not supported.
        '''

        procedurelogger.action('Open the %s menu.' % ' => '.join(path), self)

        return self._open(path)

class ComboBox(Accessible):
    def select(self, name, logName=None, log=True):
        'Select an item'

        item = self.findMenuItem(name, logName=logName, checkShowing=False)

        if log:
            procedurelogger.action('Select %s.' % str(item).replace(' menu option', ''), self)

        item.click()

        return item

