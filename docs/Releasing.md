# Releasing NuGet packages

The repository publishes **both** packages from a single version tag:

- `DimonSmart.Specification`
- `DimonSmart.Specification.EntityFrameworkCore`

The release workflow is [`.github/workflows/publish-nuget.yml`](../.github/workflows/publish-nuget.yml). It runs only after a `vMAJOR.MINOR.PATCH` tag is pushed. It validates the tag, checks that its commit belongs to `main`, runs the repository's tests, packs both projects with the **same version**, verifies their package metadata and their inter-package dependency, publishes them to NuGet.org, and creates a GitHub Release containing the `.nupkg` files.

The version from the tag overrides the development-time `<Version>4.0.0</Version>` properties in the project files. Do not edit both project files just to release a patch version.

## One-time NuGet.org configuration

Publication uses [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing). No long-lived API key or GitHub secret is required.

In the NuGet.org account that owns both packages, open **Trusted Publishing** and register a GitHub Actions policy with:

- **Owner:** `DimonSmart` (individual NuGet.org account)
- **Repository owner:** `DimonSmart`
- **Repository:** `SpecificationPattern`
- **Workflow file:** `publish-nuget.yml` (filename only)
- **Environment:** leave empty (the workflow does not specify one)
- **Scope:** permit new versions of the existing `DimonSmart.Specification` and `DimonSmart.Specification.EntityFrameworkCore` packages (for example, with the scope glob `DimonSmart.Specification*`).

Ensure GitHub Actions is enabled for the repository and that your local Git credentials can push tags. NuGet may show a temporary activation window for a new policy; follow its status prompts if it does.

## Publish a version

From a clean local checkout on Windows:

```bat
publish-next-version.bat
```

Or from PowerShell:

```powershell
.\publish-next-version.ps1
```

To inspect the next version without creating or pushing a tag:

```bat
publish-next-version.bat -DryRun
```

The script fetches tags, switches to `main`, fast-forwards it, then finds the **highest numeric** `vMAJOR.MINOR.PATCH` tag and increments the patch number. Unrelated tags are ignored. If no matching version tags exist, the **first release is `v4.0.0`**, consistent with both current project versions. It creates an annotated tag and pushes it to `origin`. Pushing the tag starts the GitHub Actions publication workflow; the local script does not publish to NuGet itself.

The two package versions always match. The EntityFrameworkCore package must depend on the same released version of the core package.

Check progress under [GitHub Actions](https://github.com/DimonSmart/SpecificationPattern/actions/workflows/publish-nuget.yml). If NuGet authorization is not configured yet, the workflow will fail at the authentication step, and you can configure Trusted Publishing and rerun the failed workflow. Already published packages are skipped on retries; NuGet package versions are immutable.

To start a new **minor or major** version, create its `vMAJOR.MINOR.PATCH` tag explicitly on `main` and push it. After that, the helper script continues incrementing patch versions from the highest tag. Do not delete or move release tags.
