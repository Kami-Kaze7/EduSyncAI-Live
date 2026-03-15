import sqlite3
import hashlib

# Connect to database
conn = sqlite3.connect('Data/edusync.db')
cursor = conn.cursor()

# Hash password function (matches C# SHA256)
def hash_password(password):
    return hashlib.sha256(password.encode()).hexdigest()

# Create a test lecturer
username = "lecturer1"
email = "lecturer@edusync.ai"
full_name = "Dr. John Smith"
password = "password123"  # Plain text password
pin = "1234"  # 4-digit PIN

password_hash = hash_password(password)

try:
    cursor.execute("""
        INSERT INTO Lecturers (Username, Email, FullName, PasswordHash, PIN, IsActive, CreatedAt)
        VALUES (?, ?, ?, ?, ?, 1, datetime('now'))
    """, (username, email, full_name, password_hash, pin))
    
    conn.commit()
    print("✅ Test lecturer created successfully!")
    print(f"   Username: {username}")
    print(f"   Password: {password}")
    print(f"   PIN: {pin}")
    print(f"   Email: {email}")
except sqlite3.IntegrityError:
    print("ℹ️  Lecturer already exists")

conn.close()
