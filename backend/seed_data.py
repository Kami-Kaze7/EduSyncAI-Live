import sqlite3
from pathlib import Path

db_path = Path("../Data/edusync.db")
connection = sqlite3.connect(db_path)
cursor = connection.cursor()

# Add test students
students = [
    ("S001", "John Doe", "john.doe@example.com"),
    ("S002", "Jane Smith", "jane.smith@example.com"),
    ("S003", "Bob Johnson", "bob.johnson@example.com")
]

for matric, name, email in students:
    cursor.execute(
        "INSERT INTO Students (MatricNumber, FullName, Email) VALUES (?, ?, ?)",
        (matric, name, email)
    )

connection.commit()
connection.close()
print("✅ Test students added successfully!")
print("Students added:")
for matric, name, email in students:
    print(f"  - {name} ({matric}): {email}")
