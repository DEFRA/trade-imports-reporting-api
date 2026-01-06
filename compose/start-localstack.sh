#!/bin/bash

export AWS_ENDPOINT_URL=http://localhost:4566
export AWS_REGION=eu-west-2
export AWS_DEFAULT_REGION=eu-west-2
export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test

aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name trade_imports_data_upserted_reporting_api

aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name trade_imports_data_upserted_reporting_api-deadletter

aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name trade_imports_activity_reporting_api

aws --endpoint-url=http://localhost:4566 sqs create-queue \
    --queue-name trade_imports_activity_reporting_api-deadletter

aws --endpoint-url=http://localhost:4566 sqs set-queue-attributes --queue-url "http://localhost:4566/000000000000/trade_imports_data_upserted_reporting_api" --attributes '{"RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-west-2:000000000000:trade_imports_data_upserted_reporting_api-deadletter\",\"maxReceiveCount\":\"1\"}"}'
aws --endpoint-url=http://localhost:4566 sqs set-queue-attributes --queue-url "http://localhost:4566/000000000000/trade_imports_activity_reporting_api" --attributes '{"RedrivePolicy": "{\"deadLetterTargetArn\":\"arn:aws:sqs:eu-west-2:000000000000:trade_imports_activity_reporting_api-deadletter\",\"maxReceiveCount\":\"1\"}"}'

function is_ready() {
    aws --endpoint-url=http://localhost:4566 sqs get-queue-url --queue-name trade_imports_data_upserted_reporting_api || return 1
    aws --endpoint-url=http://localhost:4566 sqs get-queue-url --queue-name trade_imports_data_upserted_reporting_api-deadletter || return 1
    aws --endpoint-url=http://localhost:4566 sqs get-queue-url --queue-name trade_imports_activity_reporting_api || return 1
    aws --endpoint-url=http://localhost:4566 sqs get-queue-url --queue-name trade_imports_activity_reporting_api-deadletter || return 1
    return 0
}

while ! is_ready; do
    echo "Waiting until ready"
    sleep 1
done

touch /tmp/ready
