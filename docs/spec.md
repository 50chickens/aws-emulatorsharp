# AWS emulator specification

## 1 Product

The emulator answers the AWS API calls a test needs to launch a container-backed instance, deliver
installer media to it, and ask whether the product inside it installed. It answers those calls on
the local machine, so no AWS account is part of the test.

## 2 Surfaces

The host reads `config/host.json`, `config/ec2.json`, `config/s3.json`, `config/secrets.json`,
`config/identity.json`, `config/containers.json`, `config/images.json`, `config/auth.json` and
`config/certificates.json` at start-up. Each surface is registered by its
module extension under `src/AwsEmulatorSharp.Emulator`.

| Surface | Request form | Configuration |
| --- | --- | --- |
| EC2 | query API over the wire module | `config/ec2.json`, `config/wire.json` |
| S3 | REST paths and query parameters | `config/s3.json` |
| Secrets Manager | JSON target | `config/secrets.json` |
| Identity | query API | `config/identity.json` |
| Containers | docker command line | `config/containers.json` |
| Certificates | `CertificateManager` JSON requests answered by `src/AwsEmulatorSharp.Emulator/Certificates`: `RequestCertificate`, `DescribeCertificate`, `GetCertificate`, `ListCertificates`, `DeleteCertificate`. `RequestCertificate` writes `certificate.pem`, `chain.pem`, `private-key.pem` and `certificate.pfx` into a folder named for the process that answers, under the `outputFolder` that `config/certificates.json` names | `config/certificates.json` |

The install plan, the installer media and the installer scripts are not part of this host: the SQL Server and report server install work lives in
`C:\git\internal\devops\vs_buildtools_docker\sql`.

## 3 Container-backed instances

A `RunInstances` request resolves the requested AMI id through `config/images.json`, starts one
docker container from the mapped image, labels it with the AMI id and instance type, and records
the container address as the instance's private IP address. When the request carries user data,
the emulator decodes it, splits it into stages at each reboot request, and runs each stage inside
the container with `powershell.exe -NoProfile -ExecutionPolicy Bypass -Command`. The standard
output of every stage is appended to the instance's console output.

The instance environment is written into the container as machine environment variables before the
user data stage runs, so a script inside the container reads the object store endpoint and the
credential it needs to download a payload without carrying its own copy.

