# HashServer
A Kestrel app server provides a just in time JitHash white list.  The client is in powershell and can be used to test remote system memory for unknown code.

## UI is in Scripting Repo
Use the powershell code in https://github.com/K2/Scripting as the client for this server.

You can optionall use the GUI to browse in a TreeMap and also a hex diff view.  Or use the returned PS objects to write you're own analytics.

## Overview

The goal is to make memory integrity checking as easy as possiable.  One of the primary roadblocks towards memory integrity chekcing is that a "golden image" database must be maintained.  This "golden image" database is usually represented in the form of cryptographically secure hash values (See tripwire for a filesystem integrity checking solution).  

Every time a patch, update or recompilation is done that chanegs a system binary, the associated integrity information (hash value) has to be updated in lock step or else you will have excessive false positives regarding unknown code files/memory blocks.

## JitHashing 
This HashServer implmentation attempts to lower the cost of ownership, administrative overhead and overall pain points regarding the maintence of the hash integrity info.  In a nutshell, you simply configure file paths to known good copies of what you have deployed (filesystem images mounted locally is probably you're best bet over network shares :).

HashServer will then, upon recieving a client JSON call (see PowerShell code for full client), will dynamically generate the expected hash values based on the required permutations that occur when a binary is loaded into memory.  This then will allow the HashServer to validate the client secure hashes and report on possiable unknown code in memory without having to manage any database at all!

### But I've got to have a filesystem around?
Yes, though that feels a lot easier to have a virtual disk or some huge cache of whatever applications you have deployed than to have to manage a database since if you have a virtual disk that's a templalte for you're servers, workstations etc.. you can simply update it and expose it's drive to the HashServer and it will take care of the rest.



