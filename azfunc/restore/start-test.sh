# Restore blobs  into source container
curl -X POST -H "Content-Type: application/json" -d "@./test-data.json" http://<function host URL>/api/restore/blobs

# Check the status of asynchronous restore request
curl -v http://<function host URL>/api/restore/2020_21/87eb878a-72e4-443b-be25-544069def07d
