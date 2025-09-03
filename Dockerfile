# Stage 1: Build the 'slice_localize' binary
# Use Debian 12 as the base to match the final .NET runtime image, ensuring library compatibility.
FROM debian:12-slim AS binary-builder

# Install build dependencies: git, C++ compiler, make, and OpenCV development headers for Debian.
RUN apt-get update && apt-get install -y \
    git \
    build-essential \
    make \
    libopencv-dev \
    && rm -rf /var/lib/apt/lists/*

# Clone the repository containing the C++ code
WORKDIR /build
RUN git clone https://github.com/marek-konecny/slice-recognition-in-array-tomography.git

# Change the working directory to the 'bin' subdirectory where the source and Makefile are.
WORKDIR /build/slice-recognition-in-array-tomography/bin

# Compile the binary
# The result is at:
# /build/slice-recognition-in-array-tomography/bin/slice_localize
RUN make


# Stage 2: Build the Blazor .NET application (This stage is self-contained and needs no changes)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-builder

WORKDIR /src

# Copy project files and restore dependencies
COPY src/SliceRecognitionApp/SliceRecognitionApp.csproj SliceRecognitionApp/
RUN dotnet restore "SliceRecognitionApp/SliceRecognitionApp.csproj"

# Copy the rest of the app's source code and publish
COPY src/SliceRecognitionApp/. SliceRecognitionApp/
RUN dotnet publish "SliceRecognitionApp/SliceRecognitionApp.csproj" -c Release -o /app/publish


# Stage 3: Create the final, lean runtime image
# The aspnet:8.0 image is based on Debian 12 ("Bookworm")
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Install only the required runtime libraries for the C++ binary.
# Use the package names for Debian 12, which are consistent with Stage 1.
RUN apt-get update && apt-get install -y \
    libopencv-core406 \
    libopencv-imgproc406 \
    libopencv-imgcodecs406 \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy the published app from the 'dotnet-builder' stage
COPY --from=dotnet-builder /app/publish .

# Copy the compiled native binary from its subdirectory in the 'binary-builder' stage to /usr/local/bin.
COPY --from=binary-builder /build/slice-recognition-in-array-tomography/bin/slice_localize /usr/local/bin/

# Ensure the binary is executable
RUN chmod +x /usr/local/bin/slice_localize

# Expose the port the app will run on
EXPOSE 8080

# Define the entry point for the container to run the app
ENTRYPOINT ["dotnet", "SliceRecognitionApp.dll"]