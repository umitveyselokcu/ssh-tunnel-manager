# SSH Tunnel Manager

A lightweight WPF application for managing SSH tunnels with ease. This tool simplifies the process of configuring and maintaining SSH tunnels for accessing remote services.

## Features
- Manage multiple SSH tunnel configurations.
- Add, edit, or remove configurations.
- Import configurations using a predefined JSON format.
- Connect and disconnect SSH tunnels with a single click.
- Select a folder for storing PEM files.

## Usage

### 1. **Setting Up the PEM File Folder**
- In the application, select a folder where all PEM files will be stored. This ensures that the application can locate the necessary authentication files when managing SSH tunnels.

### 2. **Adding Configurations**
- Use the **Add** button to create a new SSH tunnel configuration manually.
- Fill in the required fields, including `Name`, `IpAddress`, `PemFileName`, `LocalPort`, `RemoteHost`, `RemotePort`, and `BrowserUrl`.

### 3. **Importing Configurations**
- Import multiple SSH tunnel configurations by uploading a JSON file.
- The JSON file must match the following format:

   ```json
   [
     {
       "Name": "",
       "IpAddress": "",
       "PemFileName": "",
       "LocalPort": 0,
       "RemoteHost": "",
       "RemotePort": 0,
       "BrowserUrl": ""
     }
   ]

### 4. **Establish Connection**

-   Select a configuration from the list.
-   Click Connect to establish the SSH tunnel.
-   If a BrowserUrl is specified, it will open automatically in your default browser once the connection is established.

### 5. **Disconnect**

-   Select an active connection.
-   Click Disconnect to safely close the SSH tunnel and release the local port.

### 6. **Edit Configurations**

-   Select an existing configuration and click Edit to update its details.
-   Save the changes after editing.

### 7. **Remove Configurations**

-   Select a configuration and click Remove to delete it permanently.