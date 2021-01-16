[CmdletBinding()]
Param (
	[string]$inputFolder,
	[string]$outputFolder,
	[string]$differ = 'C:\Program Files\KDiff3\kdiff3.exe'
)

$inputFilenames = Get-ChildItem $inputFolder -File
$temp = $env:temp

$inputFilenames | 
	ForEach-Object {
		Write-Host "Processing $(Split-Path $_ -Leaf)..."

		$filename = Split-Path $_ -LeafBase
		$aFile = Join-Path $temp "$($filename)_a"
		$bFile = Join-Path $temp "$($filename)_b"
		$diffFile = Join-Path $outputFolder "$($filename).diff"

		.\im-flow\bin\Debug\netcoreapp3.1\im-flow.exe $_ -o $aFile | Out-Null
		.\im-flow-fsharp\bin\Debug\net5.0\im-flow-fsharp.exe $_ -o $bFile | Out-Null

		# diff (Get-Content $aFile) (Get-Content $bFile) > $diffFile
		&($differ) $aFile $bFile 
	}
