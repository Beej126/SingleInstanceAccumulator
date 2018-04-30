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
λ reg add "HKEY_CLASSES_ROOT\*\shell\Transcode\command" /f /ve /t REG_EXPAND_SZ /d "\"^%bin^%\SingleInstanceAccumulator\" -f \"-c:powershell -ExecutionPolicy bypass "\"^%bin^%\transcode.ps1\"" -list '$files'\" \"%1\""
```

## Usage
```shell
"-c:command line" (default: cmd /c echo $files && pause)
  $files will be replace with aggregated list

-f = output each item on separate line to new tempfile
  $files will be replaced by the tempfile path
  quote will default to nothing

-d:delimiter (default: ,)
-q:quote (default: ")
-t:timeout millisecs (default: 200)
-w = hidden launch
-v = debug output
```
<br/>

## Command Line Examples
note: **initial command must have path** for shell > command to work

### PowerShell & temp file
note: **-f** usage
```
%bin%\SingleInstanceAccumulator -f "-c:powershell -ExecutionPolicy bypass %bin%\test.ps1 -list '$files'" "%1"
```

### PowerShell & *inline* files list
note: **-q** usage
```
%bin%\SingleInstanceAccumulator -q:' "-c:powershell -ExecutionPolicy bypass %bin%\test.ps1 -list $files" "%1"
```

## test.ps1 (with temp file)
```powershell
param(
  [String]$listFilePath
)

gc $listFilePath | % { $_ }

pause

erase $listFilePath

pause
```

## test.ps1 (with files array parm)
```powershell
param(
  [String[]]$filesList
)

$filesList | % { $_ }

pause
```

