# Use the official .NET 8 SDK as a build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Set the working directory inside the container
WORKDIR /app

# Copy the .csproj file and restore any dependencies
COPY *.csproj ./
COPY ./nuget.config /nuget.config
RUN dotnet restore

# Copy the rest of the application code and build the application
COPY . ./
RUN dotnet publish -c Release -o out

# Use the official .NET 8 runtime image as a base for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Set the working directory inside the containe	r
WORKDIR /app

# Copy the built application from the build environment
COPY --from=build-env /app/out .

# Define the entry point for the application
ENTRYPOINT ["dotnet", "Console.Advanced.dll"]
