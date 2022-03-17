FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine3.15 as builder

ARG MINVERVERSIONOVERRIDE

WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:6.0.3-alpine3.15

RUN addgroup --gid 10001 --system nonroot && \
    adduser  --uid 10000 --system --ingroup nonroot --home /home/nonroot nonroot

WORKDIR /app

RUN apk add --no-cache bind-tools

COPY --from=builder /app/out .

USER nonroot

ENTRYPOINT ["dotnet", "cloudflare-ddns.dll"]
