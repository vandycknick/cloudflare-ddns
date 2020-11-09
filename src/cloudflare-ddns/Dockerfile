FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine as builder

ARG MINVERVERSIONOVERRIDE

WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine

WORKDIR /app

COPY --from=builder /app/out .

ENTRYPOINT ["dotnet", "cloudflare-ddns.dll"]
