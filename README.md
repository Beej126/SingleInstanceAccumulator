# SingleInstanceAccumulator

See [typical stack-o's](https://www.google.com/search?q=context+menu+single+instance+site%3Astackoverflow.com) expressing the need, e.g. [this one](https://stackoverflow.com/questions/1821662/how-to-add-new-items-to-right-click-event-on-folders-and-files-in-windows).

## Explorer Context Menu config
```batch
::creates the entry
:: and crucial multi-file handling property
reg add "HKEY_CLASSES_ROOT\FileType\shell\YourNewContextMenu" /f /v "MultiSelectModel" /d "Player"

::your desired command line
reg add "HKEY_CLASSES_ROOT\FileType\shell\YourNewContextMenu\command" /f /ve /t REG_EXPAND_SZ /d "***see command line examples***"
```
e.g. On my system, for ".mov" files, I would replace `FileType` above with `VLC.mov`

### Complex REG ADD example
Replace "*** see command line examples ***" above with your desired command line.<br/>

Note: **quotes** & **environment variables** must be escaped and escaping work slightly differently for the initial command versus later in the string!?!

Note2: this form works directly from interactive cmd.exe; if you're running from a batch file, all the %'s need to be doubled up (as is typical)

Note3: i've subsequently changed this context menu to associate to all file types (via "HKEY_CLASSES_ROOT\*\"), to avoid mapping to each file type individually.

```
λ reg add "HKEY_CLASSES_ROOT\*\shell\Transcode\command" /f /ve /t REG_EXPAND_SZ /d "\"^%bin^%\SingleInstanceAccumulator\" -f \"-c:powershell -ExecutionPolicy bypass "\"^%bin^%\transcode.ps1\"" -listFilePath '$files'\" \"%1\""
```
- tip: powershell treats spaces as separate args on the command line, so if your corresponding path for %bin% above has spaces, those will need to be escaped with back ticks (` )... 

when run from windows explorer context menu the above reg entry will execute a command line that looks like this:
```
"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe" -ExecutionPolicy bypass C:\bin\transcode.ps1 -listFilePath 'C:\Users\Beej\AppData\Local\Temp\tmp5EEF.tmp'
```

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
- shell > command registry entries require full path on the command to work (e.g. c:\bin\SingleInstanceAccumulator.exe, not just SingleInstanceAccumulator.exe)... for some ungawdly reason of Windows, our path environment variable is not honored.
- but the -c:command specified to SingleInstanceAccumulator will know your environment's path

### example 1 - put selected files onto clipboard
- this is a fun use of delimiter to create multiple echo statements that are then piped to "clip" device
- to script the corresponding registry entires in a "setup" batch file, we need to **double** escape the "&" and "|" characters...
  - once to avoid the initial setup batch file from interpreting them directly
  - and secondly to avoid the command line launched from SingleInstanceAccumulator from processing them
  - so that they are finally present to be run directly from the final cmd.exe

`setup.cmd`
```batch
reg add "HKEY_CURRENT_USER\Software\Classes\*\shell\Path2Clip\command" /f /ve /t REG_EXPAND_SZ /d "\"^%%bin^%%\SingleInstanceAccumulator\" -w -d:\" ^^^& echo \" \"-c:cmd /c (echo $files) ^^^| clip\" \"%%1\""
```

### example 2 - PowerShell script - using temp file approach
- note: **-f** usage

command line:
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
- leaving off -f is what formats all the accumulated args together as an array
- using **-q** puts the specified quote character around EACH of the comma delimited file arguments accumulated, so it comes into the ps1 script **as a true array**

shell > command line:
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
