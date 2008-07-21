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

'Custom exceptions'

class SearchError(Exception):
    'Raised when a search method fails'
    pass

class NotSensitiveError(Exception):
    'Raised when an action cannot be performed because the widget is insensitive'
    pass

