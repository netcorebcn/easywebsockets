API_KEY=$1

rm -rf ./src/EasyWebSockets/nupkgs
dotnet pack ./src/EasyWebSockets/EasyWebSockets.csproj --output nupkgs
dotnet nuget push ./src/EasyWebSockets/nupkgs/*.nupkg  \
-s https://www.nuget.org/api/v2/package \
-k $API_KEY