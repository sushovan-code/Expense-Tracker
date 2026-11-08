---
description: Prepare (not apply) the infrastructure and pipeline changes to deploy this app
---
Target environment: $ARGUMENTS (default: dev)

1. Review what this app needs in the cloud against the platform-infra Terraform modules and this repo's .github/workflows.
2. Propose the Terraform and workflow changes, and run `terraform fmt`, `terraform validate` and `terraform plan` only.
3. Show me the plan summary and the changes. Never run `terraform apply` or `destroy`; deployment happens through the manual GitHub Actions workflows.
