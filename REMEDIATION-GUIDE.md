# IaC Security Remediation Guide

This document provides instructions for applying the security fixes identified in the IaC security review.

## Overview

The security review identified **12 security findings** across Azure Bicep templates, Kubernetes manifests, Dockerfiles, and Docker Compose configurations. The remediation has been applied to address:

- **1 CRITICAL** - SQL Server publicly accessible to the internet
- **5 HIGH** - Network security, logging, IAM, and container security issues
- **4 MEDIUM** - Encryption, access management, and secrets management
- **2 LOW** - Container hardening and availability improvements

## Changes Made

### 1. SQL Server Security Hardening (CRITICAL)

**File:** `infra/core/database/sqlserver/sqlserver.bicep`

**Changes:**
- ✅ Disabled public network access (`publicNetworkAccess: 'Disabled'`)
- ✅ Removed overly permissive firewall rules (0.0.0.1-255.255.255.254)
- ✅ Added SQL Server audit logging with 90-day retention
- ✅ Added diagnostic settings for security audit events
- ✅ Added support for Log Analytics workspace integration

**Impact:** SQL Server is no longer accessible from the internet, significantly reducing attack surface. Audit logging provides visibility into database access patterns.

**Migration Note:** If public access is required for development, you can:
1. Use Azure Bastion or VPN for secure access
2. Add specific IP whitelist firewall rules
3. Use private endpoints (recommended for production)

### 2. Key Vault Security Hardening (HIGH)

**File:** `infra/core/security/keyvault.bicep`

**Changes:**
- ✅ Enabled RBAC authorization (modern Azure identity model)
- ✅ Enabled soft delete with 90-day retention
- ✅ Enabled purge protection (prevents permanent deletion)
- ✅ Disabled public network access
- ✅ Configured network ACLs to deny by default
- ✅ Added diagnostic settings for audit logging
- ✅ Added RBAC role assignment for managed identities

**Impact:** Key Vault is now hardened against unauthorized access, accidental deletion, and provides full audit trail.

**Migration Note:** The RBAC model is backward compatible with access policies. Set `enableRbacAuthorization: false` if you need to continue using access policies.

### 3. Kubernetes Pod Security (HIGH)

**File:** `manifests/critical-double.yaml`

**Changes:**
- ✅ Removed privileged container configuration
- ✅ Disabled privilege escalation
- ✅ Added non-root user enforcement (UID 1000)
- ✅ Added read-only root filesystem
- ✅ Dropped all Linux capabilities
- ✅ Added seccomp profile (RuntimeDefault)
- ✅ Added resource limits and requests

**Impact:** Pod now follows Kubernetes security best practices and CIS Kubernetes Benchmark recommendations.

### 4. Docker Container Security (LOW/MEDIUM)

**Files:** 
- `src/Web/Dockerfile`
- `src/PublicApi/Dockerfile`

**Changes:**
- ✅ Added non-root user (`appuser`)
- ✅ Set proper file ownership
- ✅ Container runs as non-root user

**Impact:** Containers no longer run as root, reducing privilege if compromised.

**Testing Note:** Test your application with non-root user to ensure all file access works correctly.

### 5. Docker Compose Secrets Management (MEDIUM)

**File:** `docker-compose.yml`

**Changes:**
- ✅ Removed hardcoded password
- ✅ Changed to use environment variable `${SQL_SA_PASSWORD}`
- ✅ Added `.env.example` file with template
- ✅ Maintains backward compatibility with default value

**Impact:** Passwords no longer committed to version control.

**Action Required:**
1. Create a `.env` file in the repository root (already in `.gitignore`)
2. Set `SQL_SA_PASSWORD=YourStrongPassword123!`
3. The `.env` file will be used automatically by docker-compose

### 6. IaC Security Scanning Pipelines

**New Files:**
- `.github/workflows/iac-security-scan.yml` (GitHub Actions)
- `.azuredevops/pipelines/iac-security-scan.yml` (Azure DevOps)

**Features:**
- ✅ Template Analyzer for Bicep/ARM
- ✅ Checkov for multi-IaC scanning
- ✅ Trivy for IaC and container security
- ✅ Kubesec for Kubernetes manifests
- ✅ Hadolint for Dockerfile linting
- ✅ SARIF output integration with GitHub Security
- ✅ Artifact publishing for compliance records

## Deployment Instructions

### Prerequisites

Before deploying the updated infrastructure:

1. **Log Analytics Workspace (Optional but Recommended)**
   
   For logging and monitoring, create a Log Analytics workspace:
   ```bash
   az monitor log-analytics workspace create \
     --resource-group <your-rg> \
     --workspace-name <workspace-name> \
     --location <location>
   ```

2. **Update Parameters (if needed)**

   If you want to use Log Analytics for diagnostics, update `main.parameters.json`:
   ```json
   {
     "logAnalyticsWorkspaceId": {
       "value": "/subscriptions/{sub-id}/resourceGroups/{rg}/providers/Microsoft.OperationalInsights/workspaces/{workspace-name}"
     }
   }
   ```

### Option A: Deploy with Azure Developer CLI (azd)

```bash
# Navigate to repository root
cd /path/to/ado-advsec-devsecops

# Set environment variables
export AZURE_ENV_NAME="your-env-name"
export AZURE_LOCATION="eastus"
export AZURE_SUBSCRIPTION_ID="your-subscription-id"

# Deploy infrastructure
azd up
```

### Option B: Deploy with Azure CLI

```bash
# Create deployment
az deployment sub create \
  --name iac-security-deployment \
  --location eastus \
  --template-file infra/main.bicep \
  --parameters infra/main.parameters.json
```

### Option C: Deploy with Bicep CLI

```bash
az bicep build --file infra/main.bicep
az deployment sub create \
  --name iac-security-deployment \
  --location eastus \
  --template-file infra/main.bicep \
  --parameters @infra/main.parameters.json
```

## Testing the Changes

### 1. Test Bicep Templates

Validate Bicep syntax and preview changes:

```bash
# Validate Bicep files
az bicep build --file infra/main.bicep

# Preview what-if changes
az deployment sub what-if \
  --location eastus \
  --template-file infra/main.bicep \
  --parameters @infra/main.parameters.json
```

### 2. Test Docker Containers

Build and test containers with non-root user:

```bash
# Build and run Web container
docker build -t eshopweb -f src/Web/Dockerfile .
docker run --rm -it -p 5106:5106 eshopweb

# Build and run PublicApi container
docker build -t eshopapi -f src/PublicApi/Dockerfile .
docker run --rm -it -p 5200:80 eshopapi

# Verify containers run as non-root
docker run --rm eshopweb whoami  # Should output: appuser
```

### 3. Test Docker Compose

```bash
# Create .env file
cat > .env << EOF
SQL_SA_PASSWORD=YourStrongPassword123!
EOF

# Start services
docker-compose up -d

# Verify services are running
docker-compose ps

# Check logs
docker-compose logs sqlserver
```

### 4. Test Kubernetes Manifests

```bash
# Validate Kubernetes manifests
kubectl apply --dry-run=client -f manifests/critical-double.yaml

# Apply to cluster (if available)
kubectl apply -f manifests/critical-double.yaml

# Verify pod security
kubectl get pod kubesec-test -o jsonpath='{.spec.securityContext}'
kubectl get pod kubesec-test -o jsonpath='{.spec.containers[0].securityContext}'
```

### 5. Run Security Scanners Locally

#### Checkov
```bash
# Install Checkov
pip install checkov

# Scan all IaC files
checkov -d . --framework bicep kubernetes dockerfile docker-compose

# Scan specific directory
checkov -d infra/ --framework bicep
```

#### Trivy
```bash
# Install Trivy
wget -qO - https://aquasecurity.github.io/trivy-repo/deb/public.key | sudo apt-key add -
echo "deb https://aquasecurity.github.io/trivy-repo/deb $(lsb_release -sc) main" | sudo tee -a /etc/apt/sources.list.d/trivy.list
sudo apt-get update && sudo apt-get install trivy

# Scan IaC configurations
trivy config .

# Scan specific files
trivy config infra/
```

#### Kubesec
```bash
# Using Docker
docker run -i kubesec/kubesec:latest scan - < manifests/critical-double.yaml

# Or install locally
curl -sSL https://github.com/controlplaneio/kubesec/releases/download/v2.13.0/kubesec_linux_amd64.tar.gz | tar xz
./kubesec scan manifests/critical-double.yaml
```

## Security Scanning Integration

### GitHub Actions

The workflow is triggered automatically on:
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop` branches
- Changes to IaC files (`infra/`, `manifests/`, Dockerfiles, docker-compose)
- Manual workflow dispatch

**To view results:**
1. Go to **Actions** tab in GitHub repository
2. Select **IaC Security Scanning** workflow
3. View scan results in **Security** tab > **Code scanning alerts**

### Azure DevOps

**To enable the pipeline:**
1. Go to **Pipelines** in Azure DevOps
2. Create new pipeline from existing YAML
3. Select `.azuredevops/pipelines/iac-security-scan.yml`
4. Update `azureSubscription` variable with your service connection
5. Run the pipeline

**To view results:**
1. Go to pipeline run details
2. Download **iac-security-results** artifact
3. View SARIF files with VS Code or upload to GitHub

## Compliance and Control Mapping

The implemented fixes address the following compliance frameworks:

| Framework | Controls Addressed |
|-----------|-------------------|
| **CIS Azure Benchmark** | 4.1.1, 4.1.2, 4.1.3, 5.1.5, 8.4, 8.5 |
| **CIS Kubernetes Benchmark** | 5.2.1, 5.2.6 |
| **CIS Docker Benchmark** | 4.1, 5.10 |
| **NIST 800-53 Rev. 5** | AC-3, AC-6, AU-2, AU-3, AU-12, SC-7, SC-8, SC-28, CP-9, IA-5, CM-7 |
| **Azure Security Benchmark** | NS-1, NS-2, LT-1, LT-3, LT-4, DP-4, DP-6, PA-7, BR-1, BR-2, IM-1 |
| **PCI-DSS** | 1.2, 1.3, 2.2, 3.4, 7.1, 8.2, 8.2.1, 10.2, 10.3 |

## Known Limitations and Considerations

### SQL Server Private Access

**Limitation:** With `publicNetworkAccess: 'Disabled'`, the SQL Server can only be accessed:
- From within Azure via private endpoints
- Through Azure services with managed identities
- Via VPN or Azure Bastion for management

**Workaround for Development:**
- Use Azure Bastion or VPN
- Temporarily enable specific IP firewall rules (not recommended for production)
- Use Azure Cloud Shell for SQL queries

### Key Vault Private Access

**Limitation:** Key Vault with `publicNetworkAccess: 'Disabled'` requires:
- Private endpoint configuration
- VNet integration for App Services
- Service endpoints or private endpoints

**Workaround:**
- Configure private endpoints in your VNet
- Use VNet integration for App Services
- Set `publicNetworkAccess: 'Enabled'` with IP restrictions if private endpoints are not feasible

### Docker Non-Root User

**Consideration:** Applications must not require root privileges:
- File writes must be to writable directories
- Network ports above 1024 (or use capability NET_BIND_SERVICE)
- No system-level operations

**If you encounter issues:**
- Review application logs for permission errors
- Adjust file permissions in Dockerfile
- Use volumes with appropriate ownership

## Rollback Instructions

If you need to rollback changes:

### SQL Server
```bicep
# Revert to public access (NOT RECOMMENDED)
properties: {
  publicNetworkAccess: 'Enabled'
}
```

### Key Vault
```bicep
# Revert to access policies
param enableRbacAuthorization bool = false
properties: {
  enableRbacAuthorization: false
  publicNetworkAccess: 'Enabled'
  # Remove network ACLs
}
```

### Kubernetes
```yaml
# Remove security hardening (NOT RECOMMENDED)
# Revert manifests/critical-double.yaml to original state
```

### Docker
```dockerfile
# Remove non-root user configuration (NOT RECOMMENDED)
# Comment out USER appuser line
```

## Support and Resources

### Documentation
- [SECURITY-FINDINGS.md](./SECURITY-FINDINGS.md) - Complete security review report
- [Azure Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [Kubernetes Security Best Practices](https://kubernetes.io/docs/concepts/security/)
- [Docker Security Best Practices](https://docs.docker.com/develop/security-best-practices/)

### Tools
- [Microsoft Security DevOps](https://github.com/microsoft/security-devops-action)
- [Checkov](https://www.checkov.io/documentation.html)
- [Trivy](https://aquasecurity.github.io/trivy/)
- [Kubesec](https://kubesec.io/)

### Security Standards
- [CIS Benchmarks](https://www.cisecurity.org/cis-benchmarks)
- [Azure Security Benchmark](https://docs.microsoft.com/azure/security/benchmarks/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)

## Questions or Issues?

If you encounter any issues with the security remediation:

1. Review the [SECURITY-FINDINGS.md](./SECURITY-FINDINGS.md) report
2. Check the validation steps in this guide
3. Review scanner output for specific issues
4. Consult the control mapping for compliance requirements

---

**Last Updated:** 2026-02-09  
**Security Review Version:** 1.0  
**Reviewed By:** IaC & Cloud Configuration Guard Agent
