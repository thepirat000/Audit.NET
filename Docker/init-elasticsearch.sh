#!/usr/bin/env bash
set -e

# Start Elasticsearch via the proper entrypoint
/usr/local/bin/docker-entrypoint.sh &  # Correct path per the official image
ES_PID=$!

# Wait for Elasticsearch to open port 9200
until curl -s http://localhost:9200 >/dev/null 2>&1; do
  echo "Waiting for Elasticsearch to listen on port 9200..."
  sleep 2
done
echo "Elasticsearch is now listening."

# Create index with one primary shard and no replicas (optional)
curl -X PUT "http://localhost:9200/auto_index" -H 'Content-Type: application/json' -d'
{
  "settings": {
    "number_of_shards": 1,
    "number_of_replicas": 0
  }
}
'

# Wait for Elasticsearch process to finish
wait $ES_PID
