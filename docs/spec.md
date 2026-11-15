# AWS emulator specification

## 1 Product

The emulator answers the AWS API calls a test needs to launch a container-backed instance, deliver
installer media to it, and ask whether the product inside it installed. It answers those calls on
the local machine, so no AWS account is part of the test.

## 2 Surfaces

The host reads `config/host.json`, `config/ec2.json`, `config/s3.json`, `config/secrets.json`,
`config/identity.json`, `config/containers.json`, `config/images.json`, `config/auth.json` and
`config/install.json` at start-up. Each surface is registered by its module extension under
`src/AwsEmulatorSharp.Emulator`.

| Surface | Request form | Configuration |
| --- | --- | --- |
| EC2 | query API over the wire module | `config/ec2.json`, `config/wire.json` |
| S3 | REST paths and query parameters | `config/s3.json` |
| Secrets Manager | JSON target | `config/secrets.json` |
| Identity | query API | `config/identity.json` |
| Containers | docker command line | `config/containers.json` |
| Install plan | generated per instance | `config/install.json` |

## 3 Container-backed instances

A `RunInstances` request resolves the requested AMI id through `config/images.json`, starts one
docker container from the mapped image, labels it with the AMI id and instance type, and records
the container address as the instance's private IP address. When the request carries user data,
the emulator decodes it, splits it into stages at each reboot request, and runs each stage inside
the container with `powershell.exe -NoProfile -ExecutionPolicy Bypass -Command`. The standard
output of every stage is appended to the instance's console output.

The instance environment is written into the container as machine environment variables before the
user data stage runs, so a script inside the container reads the S3 endpoint and credential it
needs to download media without carrying its own copy.

## 4 Install plan

`InstallPlanGenerator` builds one plan per instance from `config/install.json` and the objects the
media bucket holds.

- The plan carries two steps in this order: `database-engine`, then `report-server`.
- Each step names the newest object under the media key prefix configured for it, a presigned URL
  that addresses that object from the container endpoint, and the command line the container runs.
- The report server step carries the report server instance name and the report server database
  name that the configuration step consumes.
- The plan names the report server URL and the portal URL the running container answers on.

## 5 Pipeline

`Pipeline` runs these steps in this order: `create-bucket`, `upload-media`, `write-secrets`,
`launch`, `await-running`, `await-user-data`, `assert-install`, `terminate`.

## 6 Container install and configuration

### 6.1 Media folder

The container holds the installer media in one folder. `config/install.json` records that folder as
`mediaFolderPath`, and every settings file under `installer` repeats it.

| Folder | Media | File |
| --- | --- | --- |
| `installer\sql-server` | SQL Server 2025 setup, extracted from `SQLServer2025-x64-ENU-EntDev.iso` | `setup.exe` |
| `installer\report-server` | Power BI Report Server bundle | `PowerBIReportServer.exe` |

### 6.2 SQL Server 2025 install

`installer\sql-server\install.ps1` runs the setup program with this switch string:

```
setup.exe /Q /ACTION=Install /FEATURES=SQLENGINE,FULLTEXT,IS,Conn,SDK /IACCEPTSQLSERVERLICENSETERMS
```

The feature set is `SQLENGINE,FULLTEXT,IS,Conn,SDK`. The setup program installs the default
instance of the database engine, so the switch string carries no instance name. The script throws
when the setup program exits with a non-zero exit code.

### 6.3 Report server install

`installer\report-server\install.ps1` runs the report server bundle with this switch string:

```
PowerBIReportServer.exe /quiet /Edition=DEV /IAcceptLicenseTerms=True
```

The bundle installs the fixed instance `PBIRS` with the `DEV` edition, the free edition, so the
switch string carries no instance name. The bundle rejects `/ACTION=INSTALL` and `/INSTANCENAME`:
a command that carries either switch ends with a non-zero exit code. The script throws when the
bundle exits with a non-zero exit code.

### 6.4 Report server configuration

`installer\report-server\configure.ps1` reads the WMI configuration setting class
`MSReportServer_ConfigurationSetting` from the namespace
`root\Microsoft\SqlServer\ReportServer\RS_PBIRS\V15\Admin`, then calls these methods in this order:

| Order | WMI method | Purpose |
| --- | --- | --- |
| 1 | `GenerateDatabaseCreationScript` | generate the script that creates the report server database |
| 2 | `SetDatabaseConnection` | point the instance at the database engine |
| 3 | `SetVirtualDirectory` | name the report server virtual directory |
| 4 | `InitializeReportServer` | initialize the instance with the configuration it now holds |
| 5 | `ReserveURL` | reserve the URL the instance answers on |

`SetDatabaseConnection` takes `CredentialsType` 1: `CredentialsType` 0 answers HRESULT `0x8004022C`.
The applications are `ReportServerWebService` and `ReportServerWebApp`. `ReserveURL` takes the
base URL `http://+:80`, so the instance answers on that one URL, and it runs after
`SetVirtualDirectory`.

Every method returns an HRESULT as its `ReturnValue`. The script throws when a call returns a
non-zero HRESULT, so a call after a failed one never runs.

### 6.5 Settings files

Each script takes no parameter. Every script reads `settings.json` from its own folder: a folder
that holds two scripts holds one settings file that both read.

| Settings file | Keys |
| --- | --- |
| `installer\sql-server\settings.json` | `mediaFolderPath`, `installerFileName`, `features` |
| `installer\report-server\settings.json` | `mediaFolderPath`, `installerFileName`, `instanceName`, `edition`, `databaseServerName`, `databaseName`, `databaseCredentialsType`, `wmiNamespace`, `baseUrl`, `applications` |

### 6.6 Installed instance check

A configured instance answers HTTP 401 on `http://localhost/Reports` and on
`http://localhost/ReportServer`, so that status proves the portal and the report server answer on
the URLs `ReserveURL` reserved. `sqlcmd` needs `-C` to connect to the instance: the instance
certificate is self-signed, and sqlcmd without `-C` fails the connection.
