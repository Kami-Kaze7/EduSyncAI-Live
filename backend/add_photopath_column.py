import sqlite3

# Connect to database
conn = sqlite3.connect('../Data/edusync.db')
cursor = conn.cursor()

# Add PhotoPath column to Students table
try:
    cursor.execute("ALTER TABLE Students ADD COLUMN PhotoPath TEXT")
    conn.commit()
    print("✅ Successfully added PhotoPath column to Students table!")
except sqlite3.OperationalError as e:
    if "duplicate column name" in str(e):
        print("ℹ️  PhotoPath column already exists")
    else:
        print(f"❌ Error: {e}")

# Verify the column was added
cursor.execute("PRAGMA table_info(Students)")
columns = cursor.fetchall()
print("\nCurrent Students table columns:")
for col in columns:
    print(f"  - {col[1]} ({col[2]})")

conn.close()
print("\n✅ Database updated successfully!")
