$env:ConnectionStrings__Sql="Data Source=(localdb)\MSSQLLocalDb;Initial Catalog=SteamScrapper;Integrated Security=true"
dotnet ef database update --project SteamScrapper.Infrastructure
