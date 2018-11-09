$dbServer           = "<< YOUR SERVER NAME >>"
$db                 = "<< YOUR DATABASE NAME >>"
$dbConnectionString = "Data Source=$($dbServer).database.windows.net; Initial Catalog=$($db);";


# Get a managed service identity toekn
$realm = "https://database.windows.net/"
$token = ((Invoke-WebRequest -Uri http://localhost:50342/oauth2/token -Method GET -Body @{resource=$realm} -Headers @{Metadata="true"}).Content | ConvertFrom-Json).access_token

# Open the database using the access token
$dbConnection                  = New-Object System.Data.SqlClient.SqlConnection
$dbConnection.ConnectionString = $dbConnectionString
$dbConnection.AccessToken      = $token
$dbConnection.Open()

# Query some daterz
$dbCommand                = $dbConnection.CreateCommand()
$dbCommand.CommandTimeout = 120
$dbCommand.CommandText    = "Select * from People"

# Spit out the results
$reader = $dbCommand.ExecuteReader()

while ($reader.Read())
{
    Write-Output "$($reader.GetValue(0)) :: $($reader.GetValue(1))"
}

# Clean up
$reader.Close()
$reader.Dispose()
$dbCommand.Dispose()
$dbConnection.Dispose()

