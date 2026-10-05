import os

app_path = r"D:\Project\CapStone\ETR_Record_BE\ETR.Application\ETR.Application.csproj"
infra_path = r"D:\Project\CapStone\ETR_Record_BE\ETR.Infrastructure\ETR.Infrastructure.csproj"
api_path = r"D:\Project\CapStone\ETR_Record_BE\ETR.API\ETR.API.csproj"

app_content = """<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\ETR.Domain\\ETR.Domain.csproj" />
  </ItemGroup>

</Project>"""

infra_content = """<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\\ETR.Domain\\ETR.Domain.csproj" />
    <ProjectReference Include="..\\ETR.Application\\ETR.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
  </ItemGroup>

</Project>"""

api_content = """<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.9" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\\ETR.Application\\ETR.Application.csproj" />
    <ProjectReference Include="..\\ETR.Infrastructure\\ETR.Infrastructure.csproj" />
  </ItemGroup>

</Project>"""

with open(app_path, "w", encoding="utf-8") as f:
    f.write(app_content)

with open(infra_path, "w", encoding="utf-8") as f:
    f.write(infra_content)

with open(api_path, "w", encoding="utf-8") as f:
    f.write(api_content)

print("Successfully configured all project files.")
