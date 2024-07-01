FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine3.20 as builder

ARG MINVERVERSIONOVERRIDE

WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0.3-alpine3.19

WORKDIR /app

RUN apk add --no-cache bind-tools

COPY --from=builder /app/out .

USER 64198

ENTRYPOINT ["dotnet", "cloudflare-ddns.dll"]
