#! /usr/bin/python

import re
import sys
import time
import getopt
import subprocess
#from pprint import pprint



text = """script /Users/robnea/dev/fsharp-fsbinding/emacs/test.fs
type Hello(who) =
  member x.Say() =
    printfn "Hello %s!" who
    
  member x.Try() =
    printfn "Hello %s!" who
    
  member x.Bry() =
    printfn "Hello %s!" who

let hi = Hello("world")

let hifice = 5

let fufsldjafif = "bla"

let fufasdfikl = "no"

let x = hi.

<<EOF>>
"""

test = text + """completion 19 11
quit
"""

la = """
completion 5 1
completion 5 2
completion 5 3
completion 5 4
completion 5 5
tip 4 5
quit
"""

def main():
  try:
    opts, args = getopt.getopt(sys.argv[1:], "nva:w:")
  except getopt.GetoptError, err:
    # print help information and exit:
    print str(err) # will print something like "option -a not recognized"
    sys.exit(2)
  upload   = True
  verbose  = False
  wait     = 0
  attempts = 1
  for o, a in opts:
    if o == "-n":
      upload = False
    elif o == "-v":
      verbose = True
    elif o == "-a":
      attempts = int(a)
    elif o == "-w":
      wait = int(a)
    else:
      assert False, "unhandled option"


  child = subprocess.Popen(['mono', '../bin/fsautocomplete.exe'], stdin=subprocess.PIPE, stdout=subprocess.PIPE)

  out, err = child.communicate(test)

  print "output:\n%s" % out
  

if __name__ == "__main__":
  main()
