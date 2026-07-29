QUEUES=(
	"trade_imports_data_upserted_reporting_api"
	"trade_imports_data_upserted_reporting_api-deadletter"
	"trade_imports_btms_activity_reporting_api"
	"trade_imports_btms_activity_reporting_api-deadletter"
)

function is_ready() {
  for queue in "${QUEUES[@]}"; do
    aws --endpoint-url="$AWS_ENDPOINT_URL" sqs get-queue-url --queue-name "${queue}" || return 1
  done

  return 0
}

while ! is_ready; do
  echo "Waiting until ready"
  sleep 1
done