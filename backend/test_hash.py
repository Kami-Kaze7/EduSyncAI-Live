import sqlite3
import hashlib

def hash_password(password):
    return hashlib.sha256(password.encode('utf-8')).hexdigest()

# Test password hashing
test_password = "test123"
hashed = hash_password(test_password)
print(f"Password: {test_password}")
print(f"Hash: {hashed}")

# Check database
conn = sqlite3.connect('Data/edusync.db')
cursor = conn.cursor()

print("\n=== Checking Student ID 3 ===")
cursor.execute("SELECT MatricNumber, PasswordHash FROM Students WHERE Id = 3")
student = cursor.fetchone()
print(f"Matric: {student[0]}")
print(f"Stored Hash: {student[1]}")
print(f"Match: {student[1] == hashed}")

conn.close()
