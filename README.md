# SingleInstanceAccumulator

See [typical stack-o's](https://www.google.com/search?q=context+menu+single+instance+site%3Astackoverflow.com) expressing the need, e.g. [this one](https://stackoverflow.com/questions/1821662/how-to-add-new-items-to-right-click-event-on-folders-and-files-in-windows).

## Explorer Context Menu config
```batch
:: crucial multi-file handling property - https://learn.microsoft.com/en-us/windows/win32/shell/context-menu-handlers?redirectedfrom=MSDN#employing-the-verb-selection-model
reg add "HKEY_CLASSES_ROOT\FileType\shell\YourNewContextMenu" /f /v "MultiSelectModel" /d "Player"

::your desired command line
reg add "HKEY_CLASSES_ROOT\FileType\shell\YourNewContextMenu\command" /f /ve /t REG_EXPAND_SZ /d "***see command line examples***"
```
for example, to send ".mov" files to VLC, I would replace `FileType` above with `VLC.mov`

### Complex REG ADD example
- Replace "*** see command line examples ***" above with your desired command line.<br/>
- **quotes** & **environment variables** must be escaped and escaping work slightly differently for the initial command versus later in the string!?!
- the format below works directly from interactive cmd.exe; **if you're running from a batch file, all the %'s need to be doubled up** (as is typical)
- the example below maps to **all file types** (via "HKEY_CLASSES_ROOT\\\*\\"), which avoids needing to create an entry for eaach individual file type if you don't need it filetype specific.

```
λ reg add "HKEY_CLASSES_ROOT\*\shell\Transcode\command" /f /ve /t REG_EXPAND_SZ /d "\"^%bin^%\SingleInstanceAccumulator\" -f \"-c:powershell -ExecutionPolicy bypass "\"^%bin^%\transcode.ps1\"" -listFilePath '$files'\" \"%1\""
```
when run from windows explorer context menu the above reg entry will execute a command line that looks like this:
```
"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe" -ExecutionPolicy bypass C:\bin\transcode.ps1 -listFilePath 'C:\Users\Beej\AppData\Local\Temp\tmp5EEF.tmp'
```

side note if interested, here's the [transcode.ps1](https://github.com/Beej126/PowerShell/blob/master/transcode.ps1) script where i drive [handbrake.exe](https://handbrake.fr/downloads.php) command line to process .mov files (from point and shoot cameras) into mp4's.

## Usage
```shell
"-c:command line" (default: cmd /c echo $files && pause)
  $files will be replace with aggregated list

-f = output each item on separate line to new tempfile
  $files will be replaced by the tempfile path
  quote will default to nothing

-d:delimiter (default: ,)
-q:quote character around each accumulated argument (default: ") - see examples below
-t:timeout millisecs (default: 200)
-w = hidden launch
-v = debug output
```
<br/>

## Command Line Examples
Heads up:
- registry entries like HKEY_CURRENT_USER\Software\Classes\*\shell\xxxx\command require full path on the command (e.g. c:\bin\SingleInstanceAccumulator.exe, not just SingleInstanceAccumulator.exe)... for some ungawdly reason of Windows, our path environment variable is not honored.
- but the -c:command executed from SingleInstanceAccumulator will know your environment's path.

### example 0 - feed files to a 3rd party exe
- save this in a setup.cmd batch file to have convenient way to re-apply to new workstations
- **big reminder: %'s are doubled up to be executed from batch file, if you did this in interactive cmd.exe just use single %'s**
```batch
reg add "HKEY_CURRENT_USER\Software\Classes\*\shell\MyCommand\command" /f /ve /t REG_EXPAND_SZ /d "\"^%%bin^%%\SingleInstanceAccumulator\" -w \"-c:myprogram.exe $files\" \"%%1\""
```


### example 1 - put selected files onto clipboard
- this is a fun use of delimiter to create multiple echo statements that are then piped to "clip" device
- note using the delimeter to add echo commands to each selected file as interesting leverage of native batch file capabilities
- to script the corresponding registry entires in a "setup" batch file, we need to **double** escape the "&" and "|" characters...
  - once to avoid the initial setup batch file from interpreting them directly
  - and secondly to avoid the command line launched from SingleInstanceAccumulator from processing them
  - so that they are finally present to be run directly from the final cmd.exe

`setup.cmd` (note: %'s are doubled up to be executed from batch file)
```batch
reg add "HKEY_CURRENT_USER\Software\Classes\*\shell\Path2Clip\command" /f /ve /t REG_EXPAND_SZ /d "\"^%%bin^%%\SingleInstanceAccumulator\" -w -d:\" ^^^& echo \" \"-c:cmd /c (echo $files) ^^^| clip\" \"%%1\""
```

### the following are examples of JUST the command line portion that can be plugged into INTERACTIVE cmd.exe, would need to double %'s to put in batch file

### example 2 - PowerShell script - using temp file approach
- note: **-f** usage

```cmd
%bin%\SingleInstanceAccumulator -f "-c:powershell -ExecutionPolicy bypass %bin%\test.ps1 -listFilePath '$files'" "%1"
```
note: -listFilePath command line argument corresponds to test.ps1's $listFilePath param shown next below

corresponding test.ps1 sample:
```powershell
param(
  [String]$listFilePath
)

gc $listFilePath | % { $_ }

# erase $listFilePath # you probably want this in your final script as good cleanup, commenting out for debug
pause
```

### example 3 - files list passed directly to powershell script on command line as a string array
- leaving off -f is what formats all the accumulated args together on one line that will come into powershell script as an array
- using **-q** puts the specified quote character around EACH of the comma delimited file arguments accumulated, so it comes into the ps1 script **as a true array**

```cmd
%bin%\SingleInstanceAccumulator -q:' "-c:powershell -ExecutionPolicy bypass %bin%\test.ps1 -filesArray $files" "%1"
```
- note: -filesArray command line argument corresponds to test.ps1's $filesArray param shown next below

will execute like this:
```
powershell test.ps1 -filesArray 'filepath1', 'filepath2', 'filepath3'
```

corresponding test.ps1 sample:
```powershell
param(
  [String[]]$filesArray
)

$filesArray | % { $_ }

pause
```

## General Tips
1. use the -v flag to see what will be executed so you can see how all the quoting and escaping works and fine tune from there
1. By default Explorer will only show context menu's when **less than 15 files are selected** - https://www.tenforums.com/tutorials/94513-fix-context-menu-items-missing-when-more-than-15-selected-windows.html ... here's the reg setting to bump it to 100:
   `reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer" /f /v MultipleInvokePromptMinimum /t REG_DWORD /d 100`
1. you should probably only **interactively test manual SingleInstanceAccumulator.exe calls via cmd.exe** (i.e. not at a powershell command line)... if you must test at powershell cli, you then also have to escape the single tick after -q (e.g. ``-q:`'``) because otherwise your interactive powershell cli will get caught on the single tick as an open string... remember these SingleInstanceAccumulator command lines are primarily intended to be fired from shell > command registry entries and that initial command line execution context is not powershell, it is traditional cmd.exe syntax rules, so the -q:' is fine there
1. can be super helpful to use "-NoExit" arg on powershell.exe command lines so those windows stay open to see errors 
1. powershell treats spaces as separate args on the command line, so if you're passing any args with spaces you'll need to escape them with back ticks (i.e. the ` character)
   1. one tricky place this can happen is if you're using a variable that winds up with spaces in it, e.g. a variable that contains the path to a command
   1. along those lines, i've found it convenient to create "setup.cmd" batch file scripts that populate my desired shell\blah\command registry entries  ... so here's some helpful code in traditional batch syntax:
     ```
     :: get path where this script is started from
     set cwd=%~dp0
     ::replace spaces with powershell escapes
     set cwd=%cwd: =` %
     ```
   1. **-OR-** instead of escaping spacing you can surround powershell arguments with double quotes which requires nested escaping for the quotes to pass through to that execution context... [example here in another repo of mine](https://gist.github.com/Beej126/f26e6649cfcc38accee3a0a8cc0a9d04#file-beejnetilpatcher_setup-cmd-L21)
