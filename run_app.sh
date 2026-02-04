#!/bin/bash
# Kill any existing process on 5048
kill $(lsof -t -i :5048) 2>/dev/null || true

# Build and Run
dotnet run --project MetalFlowSystemV2/MetalFlowSystemV2.csproj --urls "http://localhost:5048" > server.log 2>&1 &
PID=$!
echo $PID > server.pid
echo "Server started with PID $PID. Waiting for it to come online..."

# Wait for server
for i in {1..30}; do
    if curl -s http://localhost:5048 > /dev/null; then
        echo "Server is up!"
        exit 0
    fi
    sleep 2
done

echo "Server failed to start."
cat server.log
exit 1
