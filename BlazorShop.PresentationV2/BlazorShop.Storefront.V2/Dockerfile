ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.202-noble
ARG DOTNET_ASPNET_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.6-noble

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends nodejs npm \
    && rm -rf /var/lib/apt/lists/*

COPY BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj BlazorShop.PresentationV2/BlazorShop.Storefront.V2/
COPY BlazorShop.PresentationV2/BlazorShop.Storefront.Components/BlazorShop.Storefront.Components.csproj BlazorShop.PresentationV2/BlazorShop.Storefront.Components/
COPY BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/BlazorShop.Storefront.WASM.csproj BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/
COPY BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/BlazorShop.Web.SharedV2.csproj BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/
COPY BlazorShop.ServiceDefaults/BlazorShop.ServiceDefaults.csproj BlazorShop.ServiceDefaults/

RUN dotnet restore BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj

COPY BlazorShop.PresentationV2/BlazorShop.Storefront.V2/package.json BlazorShop.PresentationV2/BlazorShop.Storefront.V2/package.json
COPY BlazorShop.PresentationV2/BlazorShop.Storefront.V2/package-lock.json BlazorShop.PresentationV2/BlazorShop.Storefront.V2/package-lock.json

COPY BlazorShop.PresentationV2/BlazorShop.Storefront.V2/ BlazorShop.PresentationV2/BlazorShop.Storefront.V2/
COPY BlazorShop.PresentationV2/BlazorShop.Storefront.Components/ BlazorShop.PresentationV2/BlazorShop.Storefront.Components/
COPY BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/ BlazorShop.PresentationV2/BlazorShop.Storefront.WASM/
COPY BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/ BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/
COPY BlazorShop.ServiceDefaults/ BlazorShop.ServiceDefaults/

WORKDIR /src/BlazorShop.PresentationV2/BlazorShop.Storefront.V2

RUN rm -rf node_modules \
    && npm ci \
    && npm run tailwind:build

WORKDIR /src

RUN dotnet publish BlazorShop.PresentationV2/BlazorShop.Storefront.V2/BlazorShop.Storefront.V2.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM ${DOTNET_ASPNET_IMAGE} AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BlazorShop.Storefront.V2.dll"]
