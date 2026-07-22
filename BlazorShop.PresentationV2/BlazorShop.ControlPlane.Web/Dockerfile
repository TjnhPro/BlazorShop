ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.202-noble
ARG NGINX_IMAGE=nginx:1.27-alpine

FROM ${DOTNET_SDK_IMAGE} AS build
WORKDIR /src

RUN apt-get update \
    && apt-get install -y --no-install-recommends nodejs npm \
    && rm -rf /var/lib/apt/lists/*

COPY BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/
COPY BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/BlazorShop.Web.SharedV2.csproj BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/
COPY BlazorShop.Application/BlazorShop.Application.csproj BlazorShop.Application/
COPY BlazorShop.Domain/BlazorShop.Domain.csproj BlazorShop.Domain/

RUN dotnet restore BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj

COPY BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/package.json BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/package.json
COPY BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/package-lock.json BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/package-lock.json

COPY BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/ BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/
COPY BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/ BlazorShop.PresentationV2/BlazorShop.Web.SharedV2/
COPY BlazorShop.Application/ BlazorShop.Application/
COPY BlazorShop.Domain/ BlazorShop.Domain/

RUN dotnet publish BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/BlazorShop.ControlPlane.Web.csproj \
    --configuration Release \
    --output /app/publish \
    /p:UseAppHost=false

FROM ${NGINX_IMAGE} AS final

COPY BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/nginx.conf /etc/nginx/conf.d/default.conf
COPY BlazorShop.PresentationV2/BlazorShop.ControlPlane.Web/docker-entrypoint.d/10-controlplane-config.sh /docker-entrypoint.d/10-controlplane-config.sh
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

RUN chmod +x /docker-entrypoint.d/10-controlplane-config.sh

EXPOSE 80
