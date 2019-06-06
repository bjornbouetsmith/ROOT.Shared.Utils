param(
  [string]$VersionFile = "$env:BUILD_SOURCESDIRECTORY\version.json",
  [string]$SourceBranch = $env:BUILD_SOURCEBRANCH,
  [string]$BuildNumber = $env:BUILD_BUILDNUMBER,
  [int]$MaxCharacters = 30,
  [switch]$UseCurrentDateForPatchValue = $false,
  [switch]$UsePreReleasePrefixes = $false,
  [switch]$Truncate = $false
)
if($BuildNumber.Length -eq 0)
{
 $BuildNumber="0"
}
$Revision = $buildNumber
Write-Output "Getting the version number from file: [$VersionFile]"
[version]$version
$versionFileContent = Get-Content -Path $VersionFile -raw | ConvertFrom-Json
$fileVersion = $versionFileContent.version
if ($fileVersion -is [String]) {
  $version = New-Object System.Version($fileVersion)
}
else { 
  $version = New-Object System.Version($fileVersion.major, $fileVersion.minor, $fileVersion.patch, $fileVersion.revision)
}
$major = $version.Major
$minor = $version.Minor
$patch = $version.Build

Write-Output "Source version is: [$version]"
if ($UseCurrentDateForPatchValue -eq $true) {
  $patch = Get-Date -Format "yyMdd"
}

if($SourceBranch.Length -eq 0)
{
 $SourceBranch="refs/heads/master"
}

[string]$sourceBranchName = $SourceBranch.Substring($SourceBranch.LastIndexOf("/") + 1, $SourceBranch.Length - $SourceBranch.LastIndexOf("/") - 1).Replace(".", "")
if ($sourceBranchName.Substring(0, 1) -match "[0-9]") {
  Write-Output "Source branch name starts with a number. Prefixing it with B."
  $sourceBranchName = "B$sourceBranchName"
}
if ($SourceBranch -eq "refs/heads/master") {
  $buildNumber = "$major.$minor.$patch.$Revision"  
}
elseif ($UsePreReleasePrefixes -and $SourceBranch -eq "refs/heads/develop") {
  $buildNumber = "$major.$minor.$patch-alpha.$Revision"  
}
elseif ($UsePreReleasePrefixes -and $SourceBranch -eq "refs/heads/release/*") {
  $buildNumber = "$major.$minor.$patch-rc.$Revision"
}
elseif ($UsePreReleasePrefixes -and $SourceBranch -eq "refs/heads/hotfix/*") {
  $buildNumber = "$major.$minor.$patch-fix.$Revision"
}
else {
  $buildNumber = "$major.$minor.$patch.$Revision-$sourceBranchName"
}
if ($buildNumber.EndsWith(".")) {
  $buildNumber.Remove($buildNumber.Length - 1)
}
Write-Output "Setting build number to: [$buildNumber]"
Write-Output "##vso[build.updatebuildnumber]$buildNumber"