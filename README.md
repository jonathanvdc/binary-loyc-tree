# Build Status

Linux | Windows
----- | -------
[![Build Status](https://travis-ci.org/jonathanvdc/binary-loyc-tree.svg?branch=master)](https://travis-ci.org/jonathanvdc/binary-loyc-tree) | [![Build status](https://ci.appveyor.com/api/projects/status/t3w2i0blf050ami4?svg=true)](https://ci.appveyor.com/project/jonathanvdc/binary-loyc-tree)

# Binary Loyc Tree format
The binary loyc tree (BLT) file format is a succinct binary representation of loyc trees.
Its goal is to serve as an efficient format for program-to-program loyc tree transfer.
BLT optimizes for relatively large files, such as entire assemblies, and emphasizes read times and
on-disk size.

## File layout

BLT files have the following layout:
 * Magic string ("BLT")
 * Version number
 * Header
   * Symbol table (length-prefixed list of symbol definitions, which are really just character strings)
   * Template table (length-prefixed list of template definitions)
 * Node table (length-prefixed list of (encoding, length)-prefixed node lists)
 * Top-level nodes (length-prefixed list of indices in the node table)

This layout was chosen because it makes it easy to read BLT files from start to end, without any
seek operations.
