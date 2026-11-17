# Milestones

## 1 Purpose

The emulator must answer the AWS API calls the evaluation suite of `C:\git\external\aws\aws-emulator-evaluation-suite` exercises for the surfaces a deployment pipeline uses. A milestone is one surface, or one slice of a surface, delivered through Cursus as a requirement document in `cursus\data\requirements`.

SQL Server, SSIS and SSRS are outside this sequence and outside this repository. The installer media, the install and configuration scripts, the install plan generator and the step pipeline they drove live in `C:\git\internal\devops\vs_buildtools_docker\sql`, together with the requirement documents that were waiting on them.

## 2 Instrument

The evaluation suite is the acceptance instrument. A fidelity test is one `behavioral/<service>/check_<name>.py` program that drives a workflow with boto3 and asserts the resulting state and the error code. Each row of `manifest.jsonl` names the AWS operations that test calls, so the tests a surface claims are the rows whose operations the surface answers.

Run the suite against a started host with the runner under `src/fidelity_tests_runner`. The runner requires a loopback endpoint and a POSIX host, so it runs in the Linux distribution beside a loopback forwarder to the host port, and the host listens on every interface.

`config/evaluation.json` is the list of operations a surface claims. A surface adds its operations to that file in the same change that answers them, and the checks a milestone is measured by are the manifest rows whose operations all appear there. A check outside that set fails on an operation the surface does not claim and is not a milestone's evidence.

## 3 Verified state

The first run of the selection covered thirty checks: twelve for ACM, seventeen for EC2 and one cross-service test whose required tool was absent. None passed, five failed an assertion and twenty-five returned an error the emulator raised.

| Finding | Evidence | Surface |
| --- | --- | --- |
| Every certificate request answers 500 | `System.InvalidOperationException: Incorrect Content-Type: application/x-amz-json-1.1` in the host log, raised for `acm.op-contract-listCertificates` and eleven further ACM checks | ACM |
| An image carries no `CreationDate` | `KeyError: 'CreationDate'` at `ami_id = sorted(images, key=lambda i: i["CreationDate"], reverse=True)[0]["ImageId"]` in `ec2.op-contract-run-instances`, and in the five lifecycle checks after it | EC2 |
| An image carries no `OwnerId` | `KeyError: 'OwnerId'` in `ec2.op-contract-describe-images` | EC2 |
| A region carries no `OptInStatus` | `KeyError: 'OptInStatus'` in `ec2.op-contract-describe-regions` run with `--region us-east-1` | EC2 |
| An availability zone carries no `ZoneId` | `missing expected field 'ZoneId' on AvailabilityZone` in `ec2.op-contract-describeAvailabilityZones` | EC2 |
| An instance type carries no `BurstablePerformanceSupported` | `KeyError: 'BurstablePerformanceSupported'` in `ec2.op-contract-describeInstanceTypes` | EC2 |
| An unknown instance id is answered with console output | `expected a ClientError, but the call succeeded` in `ec2.get-console-output-invalid-instance-rejected-dup` | EC2 |
| An unknown security group name is answered with an empty list | `expected InvalidGroup.NotFound, got None` in `ec2.security-group-name-notfound-error` | EC2 |

The remaining errors name an operation no surface claims: `CreateVpc` for three checks, `CreateCapacityReservationFleet` for one, and the `aws` command-line tool for one. Those are the milestone sequence below, not defects.

## 4 Sequence

Each entry is one requirement document. A slice names the operations it adds, the error shapes those operations answer, and the fixture that proves them.

| Order | Milestone | Scope | State |
| --- | --- | --- | --- |
| 1 | EC2 response shapes | `CreationDate`, `OwnerId`, `OptInStatus`, `ZoneId` and `BurstablePerformanceSupported` on the describe responses, and an unknown instance id and security group name answered with the error AWS returns | delivered as m25 and m26 |
| 2 | ACM request form | The certificate surface reads the JSON body a boto3 client sends, and answers the error shapes `RequestCertificate` and `DescribeCertificate` define | delivered as m27 |
| 3 | IAM minimum | Users, roles, policies, `CreateRole`, `AttachRolePolicy`, `PassRole`, `GetRole`, `ListRoles`, `CreateInstanceProfile`, and the credential that names a role; the minimum an instance profile and a CloudFormation template need | next |
| 4 | VPC minimum | `CreateVpc`, `CreateSubnet`, `CreateSecurityGroup`, `AuthorizeSecurityGroupIngress`, `CreateInternetGateway`, `AttachInternetGateway`, `DescribeVpcs`; the network a load balancer and a launch template reference | not started |
| 5 | Auto Scaling | `CreateLaunchTemplate`, `CreateAutoScalingGroup`, `DescribeAutoScalingGroups`, `SetDesiredCapacity`, `TerminateInstanceInAutoScalingGroup`; each instance a container, so the group's capacity is containers | not started |
| 6 | Application Load Balancer | `CreateLoadBalancer`, `CreateTargetGroup`, `CreateListener`, `RegisterTargets`, `DescribeTargetHealth`; the target decides whether the container port answers | not started |
| 7 | CloudFormation | `CreateStack`, `DescribeStacks`, `DeleteStack` over a template that uses the resources above, with `ValidateTemplate` | not started |
| 8 | S3 completion | `CreateBucket`, `DeleteBucket`, `ListBuckets`, `CopyObject`, `ListObjectsV2`, `AbortMultipartUpload`, and the error shapes a bucket and an object define | not started |
| 9 | Secrets Manager completion | `CreateSecret`, `UpdateSecret`, `DeleteSecret`, `DescribeSecret`, `ListSecrets`, `GetSecretValue` with a version stage | not started |

Milestones 1 and 2 restore the surfaces already claimed. Milestones 3 to 7 follow one dependency: IAM, then the network, then the load balancer and the group whose instances it targets, then the template that declares all of them.

## 5 Stretch

EKS is not in this sequence. A cluster that runs real Kubernetes needs the docker engine in Linux mode on this machine, which no other milestone switches, and the decision to switch is separate from the surfaces above.
