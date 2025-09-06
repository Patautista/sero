import subprocess
import os

def get_user_input():
    print("Select the operation you want to perform:")
    print("1 - Update database")
    print("2 - Add migration")
    
    operation = input("Enter the number of the operation (1 or 2): ").strip()
    if operation not in ['1', '2']:
        print("Invalid input. Please enter 1 or 2.")
        return None, None
    
    return operation

def run_command(operation):
    
    if operation == '1':
        command = "dotnet ef database update"
    elif operation == '2':
        migration_name = input("Enter the migration name: ").strip()
        if not migration_name:
            print("Migration name cannot be empty.")
            return
        command = f"dotnet ef migrations add {migration_name}"
    
    try:
        print(f"Running command: {command}")
        subprocess.run(command, check=True, shell=True)
        print("Command executed successfully.")
    except subprocess.CalledProcessError as e:
        print(f"An error occurred while executing the command: {e}")

def main():
    try:
        operation = get_user_input()
        if operation:
            run_command(operation)
    except KeyboardInterrupt:
        print("\nOperation cancelled by user. Exiting...")

if __name__ == "__main__":
    main()
    exit = input("Press any key...").strip()
