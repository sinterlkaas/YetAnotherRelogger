PETool 1.5 -- Useful command-line tool for manipulating EXE & DLL files
Web site: http://wiz0u.free.fr/prog/PETool
---------------------------------------------------------

Usage
-----
PETool.exe <pathname> <action> [<action>] [<action>]

<pathname> should be the path to a PE module (Portable Executable), typically an EXE or DLL
	It can also contain wildcards to apply the following actions on several files

Some <actions> may require additional parameters


Actions
-------

/Destamp
	Remove the build timestamps from the file.
	This allows identical builds made at different times to be binary identical

/ShowStamp
	Displays the linker's build date/time of the file

/DelRes <type> <name> <language>
	Remove given resource.
	Type & name can be numeric or string. Type can also be a standard type like ICON, BITMAP, ...
	Language must be numeric. Numeric values can be specified as hexadecimal 0xHHH

/SetRes <type> <name> <language> <file>
	Add/replace given resource with data from the given file.
	Standard resource type like ICON, BITMAP are recognized and converted adequately
	
/GetRes <type> <name> <language> <file>
	Retrieve given resource data and store it into the given file.
	Standard resource type like ICON, BITMAP are recognized and converted adequately

/Unsign
	Remove the Authenticode signature from the file.
	This is sometimes necessary in order to re-sign the file.

/NSIS <output.nsh>
	Generates an NSIS header with information from the file's VERSION resource

/FileVer <n.n.n.n>
	Set the FileVersion information in the file's VERSION resource to the given version

/ProductVer <n.n.n.n>
	Set the ProductVersion information in the file's VERSION resource to the given version

/VerQuery <EntryName>
	Outputs the value of the given entry found in the file's VERSION resource
	
/VerChange <EntryName> <value>
	Change the value of the given entry with the new value in the file's VERSION resource

/Release
	Unset the 'Prerelease' bit from the file's VERSION resource.


Version history
---------------
1.5 : Added /DelRes /SetRes /GetRes (resource handling functions)
1.4 : Fixed some bugs
1.3 : Added /ShowStamp
1.2 : Added /Unsign
1.1 : Added /Destamp
1.0 : First version (had only Version manipulation)


License
-------
Copyright (c) 2010 Olivier Marcoux

This software is provided 'as-is', without any express or implied warranty. In no event will the authors be held liable for any damages arising from the use of this software.

Permission is granted to anyone to use this program for any purpose, including commercial applications, and to redistribute it freely, subject to the following restriction:

The origin of this software must not be misrepresented; you must not claim that you wrote the original software. If you use this software in a product, an acknowledgment in the product documentation would be appreciated but is not required.
