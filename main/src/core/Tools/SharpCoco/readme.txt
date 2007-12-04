Modified version of Coco/R

The original version can be found at:
http://www.ssw.uni-linz.ac.at/Research/Projects/Coco/CSharp/

Changes

+ Added #line pragmas for the generated parser
+ Now Coco uses more enums than ints...
+ no static method generation (now all is public)
+ Error & Scanner are now fields inside the parser, no more static
  calling

Mike