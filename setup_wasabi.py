import boto3
import json

REGION = 'eu-west-1'
BUCKET_NAME = 'edusyncai-videos'
ENDPOINT = f'https://s3.{REGION}.wasabisys.com'

s3 = boto3.client('s3',
    endpoint_url=ENDPOINT,
    aws_access_key_id='XMU6S90EPC23KXQPHN03',
    aws_secret_access_key='wIDrhjtzOGH02TwrAqCwO6aEmy8yLd09gOhgPpin',
    region_name=REGION
)

# 1. Create bucket
print(f"Creating bucket: {BUCKET_NAME} in {REGION}...")
try:
    s3.create_bucket(
        Bucket=BUCKET_NAME,
        CreateBucketConfiguration={'LocationConstraint': REGION}
    )
    print(f"Bucket '{BUCKET_NAME}' created successfully!")
except s3.exceptions.BucketAlreadyOwnedByYou:
    print(f"Bucket '{BUCKET_NAME}' already exists (owned by you).")
except Exception as e:
    print(f"Bucket creation error: {e}")

# 2. Set CORS for direct browser uploads
print("\nConfiguring CORS...")
cors_config = {
    'CORSRules': [
        {
            'AllowedHeaders': ['*'],
            'AllowedMethods': ['GET', 'PUT', 'POST', 'DELETE', 'HEAD'],
            'AllowedOrigins': ['*'],  # Allow all origins for now; tighten in production
            'ExposeHeaders': ['ETag', 'x-amz-request-id'],
            'MaxAgeSeconds': 3600
        }
    ]
}
try:
    s3.put_bucket_cors(Bucket=BUCKET_NAME, CORSConfiguration=cors_config)
    print("CORS configured successfully!")
except Exception as e:
    print(f"CORS error: {e}")

# 3. Verify by listing bucket contents
print(f"\nBucket URL: {ENDPOINT}/{BUCKET_NAME}")
print(f"Direct object URL pattern: {ENDPOINT}/{BUCKET_NAME}/{{key}}")

# 4. Test with a small text file upload
print("\nUploading test file...")
try:
    s3.put_object(
        Bucket=BUCKET_NAME,
        Key='test/hello.txt',
        Body=b'EduSyncAI Wasabi integration test - OK',
        ContentType='text/plain'
    )
    print("Test file uploaded successfully!")
    
    # Generate a pre-signed URL to verify access
    url = s3.generate_presigned_url(
        'get_object',
        Params={'Bucket': BUCKET_NAME, 'Key': 'test/hello.txt'},
        ExpiresIn=3600
    )
    print(f"Pre-signed URL (valid 1hr): {url[:100]}...")
    
    # Clean up test file
    s3.delete_object(Bucket=BUCKET_NAME, Key='test/hello.txt')
    print("Test file cleaned up.")
except Exception as e:
    print(f"Test upload error: {e}")

print("\n=== SETUP COMPLETE ===")
print(f"Bucket: {BUCKET_NAME}")
print(f"Region: {REGION}")
print(f"Endpoint: {ENDPOINT}")
