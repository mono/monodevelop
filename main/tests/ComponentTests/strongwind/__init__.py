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

"""
Strongwind is a GUI test automation framework inspired by dogtail.

Strongwind is object-oriented and extensible. You can use Strongwind to build
object-oriented representations of your applications ("application wrappers"),
then reuse the application wrappers to quickly develop many test scripts.
Strongwind scripts generate a human-readable log that contains the action,
expected result and a screen shot of each step.  Most simple actions are logged
automatically.
"""

import config

import procedurelogger
import watchdog
import cache

from errors import *
from utils import *
from accessibles import *

