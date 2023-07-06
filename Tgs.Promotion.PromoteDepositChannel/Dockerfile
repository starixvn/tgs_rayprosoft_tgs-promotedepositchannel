#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.PromoteDepositChannel/Tgs.Promotion.PromoteDepositChannel.csproj", "Tgs.Promotion.PromoteDepositChannel/"]
COPY ["PromoteDepositChannel.Core/PromoteDepositChannel.Core.csproj", "PromoteDepositChannel.Core/"]
COPY ["PromoteDepositChannel.Repositories/PromoteDepositChannel.Repositories.csproj", "PromoteDepositChannel.Repositories/"]
RUN dotnet restore "Tgs.Promotion.PromoteDepositChannel/Tgs.Promotion.PromoteDepositChannel.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.PromoteDepositChannel"
RUN dotnet build "Tgs.Promotion.PromoteDepositChannel.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Promotion.PromoteDepositChannel.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.PromoteDepositChannel.dll"]