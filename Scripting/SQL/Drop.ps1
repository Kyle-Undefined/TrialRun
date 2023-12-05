param(
    [Parameter()]
    [String] $Name
)
[string] $dbCommand = "USE master;" +
"ALTER DATABASE [$Name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;" +
"DROP DATABASE [$Name];"

sqlcmd -E -Q $dbCommand