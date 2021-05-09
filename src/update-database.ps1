$env:ConnectionStrings__Sql="Data Source=.;Initial Catalog=SteamScrapper;Integrated Security=true"
dotnet ef database update --project SteamScrapper.Infrastructure
