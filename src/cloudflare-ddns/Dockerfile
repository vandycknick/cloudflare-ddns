FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.16 as builder

ARG MINVERVERSIONOVERRIDE

WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:6.0.8-alpine3.16

WORKDIR /app

RUN apk add --no-cache bind-tools

COPY --from=builder /app/out .

USER 65534

ENTRYPOINT ["dotnet", "cloudflare-ddns.dll"]
