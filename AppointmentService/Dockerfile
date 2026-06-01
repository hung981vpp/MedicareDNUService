# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["AppointmentService.csproj", "./"]
RUN dotnet restore "AppointmentService.csproj"

# Copy the remaining source code and build the app
COPY . .
RUN dotnet build "AppointmentService.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "AppointmentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the ASP.NET runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose the port the app runs on (Render assigns the PORT environment variable)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "AppointmentService.dll"]
