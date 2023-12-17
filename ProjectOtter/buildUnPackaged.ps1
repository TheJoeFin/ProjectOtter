$BuildPath = "$PSScriptRoot\bld\x64"
$BuildPathSC = "$PSScriptRoot\bld\x64\Project-Otter-Self-Contained"
$Version = Get-Date -Format "yyyy-MM-dd" # 2020-11-1
$VersionDot = $Version -replace '-','.'
$Project = "ProjectOtter"
$Archive = "$BuildPath\$Project-$Version.zip"
$ArchiveSC = "$BuildPath\$Project-Self-Contained-$Version.zip"

# Clean up
if(Test-Path -Path $BuildPath)
{
    Remove-Item $BuildPath -Recurse
}

# Dotnet restore and build
dotnet publish `
	   --runtime win-x64 `
	   --self-contained false `
	   -c Release `
	   -v minimal `
	   -p:Platform=x64 `
	   -p:PublishReadyToRun=true `
	   -p:PublishSingleFile=true `
	   -p:Version=$VersionDot `

