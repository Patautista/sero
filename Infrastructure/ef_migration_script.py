import subprocess
import os

CONN_STRINGS_FILE = os.path.expanduser("~/.ef_migration/connection_strings.txt")

def ensure_connection_strings_file_exists():
    os.makedirs(os.path.dirname(CONN_STRINGS_FILE), exist_ok=True)
    if not os.path.isfile(CONN_STRINGS_FILE):
        with open(CONN_STRINGS_FILE, 'w') as f:
            f.write('')

def load_connection_strings():
    ensure_connection_strings_file_exists()
    with open(CONN_STRINGS_FILE, 'r') as f:
        return [line.strip() for line in f.readlines()]

def save_connection_string(connection_string):
    connection_strings = load_connection_strings()
    if connection_string not in connection_strings:
        connection_strings.append(connection_string)
        with open(CONN_STRINGS_FILE, 'a') as f:
            f.write(connection_string + '\n')

def get_user_input():
    print("Select the operation you want to perform:")
    print("1 - Update database")
    print("2 - Add migration")
    
    operation = input("Enter the number of the operation (1 or 2): ").strip()
    if operation not in ['1', '2']:
        print("Invalid input. Please enter 1 or 2.")
        return None, None
    
    connection_strings = load_connection_strings()
    if connection_strings:
        print("Select a recently used connection string or enter a new one:")
        for i, conn in enumerate(connection_strings, 1):
            print(f"{i} - {conn}")
        print("N - Enter a new connection string")
        
        selection = input("Enter your choice: ").strip()
        if selection.lower() == 'n':
            connection_string = input("Enter the connection string: ").strip()
        elif selection.isdigit() and 1 <= int(selection) <= len(connection_strings):
            connection_string = connection_strings[int(selection) - 1]
        else:
            print("Invalid input.")
            return None, None
    else:
        connection_string = input("Enter the connection string: ").strip()
    
    if not connection_string:
        print("Connection string cannot be empty.")
        return None, None
    
    save_connection_string(connection_string)
    return operation, connection_string

def run_command(operation, connection_string):
    connection_string_escaped = connection_string.replace("'", "''")  # Escape single quotes in the connection string
    
    if operation == '1':
        command = f"dotnet ef database update -- --connection=\"{connection_string_escaped}\""
    elif operation == '2':
        migration_name = input("Enter the migration name: ").strip()
        if not migration_name:
            print("Migration name cannot be empty.")
            return
        command = f"dotnet ef migrations add {migration_name} -- --connection=\"{connection_string_escaped}\""
    
    try:
        print(f"Running command: {command}")
        subprocess.run(command, check=True, shell=True)
        print("Command executed successfully.")
    except subprocess.CalledProcessError as e:
        print(f"An error occurred while executing the command: {e}")

def main():
    try:
        operation, connection_string = get_user_input()
        if operation and connection_string:
            run_command(operation, connection_string)
    except KeyboardInterrupt:
        print("\nOperation cancelled by user. Exiting...")

if __name__ == "__main__":
    main()
    exit = input("Press any key...").strip()
