import sqlite3
import hashlib
from datetime import datetime

def hash_password(password):
    return hashlib.sha256(password.encode('utf-8')).hexdigest()

# Connect to database
conn = sqlite3.connect('Data/edusync.db')
cursor = conn.cursor()

# Delete existing test student if exists
cursor.execute("DELETE FROM Students WHERE MatricNumber = 'TEST001'")

# Create test student with known password
test_matric = "TEST001"
test_name = "Test Student"
test_email = "test@student.com"
test_password = "password123"
test_pin = "9999"

password_hash = hash_password(test_password)

cursor.execute("""
    INSERT INTO Students (MatricNumber, FullName, Email, WindowsUsername, PasswordHash, PIN, IsActive, CreatedAt)
    VALUES (?, ?, ?, ?, ?, ?, 1, ?)
""", (test_matric, test_name, test_email, None, password_hash, test_pin, datetime.now().strftime('%Y-%m-%d %H:%M:%S')))

conn.commit()

print("✅ Test student created successfully!")
print(f"   Matric Number: {test_matric}")
print(f"   Password: {test_password}")
print(f"   PIN: {test_pin}")
print(f"   Password Hash: {password_hash}")
print("\n📌 Try logging in with:")
print(f"   Username: {test_matric}")
print(f"   Password: {test_password}")
print(f"   OR PIN: {test_pin}")

# Verify it was created
cursor.execute("SELECT MatricNumber, FullName, PasswordHash, PIN FROM Students WHERE MatricNumber = ?", (test_matric,))
student = cursor.fetchone()
print(f"\n✓ Verified in database:")
print(f"   Matric: {student[0]}")
print(f"   Name: {student[1]}")
print(f"   Hash matches: {student[2] == password_hash}")
print(f"   PIN: {student[3]}")

conn.close()
