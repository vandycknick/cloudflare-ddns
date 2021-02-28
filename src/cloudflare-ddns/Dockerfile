FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine as builder

ARG MINVERVERSIONOVERRIDE

WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine

WORKDIR /app

COPY --from=builder /app/out .

USER 1000

ENTRYPOINT ["dotnet", "cloudflare-ddns.dll"]
