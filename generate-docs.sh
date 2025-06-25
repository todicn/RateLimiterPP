#!/bin/bash

# Set up environment
export PATH="$PATH:/home/ubuntu/.dotnet:/home/ubuntu/.dotnet/tools"
export DOTNET_ROOT="/home/ubuntu/.dotnet"

echo "=== RateLimiter Documentation Generation ==="
echo "Working directory: $(pwd)"
echo "PATH: $PATH"
echo "DOTNET_ROOT: $DOTNET_ROOT"

# Build the project first to ensure XML docs are generated
echo "Building the project..."
/home/ubuntu/.dotnet/dotnet build RateLimiter/RateLimiter.csproj

# Check if XML file exists
if [ -f "RateLimiter/bin/Debug/net8.0/RateLimiter.xml" ]; then
    echo "✓ XML documentation file found"
else
    echo "✗ XML documentation file not found"
    exit 1
fi

# Try to run DocFX directly
echo "Attempting to run DocFX..."
if command -v docfx &> /dev/null; then
    echo "Running DocFX metadata generation..."
    docfx metadata docfx.json
    
    echo "Running DocFX build..."
    docfx build docfx.json
    
    echo "Documentation generated successfully!"
    echo "You can find the generated documentation in the '_site' folder"
    echo "To serve locally, run: docfx serve _site"
else
    echo "DocFX command not found. Trying with full path..."
    /home/ubuntu/.dotnet/tools/docfx metadata docfx.json
    /home/ubuntu/.dotnet/tools/docfx build docfx.json
fi