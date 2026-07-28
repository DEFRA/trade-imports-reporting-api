#!/bin/bash

set -e

QUEUES=(
	"trade_imports_data_upserted_reporting_api"
	"trade_imports_data_upserted_reporting_api-deadletter"
	"trade_imports_btms_activity_reporting_api"
	"trade_imports_btms_activity_reporting_api-deadletter"
)

echo "Creating SNS queues..." 

for queue in "${QUEUES[@]}"; do

  queue_arn=$(aws --endpoint-url="$AWS_ENDPOINT_URL" sqs create-queue --queue-name "$queue")

  echo "Queue ARN: $queue_arn" # NOSONAR
done

aws --endpoint-url="$AWS_ENDPOINT_URL" sqs set-queue-attributes --queue-url "$AWS_ENDPOINT_URL/000000000000/trade_imports_data_upserted_reporting_api" --attributes '{"RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-west-2:000000000000:trade_imports_data_upserted_reporting_api-deadletter\",\"maxReceiveCount\":\"1\"}"}'
aws --endpoint-url="$AWS_ENDPOINT_URL" sqs set-queue-attributes --queue-url "$AWS_ENDPOINT_URL/000000000000/trade_imports_btms_activity_reporting_api" --attributes '{"RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-west-2:000000000000:trade_imports_btms_activity_reporting_api-deadletter\",\"maxReceiveCount\":\"1\"}"}'

for queue in "${QUEUES[@]}"; do

  queue_properties=$(aws --endpoint-url="$AWS_ENDPOINT_URL" sqs get-queue-attributes --queue-url "$AWS_ENDPOINT_URL/000000000000/$queue" --attribute-names All)

  echo "Queue $queue Properties: $queue_properties" # NOSONAR
done