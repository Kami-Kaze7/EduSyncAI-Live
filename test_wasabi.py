import boto3

s3 = boto3.client('s3',
    endpoint_url='https://s3.eu-west-1.wasabisys.com',
    aws_access_key_id='XMU6S90EPC23KXQPHN03',
    aws_secret_access_key='wIDrhjtzOGH02TwrAqCwO6aEmy8yLd09gOhgPpin',
    region_name='eu-west-1'
)

try:
    buckets = s3.list_buckets()
    print("Credentials verified! Existing buckets:")
    for b in buckets['Buckets']:
        print(f"  - {b['Name']}")
except Exception as e:
    print(f"Error: {e}")
    # Try other regions
    for region in ['us-east-1', 'us-east-2', 'us-west-1', 'eu-central-1', 'eu-central-2', 'ap-northeast-1']:
        try:
            endpoint = f'https://s3.{region}.wasabisys.com' if region != 'us-east-1' else 'https://s3.wasabisys.com'
            s3_alt = boto3.client('s3',
                endpoint_url=endpoint,
                aws_access_key_id='XMU6S90EPC23KXQPHN03',
                aws_secret_access_key='wIDrhjtzOGH02TwrAqCwO6aEmy8yLd09gOhgPpin',
                region_name=region
            )
            buckets = s3_alt.list_buckets()
            print(f"SUCCESS with region: {region} (endpoint: {endpoint})")
            for b in buckets['Buckets']:
                print(f"  - {b['Name']}")
            break
        except Exception as e2:
            print(f"  Region {region}: {e2}")
