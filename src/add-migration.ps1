$env:ConnectionStrings__Sql="Data Source=(localdb)\MSSQLLocalDb;Initial Catalog=SteamScrapper;Integrated Security=true"

$MigrationName = Read-Host "Enter the name of the migration: "

dotnet ef migrations add $MigrationName --project SteamScrapper.Infrastructure --output-dir Database/Migrations
