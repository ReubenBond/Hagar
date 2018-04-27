$env:VersionDateSuffix = [System.DateTime]::Now.ToString("yyyyMMddHHmmss");

dotnet build -bl:Build.binlog;
if ($LASTEXITCODE -eq 0) {
   dotnet pack -bl:Pack.binlog;
}