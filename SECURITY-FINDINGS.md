# IaC Security Scan Results

**Scan Date:** 2026-02-09  
**Repository:** githubabcs-devops/ado-advsec-devsecops  
**IaC Technologies:** Bicep, Kubernetes, Docker, Docker Compose

## Executive Summary

This security review identified **12 security findings** across Infrastructure as Code (IaC) configurations, with **1 CRITICAL**, **5 HIGH**, **4 MEDIUM**, and **2 LOW** severity issues. The findings span Azure Bicep templates, Kubernetes manifests, Dockerfiles, and Docker Compose configurations.

## Summary

| Category | Critical | High | Medium | Low | Total |
|----------|----------|------|--------|-----|-------|
| Network Security | 1 | 2 | 0 | 0 | 3 |
| Data Protection & Encryption | 0 | 0 | 1 | 0 | 1 |
| Logging & Monitoring | 0 | 0 | 2 | 0 | 2 |
| Identity & Access Management | 0 | 1 | 1 | 1 | 3 |
| Container & Workload Security | 0 | 2 | 0 | 1 | 3 |
| **Total** | **1** | **5** | **4** | **2** | **12** |

---

## Critical Findings

### [CRITICAL] NSG-001: SQL Server Publicly Accessible to Internet
- **File:** `infra/core/database/sqlserver/sqlserver.bicep`
- **Lines:** 23, 33-42
- **Resource:** `Microsoft.Sql/servers` (sqlServer)
- **Issue:** SQL Server allows public network access with firewall rules permitting nearly all IP addresses (0.0.0.1 to 255.255.255.254)
- **Impact:** 
  - Direct internet exposure of database server
  - Unauthorized access to sensitive application data
  - Potential data breach and data exfiltration
  - SQL injection attack surface
  - Credential brute-force attacks
- **Control Mapping:** 
  - CIS Azure 4.1.1 - Ensure that 'Auditing' is set to 'On'
  - CIS Azure 4.1.3 - Ensure no SQL Databases allow ingress from 0.0.0.0/0
  - NIST 800-53: SC-7 (Boundary Protection)
  - Azure Security Benchmark: NS-1, NS-2
  - PCI-DSS: 1.2, 1.3
- **Current Configuration:**
```bicep
properties: {
  publicNetworkAccess: 'Enabled'  // ❌ Public access enabled
}

resource firewall 'firewallRules' = {
  name: 'Azure Services'
  properties: {
    startIpAddress: '0.0.0.1'      // ❌ Allows nearly all IPs
    endIpAddress: '255.255.255.254'
  }
}
```

---

## High Severity Findings

### [HIGH] NSG-002: Key Vault Missing Network Restrictions
- **File:** `infra/core/security/keyvault.bicep`
- **Lines:** 7-22
- **Resource:** `Microsoft.KeyVault/vaults`
- **Issue:** Key Vault has no network ACLs configured, allowing access from any network
- **Impact:**
  - Secrets, keys, and certificates accessible from any network
  - No network-level defense against unauthorized access
  - Increased attack surface for credential theft
- **Control Mapping:**
  - CIS Azure 8.4 - Ensure the key vault is recoverable
  - CIS Azure 8.5 - Enable role-based access control for Azure Key Vault
  - NIST 800-53: AC-3, SC-7
  - Azure Security Benchmark: NS-2, IM-1
  - PCI-DSS: 8.2

### [HIGH] LOG-001: SQL Server Missing Audit Logging
- **File:** `infra/core/database/sqlserver/sqlserver.bicep`
- **Lines:** 16-43
- **Resource:** `Microsoft.Sql/servers`
- **Issue:** No audit logging or diagnostic settings configured for SQL Server
- **Impact:**
  - No visibility into database access patterns
  - Cannot detect unauthorized access or SQL injection attempts
  - Compliance violations (PCI-DSS, HIPAA, SOC 2)
  - Inability to perform forensics after security incidents
- **Control Mapping:**
  - CIS Azure 4.1.1 - Ensure that 'Auditing' is set to 'On'
  - NIST 800-53: AU-2, AU-3, AU-12
  - Azure Security Benchmark: LT-1, LT-4
  - PCI-DSS: 10.2, 10.3

### [HIGH] LOG-002: Key Vault Missing Diagnostic Settings
- **File:** `infra/core/security/keyvault.bicep`
- **Lines:** 7-22
- **Resource:** `Microsoft.KeyVault/vaults`
- **Issue:** No diagnostic settings for audit logging of Key Vault access
- **Impact:**
  - No audit trail for secret access
  - Cannot detect unauthorized secret retrieval
  - Compliance violations
  - No forensic capability for security incidents
- **Control Mapping:**
  - CIS Azure 5.1.5 - Ensure that logging for Azure Key Vault is 'Enabled'
  - NIST 800-53: AU-2, AU-12
  - Azure Security Benchmark: LT-3
  - PCI-DSS: 10.2

### [HIGH] IAM-001: Key Vault Using Access Policies Instead of RBAC
- **File:** `infra/core/security/keyvault.bicep`
- **Lines:** 14-20
- **Resource:** `Microsoft.KeyVault/vaults`
- **Issue:** Key Vault uses legacy access policies instead of Azure RBAC
- **Impact:**
  - Less granular access control
  - Harder to audit and manage permissions
  - Not aligned with modern Azure identity best practices
  - Difficulty integrating with Conditional Access policies
- **Control Mapping:**
  - CIS Azure 8.5 - Enable role-based access control for Azure Key Vault
  - NIST 800-53: AC-6 (Least Privilege)
  - Azure Security Benchmark: PA-7
  - PCI-DSS: 7.1

### [HIGH] CNT-001: Kubernetes Pod Running Privileged Container
- **File:** `manifests/critical-double.yaml`
- **Lines:** 9-11
- **Resource:** Pod `kubesec-test`
- **Issue:** Container configured with privileged mode and allowPrivilegeEscalation enabled
- **Impact:**
  - Container can access host resources
  - Potential container escape to host
  - Elevated privileges for malicious code execution
  - Violation of least privilege principle
- **Control Mapping:**
  - CIS Kubernetes 5.2.1 - Minimize the admission of privileged containers
  - NSA/CISA: Non-root Containers and Privilege Escalation
  - NIST 800-53: AC-6, CM-7
  - PCI-DSS: 2.2

---

## Medium Severity Findings

### [MEDIUM] ENC-001: SQL Server Missing Transparent Data Encryption
- **File:** `infra/core/database/sqlserver/sqlserver.bicep`
- **Lines:** 28-31
- **Resource:** `Microsoft.Sql/servers/databases`
- **Issue:** Database does not explicitly enable Transparent Data Encryption (TDE)
- **Impact:**
  - Data at rest not explicitly encrypted with TDE
  - Potential compliance violations
  - Risk if backup files are compromised
- **Control Mapping:**
  - CIS Azure 4.1.2 - Ensure that 'Data encryption' is set to 'On' on a SQL Database
  - NIST 800-53: SC-28 (Protection of Information at Rest)
  - Azure Security Benchmark: DP-4
  - PCI-DSS: 3.4

### [MEDIUM] IAM-002: Key Vault Missing Soft Delete and Purge Protection
- **File:** `infra/core/security/keyvault.bicep`
- **Lines:** 7-22
- **Resource:** `Microsoft.KeyVault/vaults`
- **Issue:** Key Vault does not enable soft delete and purge protection
- **Impact:**
  - Permanent loss of secrets if accidentally deleted
  - No recovery window for deleted secrets
  - Potential service disruption
  - Insider threat: malicious deletion without recoverability
- **Control Mapping:**
  - CIS Azure 8.4 - Ensure the key vault is recoverable
  - Azure Security Benchmark: BR-2, DP-6
  - NIST 800-53: CP-9

### [MEDIUM] NSG-003: App Service Missing Private Endpoint Configuration
- **File:** `infra/core/host/appservice.bicep`
- **Lines:** 38-89
- **Resource:** `Microsoft.Web/sites`
- **Issue:** App Service accessible via public endpoint without option for private endpoint
- **Impact:**
  - Application exposed to internet without network isolation option
  - No option for VNet integration for internal apps
  - Increased attack surface
- **Control Mapping:**
  - Azure Security Benchmark: NS-2
  - NIST 800-53: SC-7
  - PCI-DSS: 1.2

### [MEDIUM] CNT-002: Docker Compose Exposes Hardcoded Secrets
- **File:** `docker-compose.yml`
- **Lines:** 22-24
- **Resource:** SQL Server container
- **Issue:** Database password hardcoded in plain text in docker-compose file
- **Impact:**
  - Credentials exposed in version control
  - Password visible to anyone with repository access
  - No credential rotation capability
  - Violation of secrets management best practices
- **Control Mapping:**
  - CIS Docker 5.10 - Do not store secrets in Docker images
  - NIST 800-53: IA-5 (Authenticator Management)
  - PCI-DSS: 8.2.1

---

## Low Severity Findings

### [LOW] CNT-003: Dockerfiles Not Running as Non-Root User
- **File:** `src/Web/Dockerfile`, `src/PublicApi/Dockerfile`
- **Lines:** Various
- **Resources:** Container images
- **Issue:** Dockerfiles do not specify USER directive to run as non-root
- **Impact:**
  - Containers run as root by default
  - Increased privilege if container is compromised
  - Defense-in-depth gap
- **Control Mapping:**
  - CIS Docker 4.1 - Create a user for the container
  - NSA/CISA: Non-root Containers
  - NIST 800-53: AC-6

### [LOW] IAM-003: App Service Plan Missing Zone Redundancy
- **File:** `infra/main.bicep`
- **Lines:** 128-130
- **Resource:** App Service Plan SKU
- **Issue:** App Service Plan uses B1 SKU without zone redundancy
- **Impact:**
  - Reduced availability during Azure datacenter outages
  - No automatic failover capability
  - Potential service disruption
- **Control Mapping:**
  - Azure Security Benchmark: BR-1
  - NIST 800-53: CP-2, CP-6

---

## Remediation Recommendations

### Priority 1: Critical Issues (Immediate Action Required)

#### Fix NSG-001: Secure SQL Server Network Access

**Recommended Solution:** Disable public network access and use private endpoints

```diff
# File: infra/core/database/sqlserver/sqlserver.bicep

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
-   publicNetworkAccess: 'Enabled'
+   publicNetworkAccess: 'Disabled'
    administratorLogin: sqlAdmin
    administratorLoginPassword: sqlAdminPassword
  }

  resource database 'databases' = {
    name: databaseName
    location: location
  }

- resource firewall 'firewallRules' = {
-   name: 'Azure Services'
-   properties: {
-     // Allow all clients
-     // Note: range [0.0.0.0-0.0.0.0] means "allow all Azure-hosted clients only".
-     // This is not sufficient, because we also want to allow direct access from developer machine, for debugging purposes.
-     startIpAddress: '0.0.0.1'
-     endIpAddress: '255.255.255.254'
-   }
- }
}
```

**Alternative for Development:** If public access is required for development, restrict to specific IP ranges:

```bicep
# Only for development environments
resource firewall 'firewallRules' = if (enablePublicAccess) {
  name: 'DeveloperAccess'
  properties: {
    startIpAddress: '203.0.113.0'  // Replace with actual developer IP
    endIpAddress: '203.0.113.255'
  }
}
```

### Priority 2: High Severity Issues

#### Fix NSG-002, LOG-002, IAM-001: Secure and Monitor Key Vault

```diff
# File: infra/core/security/keyvault.bicep

+param logAnalyticsWorkspaceId string = ''
+param enableRbac bool = true

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: { family: 'A', name: 'standard' }
-   accessPolicies: !empty(principalId) ? [
-     {
-       objectId: principalId
-       permissions: { secrets: [ 'get', 'list' ] }
-       tenantId: subscription().tenantId
-     }
-   ] : []
+   enableRbacAuthorization: enableRbac
+   enableSoftDelete: true
+   softDeleteRetentionInDays: 90
+   enablePurgeProtection: true
+   networkAcls: {
+     defaultAction: 'Deny'
+     bypass: 'AzureServices'
+     ipRules: []
+     virtualNetworkRules: []
+   }
  }
}

+// Diagnostic settings for audit logging
+resource keyVaultDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
+  name: '${name}-diagnostics'
+  scope: keyVault
+  properties: {
+    workspaceId: logAnalyticsWorkspaceId
+    logs: [
+      {
+        categoryGroup: 'audit'
+        enabled: true
+        retentionPolicy: {
+          enabled: true
+          days: 90
+        }
+      }
+      {
+        categoryGroup: 'allLogs'
+        enabled: true
+      }
+    ]
+    metrics: [
+      {
+        category: 'AllMetrics'
+        enabled: true
+      }
+    ]
+  }
+}
+
+// Use RBAC instead of access policies
+resource keyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(principalId) && enableRbac) {
+  name: guid(keyVault.id, principalId, 'Key Vault Secrets User')
+  scope: keyVault
+  properties: {
+    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
+    principalId: principalId
+    principalType: 'ServicePrincipal'
+  }
+}

output endpoint string = keyVault.properties.vaultUri
output name string = keyVault.name
```

#### Fix LOG-001: Enable SQL Server Auditing

```diff
# File: infra/core/database/sqlserver/sqlserver.bicep

+param logAnalyticsWorkspaceId string = ''
+param auditStorageAccountId string = ''

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Disabled'
    administratorLogin: sqlAdmin
    administratorLoginPassword: sqlAdminPassword
  }

  resource database 'databases' = {
    name: databaseName
    location: location
+   properties: {
+     transparentDataEncryption: {
+       state: 'Enabled'
+     }
+   }
  }
}

+// SQL Server auditing
+resource sqlServerAudit 'Microsoft.Sql/servers/auditingSettings@2022-05-01-preview' = {
+  name: 'default'
+  parent: sqlServer
+  properties: {
+    state: 'Enabled'
+    isAzureMonitorTargetEnabled: true
+    retentionDays: 90
+  }
+}
+
+// SQL Server diagnostic settings
+resource sqlServerDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
+  name: '${name}-diagnostics'
+  scope: sqlServer
+  properties: {
+    workspaceId: logAnalyticsWorkspaceId
+    logs: [
+      {
+        category: 'SQLSecurityAuditEvents'
+        enabled: true
+      }
+      {
+        category: 'DevOpsOperationsAudit'
+        enabled: true
+      }
+    ]
+    metrics: [
+      {
+        category: 'AllMetrics'
+        enabled: true
+      }
+    ]
+  }
+}
```

#### Fix CNT-001: Remove Privileged Container Configuration

```diff
# File: manifests/critical-double.yaml

apiVersion: v1
kind: Pod
metadata:
  name: kubesec-test
spec:
+ securityContext:
+   runAsNonRoot: true
+   runAsUser: 1000
+   fsGroup: 1000
+   seccompProfile:
+     type: RuntimeDefault
  containers:
  - name: kubesec-demo
-   image: gcr.io/google-samples/node-hello:1.0
+   image: gcr.io/google-samples/node-hello@sha256:abc123...  # Use pinned digest
    securityContext:
-     allowPrivilegeEscalation: true
-     privileged: true
+     allowPrivilegeEscalation: false
+     privileged: false
+     readOnlyRootFilesystem: true
+     capabilities:
+       drop:
+         - ALL
+   resources:
+     limits:
+       cpu: "500m"
+       memory: "256Mi"
+     requests:
+       cpu: "100m"
+       memory: "128Mi"
```

### Priority 3: Medium Severity Issues

#### Fix CNT-002: Remove Hardcoded Secrets from Docker Compose

```diff
# File: docker-compose.yml

version: '3.4'

services:
  eshopwebmvc:
    image: ${DOCKER_REGISTRY-}eshopwebmvc
    build:
      context: .
      dockerfile: src/Web/Dockerfile
    depends_on:
      - "sqlserver"
  eshoppublicapi:
    image: ${DOCKER_REGISTRY-}eshoppublicapi
    build:
      context: .
      dockerfile: src/PublicApi/Dockerfile
    depends_on:
      - "sqlserver"
  sqlserver:
    image: mcr.microsoft.com/azure-sql-edge
    ports:
      - "1433:1433"
    environment:
-     - SA_PASSWORD=@someThingComplicated1234
+     - SA_PASSWORD=${SQL_SA_PASSWORD}  # Load from environment variable or .env file
      - ACCEPT_EULA=Y
```

**Create `.env.example` file:**
```bash
# .env.example - Commit this file (without actual values)
SQL_SA_PASSWORD=<set-strong-password-here>
```

**Add to `.gitignore`:**
```
.env
```

### Priority 4: Low Severity Issues

#### Fix CNT-003: Run Containers as Non-Root User

```diff
# File: src/Web/Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY *.sln .
COPY . .
WORKDIR /app/src/Web
RUN dotnet restore

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/src/Web/out ./

+# Create non-root user
+RUN groupadd -r appuser && useradd -r -g appuser appuser
+RUN chown -R appuser:appuser /app
+
+USER appuser

ENTRYPOINT ["dotnet", "Web.dll"]
```

```diff
# File: src/PublicApi/Dockerfile

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
WORKDIR "/app/src/PublicApi"
RUN dotnet restore

RUN dotnet build "./PublicApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./PublicApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
+
+# Create non-root user
+RUN groupadd -r appuser && useradd -r -g appuser appuser
+RUN chown -R appuser:appuser /app
+
+USER appuser
+
ENTRYPOINT ["dotnet", "PublicApi.dll"]
```

---

## Control Mapping Matrix

| Finding ID | Severity | CIS Azure | CIS Kubernetes | NIST 800-53 | Azure Security Benchmark | PCI-DSS |
|------------|----------|-----------|----------------|-------------|--------------------------|---------|
| NSG-001 | CRITICAL | 4.1.3 | N/A | SC-7, SC-8 | NS-1, NS-2 | 1.2, 1.3 |
| NSG-002 | HIGH | 8.4, 8.5 | N/A | AC-3, SC-7 | NS-2, IM-1 | 8.2 |
| LOG-001 | HIGH | 4.1.1 | N/A | AU-2, AU-3, AU-12 | LT-1, LT-4 | 10.2, 10.3 |
| LOG-002 | HIGH | 5.1.5 | N/A | AU-2, AU-12 | LT-3 | 10.2 |
| IAM-001 | HIGH | 8.5 | N/A | AC-6 | PA-7 | 7.1 |
| CNT-001 | HIGH | N/A | 5.2.1 | AC-6, CM-7 | N/A | 2.2 |
| ENC-001 | MEDIUM | 4.1.2 | N/A | SC-28 | DP-4 | 3.4 |
| IAM-002 | MEDIUM | 8.4 | N/A | CP-9 | BR-2, DP-6 | N/A |
| NSG-003 | MEDIUM | N/A | N/A | SC-7 | NS-2 | 1.2 |
| CNT-002 | MEDIUM | N/A | N/A | IA-5 | N/A | 8.2.1 |
| CNT-003 | LOW | N/A | 5.2.6 | AC-6 | N/A | 2.2 |
| IAM-003 | LOW | N/A | N/A | CP-2, CP-6 | BR-1 | N/A |

---

## Microsoft Security DevOps (MSDO) Integration Recommendations

### Recommended Analyzers for CI/CD Pipeline

#### 1. Template Analyzer (ARM/Bicep)
Add to your Azure DevOps or GitHub Actions pipeline:

```yaml
# Azure DevOps Pipeline
- task: MicrosoftSecurityDevOps@1
  displayName: 'Run Microsoft Security DevOps'
  inputs:
    categories: 'IaC'
    tools: 'templateanalyzer'
```

```yaml
# GitHub Actions
- name: Run Microsoft Security DevOps
  uses: microsoft/security-devops-action@v1
  with:
    tools: templateanalyzer
```

#### 2. Checkov (Multi-IaC Scanner)
Scans Bicep, Kubernetes, Docker, and Docker Compose:

```yaml
# GitHub Actions
- name: Run Checkov
  uses: bridgecrewio/checkov-action@v12
  with:
    directory: .
    framework: terraform,bicep,kubernetes,dockerfile,docker_composition
    output_format: sarif
    output_file_path: results/checkov.sarif
    soft_fail: false
    skip_check: CKV_DOCKER_2  # Only if you have specific exceptions
```

#### 3. Trivy (Container and IaC Security)

```yaml
# GitHub Actions
- name: Run Trivy IaC scan
  uses: aquasecurity/trivy-action@master
  with:
    scan-type: 'config'
    scan-ref: '.'
    format: 'sarif'
    output: 'results/trivy-iac.sarif'
    severity: 'CRITICAL,HIGH,MEDIUM'
```

#### 4. Kubesec (Kubernetes Security)

```yaml
# GitHub Actions
- name: Run Kubesec
  uses: controlplaneio/kubesec-action@v0.0.2
  with:
    input: manifests/*.yaml
    format: sarif
    output: results/kubesec.sarif
```

#### 5. Hadolint (Dockerfile Linting)

```yaml
# GitHub Actions
- name: Run Hadolint
  uses: hadolint/hadolint-action@v3.1.0
  with:
    dockerfile: src/Web/Dockerfile
    format: sarif
    output-file: results/hadolint-web.sarif
    
- name: Run Hadolint for PublicApi
  uses: hadolint/hadolint-action@v3.1.0
  with:
    dockerfile: src/PublicApi/Dockerfile
    format: sarif
    output-file: results/hadolint-api.sarif
```

### Complete GitHub Actions Workflow Example

```yaml
# .github/workflows/iac-security-scan.yml
name: IaC Security Scanning

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]
  workflow_dispatch:

jobs:
  iac-security:
    name: IaC Security Scan
    runs-on: ubuntu-latest
    permissions:
      contents: read
      security-events: write
      
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        
      - name: Run Microsoft Security DevOps
        uses: microsoft/security-devops-action@v1
        with:
          tools: templateanalyzer
          
      - name: Run Checkov
        uses: bridgecrewio/checkov-action@v12
        with:
          directory: .
          framework: bicep,kubernetes,dockerfile,docker_composition
          output_format: sarif
          output_file_path: results/checkov.sarif
          
      - name: Run Trivy IaC Scan
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: 'config'
          scan-ref: '.'
          format: 'sarif'
          output: 'results/trivy-iac.sarif'
          
      - name: Run Kubesec
        uses: controlplaneio/kubesec-action@v0.0.2
        with:
          input: manifests/
          
      - name: Upload SARIF results to GitHub Security
        uses: github/codeql-action/upload-sarif@v3
        if: always()
        with:
          sarif_file: results/
```

### Azure DevOps Pipeline Example

```yaml
# .azuredevops/pipelines/iac-security.yml
trigger:
  branches:
    include:
      - main
      - develop

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: MicrosoftSecurityDevOps@1
  displayName: 'Run Microsoft Security DevOps'
  inputs:
    categories: 'IaC'
    tools: 'templateanalyzer'

- script: |
    docker run --rm -v $(Build.SourcesDirectory):/src bridgecrew/checkov -d /src --framework bicep kubernetes dockerfile docker-compose -o sarif --output-file-path /src/checkov-results.sarif
  displayName: 'Run Checkov'

- script: |
    docker run --rm -v $(Build.SourcesDirectory):/src aquasec/trivy config --format sarif --output /src/trivy-results.sarif /src
  displayName: 'Run Trivy IaC Scan'

- task: PublishBuildArtifacts@1
  inputs:
    PathtoPublish: '$(Build.SourcesDirectory)'
    ArtifactName: 'security-results'
```

---

## Policy as Code Recommendations

### Azure Policy Integration

Create Azure Policy assignments to enforce security controls:

```bicep
// Example: Enforce private endpoints for SQL Server
resource sqlPrivateEndpointPolicy 'Microsoft.Authorization/policyAssignments@2022-06-01' = {
  name: 'sql-private-endpoint-policy'
  properties: {
    policyDefinitionId: '/providers/Microsoft.Authorization/policyDefinitions/7698e800-9299-47a6-b3b6-5a0fee576eed'
    displayName: 'Azure SQL Database should use private link'
    enforcementMode: 'Default'
  }
}
```

### OPA/Gatekeeper for Kubernetes

```yaml
# Example constraint template for pod security
apiVersion: templates.gatekeeper.sh/v1
kind: ConstraintTemplate
metadata:
  name: k8spsprivilegedcontainer
spec:
  crd:
    spec:
      names:
        kind: K8sPSPPrivilegedContainer
  targets:
    - target: admission.k8s.gatekeeper.sh
      rego: |
        package k8spsprivileged
        
        violation[{"msg": msg}] {
          c := input.review.object.spec.containers[_]
          c.securityContext.privileged
          msg := sprintf("Privileged container is not allowed: %v", [c.name])
        }
```

---

## Next Steps

### Immediate Actions (Within 24 Hours)
1. ✅ Review this security report with the development and security teams
2. ⚠️ Address **CRITICAL** finding NSG-001 immediately
3. ⚠️ Apply HIGH severity fixes for network security and logging

### Short-term Actions (Within 1 Week)
1. Implement all HIGH severity remediations
2. Set up IaC security scanning in CI/CD pipelines
3. Configure Azure Policy for infrastructure compliance
4. Address MEDIUM severity findings

### Long-term Actions (Within 1 Month)
1. Implement Policy as Code (OPA/Gatekeeper)
2. Set up continuous compliance monitoring
3. Conduct regular IaC security reviews
4. Establish security guardrails for new infrastructure

---

## References and Resources

### Standards and Benchmarks
- [CIS Azure Foundations Benchmark v2.0](https://www.cisecurity.org/benchmark/azure)
- [CIS Kubernetes Benchmark v1.8](https://www.cisecurity.org/benchmark/kubernetes)
- [CIS Docker Benchmark v1.6](https://www.cisecurity.org/benchmark/docker)
- [Azure Security Benchmark v3](https://docs.microsoft.com/azure/security/benchmarks/)
- [NIST SP 800-53 Rev. 5](https://csrc.nist.gov/publications/detail/sp/800-53/rev-5/final)
- [NSA/CISA Kubernetes Hardening Guide](https://media.defense.gov/2022/Aug/29/2003066362/-1/-1/0/CTR_KUBERNETES_HARDENING_GUIDANCE_1.2_20220829.PDF)

### Tools and Documentation
- [Microsoft Security DevOps](https://github.com/microsoft/security-devops-action)
- [Checkov by Bridgecrew](https://www.checkov.io/)
- [Trivy by Aqua Security](https://aquasecurity.github.io/trivy/)
- [Kubesec](https://kubesec.io/)
- [Hadolint](https://github.com/hadolint/hadolint)
- [Azure Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)

### Microsoft Resources
- [Azure Well-Architected Framework - Security](https://docs.microsoft.com/azure/architecture/framework/security/)
- [Azure Security Best Practices](https://docs.microsoft.com/azure/security/fundamentals/best-practices-and-patterns)
- [Secure DevOps with Azure](https://docs.microsoft.com/azure/devops/organizations/security/)

---

**Report Generated By:** IaC & Cloud Configuration Guard Agent  
**Version:** 1.0  
**Contact:** DevSecOps Team
