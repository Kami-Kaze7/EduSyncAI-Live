import sqlite3

# Connect to database
conn = sqlite3.connect('Data/edusync.db')
cursor = conn.cursor()

# Update students with Windows usernames (simulating fingerprint enrollment)
# In production, this would happen during course enrollment with actual fingerprint scan
students_mapping = [
    ('S001', 'Hp'),  # Map to your current Windows username
    ('S002', 'Student2'),
    ('S003', 'Student3')
]

for matric, windows_user in students_mapping:
    cursor.execute("""
        UPDATE Students 
        SET WindowsUsername = ? 
        WHERE MatricNumber = ?
    """, (windows_user, matric))

conn.commit()
print("✅ Student fingerprints registered!")
print(f"   S001 (John Doe) → Windows user: Hp")
print(f"   S002 (Jane Smith) → Windows user: Student2")
print(f"   S003 (Bob Johnson) → Windows user: Student3")
print("\n📌 When you scan your fingerprint, you'll be auto-identified as John Doe (S001)")

conn.close()
