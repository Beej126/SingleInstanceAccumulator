reg add "HKEY_CURRENT_USER\Software\Classes\*\shell\MyCommand\command" /f /ve /t REG_EXPAND_SZ /d "\"^%%bin^%%\SingleInstanceAccumulator\" -q:' \"-c:pwsh -Command \\\"&'%~dp0test.ps1' -filesArray $files\\\"\" \"%%1\""
@echo off
rem double quotes surrounding the value assigned to /d in the reg add command                     ^---------------------------------------------------------------------------------------------------------------------^
rem     (escaped) double quotes surrounding the exe path in case it has spaces                     ^------------------------------------^
rem     double escaped %'s to avoid being expanded by the cmd.exe parser and therefore yield single %bin% in the registry which will get expanded at runtime, this could be allowed to expand at cmd.exe execution but that would be less flexible to MOVING the %bin% path AFTER these registry entries have been created
rem                                                                                                  ^-----^
rem     (escaped) double quotes around the -c portion which get stripped off by SingleInstanceAccumulator                                       ^-------------------------------------------------------------^
rem     slash is escaped so it passes through initial parse, along with adjascent escaped quotes                                                                   ^ ^------------------------------------^ ^
rem end of /d quoted string

rem escaped quotes and percents around the final %1 should be pretty apparent at this point                                                                                                                      ^ ^--^

rem under cmd.exe parser which is in play for .cmd batch files:
rem 1) to get double quotes inside of double quotes must be \ escaped
rem 2) backslash must be escaped itself
rem 3) hat character ^ escapes other special characters like %, which is useful to avoid premature expansion of %envar%

rem using pwsh -Command lets us leverage powershell's native internal single quoted string handling which is the main key to getting an array of strings with spaces passed to a powershell script
rem but then we also have to use the "&" "call operator" to execute the ps1
