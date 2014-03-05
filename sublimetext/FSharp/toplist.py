import argparse
import json
import os
import plistlib


THIS_DIR = os.path.abspath(os.path.dirname(__file__))

def build(source):
    with open(source, 'r') as f:
        json_data = json.load(f)
        plistlib.writePlist(json_data, os.path.splitext(source)[0] + '.tmLanguage')

if __name__ == '__main__':
    parser = argparse.ArgumentParser(
                        description="Builds .tmLanguage files out of .JSON-tmLanguage files.")
    parser.add_argument('-s', dest='source',
                        help="source .JSON-tmLanguage file")
    args = parser.parse_args()
    if args.source:
        build(args.source)
