param(
  [String[]]$filesArray
)

echo "length of `$filesArray: $($filesArray.Length)"
$filesArray | % { $_ }

pause