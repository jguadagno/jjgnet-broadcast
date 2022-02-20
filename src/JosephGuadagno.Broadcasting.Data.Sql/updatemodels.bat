dotnet add package  Microsoft.EntityframeworkCore.Tools
:: File in the password from user secrets
dotnet ef dbcontext scaffold "Server=.;Database=jjgnet;user id=jjgnet_user;password=" Microsoft.EntityFrameworkCore.SqlServer -c BroadcastingContext -o Models --force
dotnet remove package Microsoft.EntityframeworkCore.Tools