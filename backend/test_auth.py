import sqlite3
import hashlib

def hash_password(password):
    return hashlib.sha256(password.encode('utf-8')).hexdigest()

# Connect to database
conn = sqlite3.connect('Data/edusync.db')
cursor = conn.cursor()

print("=== Testing Authentication for Student ID 3 ===")
cursor.execute("SELECT MatricNumber, PasswordHash FROM Students WHERE Id = 3")
student = cursor.fetchone()
matric = student[0]
stored_hash = student[1]

print(f"Matric Number: {matric}")
print(f"Stored Hash: {stored_hash}")

# Try different passwords
test_passwords = ["test123", "4321", "password", "Test123", "TEST123"]
for pwd in test_passwords:
    hashed = hash_password(pwd)
    match = (hashed.lower() == stored_hash.lower())
    print(f"\nPassword '{pwd}': {hashed[:40]}...")
    print(f"  Match: {match}")

conn.close()
