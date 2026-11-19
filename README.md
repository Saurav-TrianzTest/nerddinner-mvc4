Nerd Dinner
===========

Copy of original ASP.NET MVC 4 source code for http://nerddinner.com

The Open Source ASP.NET MVC Project that helps nerds and computer people plan get-togethers. You can see the site running LIVE at http://www.nerddinner.com. This project's goal is to create the best website for Technology People to host their Lunches, Flashmobs, Dinners and informal get-togethers.

The original tutorial no longer applies exactly, as it was written a few years ago for an older version of MVC. That said, it can still be a good learning tool so you can download a 185-page free PDF walkthrough loaded with code and screenshots at http://tinyurl.com/aspnetmvc.

## Running the .NET 10 version

This repository has been migrated to a single ASP.NET Core MVC project (`src/NerdDinner.csproj`, `net10.0`).

* Build: `dotnet build NerdDinner.sln`
* Databases: set `ConnectionStrings:DefaultConnection` (Identity) and `ConnectionStrings:NerdDinnerContext` (dinners, uses the SQL Server `geography` type) in `appsettings.json`/user-secrets, then run
  `dotnet ef database update --context NerdDinnerContext` and `dotnet ef database update --context ApplicationDbContext`
  (or set `Database:MigrateOnStartup` to `true`).
* Secrets (`Geo:BingMapsKey`, `Geo:IpInfoDbKey`, `Geo:GeoNamesUserName`, `Authentication:*`) must be supplied through user-secrets or environment variables.
* Existing SimpleMembership users can be imported with `src/Data/LegacyMembershipImport.sql`; their password hashes are verified by ASP.NET Core Identity and upgraded on first login.
