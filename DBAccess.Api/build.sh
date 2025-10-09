#!/bin/bash

# DBAccess.Api Docker Build Script
# This script builds the Docker image for the microservice

set -e  # Exit on error

echo "🚀 Building DBAccess.Api microservice..."

# Set variables
IMAGE_NAME="dbaccess-api"
TAG="${1:-latest}"
FULL_IMAGE_NAME="${IMAGE_NAME}:${TAG}"

echo "📦 Image: ${FULL_IMAGE_NAME}"

# Build the Docker image
echo "🔨 Building Docker image..."
docker build -t "${FULL_IMAGE_NAME}" .

echo "✅ Build completed successfully!"
echo "📋 Image details:"
docker images "${IMAGE_NAME}"

echo ""
echo "🎯 Next steps:"
echo "  1. Test locally: docker run --rm -p 8080:8080 ${FULL_IMAGE_NAME}"
echo "  2. Use compose: docker-compose up -d"
echo "  3. Deploy: ./deploy.sh"
echo ""