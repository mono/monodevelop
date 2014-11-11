# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)


def decode_and_clean(data_bytes, encoding='utf-8'):
    return clean(decode(data_bytes, encoding))


def decode(data_bytes, encoding='utf-8'):
    return data_bytes.decode(encoding)


def clean(text):
    return text.replace('\r', '')
