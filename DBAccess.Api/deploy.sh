#!/bin/bash

# DBAccess.Api Deployment Script
# This script deploys the microservice using Docker Compose

set -e  # Exit on error

echo "🚀 Deploying DBAccess.Api microservice..."

# Load environment variables if .env file exists
if [ -f .env ]; then
    echo "📋 Loading environment variables from .env file..."
    export $(cat .env | grep -v '^#' | xargs)
fi

# Function to check if container is healthy
check_health() {
    local container_name="dbaccess-api"
    local max_attempts=30
    local attempt=1
    
    echo "🔍 Checking container health..."
    
    while [ $attempt -le $max_attempts ]; do
        if docker exec "$container_name" curl -f http://localhost:8080/api/dbaccess/ping?db=master > /dev/null 2>&1; then
            echo "✅ Container is healthy!"
            return 0
        fi
        
        echo "⏳ Attempt $attempt/$max_attempts - waiting for container..."
        sleep 2
        ((attempt++))
    done
    
    echo "❌ Container health check failed after $max_attempts attempts"
    return 1
}

# Deploy with Docker Compose
echo "📦 Starting containers..."
docker-compose up -d

echo "⏳ Waiting for services to start..."
sleep 5

# Check if deployment was successful
if check_health; then
    echo ""
    echo "🎉 Deployment successful!"
    echo "📋 Service information:"
    echo "  - API URL: http://localhost:8080"
    echo "  - Health: http://localhost:8080/api/dbaccess/ping?db=master"
    echo "  - Logs: docker-compose logs -f dbaccess-api"
    echo ""
    echo "🔧 Management commands:"
    echo "  - Stop: docker-compose down"
    echo "  - Restart: docker-compose restart"
    echo "  - Update: docker-compose pull && docker-compose up -d"
else
    echo ""
    echo "❌ Deployment may have issues. Check logs:"
    echo "  docker-compose logs dbaccess-api"
    exit 1
fi