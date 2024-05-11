REM NET Core Software Library

REM Build the library...
dotnet build "./OGA.WinSvc.Lib_NET452/OGA.WinSvc.Lib_NET452.csproj" -c Debug --runtime win --no-self-contained
dotnet build "./OGA.WinSvc.Lib_NET48/OGA.WinSvc.Lib_NET48.csproj" -c Debug --runtime win --no-self-contained
dotnet build "./OGA.WinSvc.Lib_NET5/OGA.WinSvc.Lib_NET5.csproj" -c Debug --runtime win --no-self-contained
dotnet build "./OGA.WinSvc.Lib_NET6/OGA.WinSvc.Lib_NET6.csproj" -c Debug --runtime win --no-self-contained
dotnet build "./OGA.WinSvc.Lib_NET7/OGA.WinSvc.Lib_NET7.csproj" -c Debug --runtime win --no-self-contained

REM Create the composite nuget package file from built libraries...
C:\Programs\nuget\nuget.exe pack ./OGA.WinSvc.Lib.nuspec -IncludeReferencedProjects -symbols -SymbolPackageFormat snupkg -OutputDirectory ./Publish -Verbosity detailed

REM To publish nuget package...
dotnet nuget push -s http://192.168.1.161:8079/v3/index.json ".\Publish\OGA.WinSvc.Lib.1.0.1.nupkg"
dotnet nuget push -s http://192.168.1.161:8079/v3/index.json ".\Publish\OGA.WinSvc.Lib.1.0.1.snupkg"


TIMEOUT 10

ECHO "DONE"