# Publishing DuonDevKit to NuGet.org

This repo publishes five packages — `DuonDevKit.Core`, `DuonDevKit.EntityFrameworkCore`,
`DuonDevKit.AspNetCore`, `DuonDevKit.Dapper`, and `DuonDevKit.Jwt` — to NuGet.org via GitHub Actions,
authenticated with **Trusted Publishing (OIDC)**. No static API key is stored in GitHub Secrets.

## One-time setup: Trusted Publishing policy on NuGet.org

1. Sign in to [nuget.org](https://www.nuget.org) with an account that owns (or will own) all five
   package IDs.
2. Go to **Account settings → Trusted Publishing** (or the equivalent section under your profile —
   this is a newer NuGet.org feature, so double-check the exact menu path in the current UI).
3. Create a Trusted Publishing policy for each package (`DuonDevKit.Core`,
   `DuonDevKit.EntityFrameworkCore`, `DuonDevKit.AspNetCore`, `DuonDevKit.Dapper`, `DuonDevKit.Jwt`)
   pointing at:
   - Repository owner: `Duon0402`
   - Repository name: `DuonDevKit`
   - Workflow file: `.github/workflows/publish.yml`
   - Environment: (leave empty unless a GitHub Environment is added later)
4. Verify the `NuGet/login@v1` step in `.github/workflows/publish.yml` still matches the action
   name/version documented by NuGet.org at setup time — this feature is newer and the action may
   evolve. Update the workflow if the docs point to a different action.

If a package doesn't exist on NuGet.org yet, the first publish for it must reserve the ID — check
NuGet.org's current guidance on whether Trusted Publishing supports first-time publish or whether an
initial manual push (with a temporary API key) is required to create the package before a policy can
be attached to it.

## Release process

1. Decide the next version for whichever package(s) changed (packages don't have to release in
   lockstep, but keep `<Version>` in each changed `.csproj` accurate to what actually shipped).
2. Bump `<Version>` in whichever of `DuonDevKit.Core/DuonDevKit.Core.csproj`,
   `DuonDevKit.EntityFrameworkCore/DuonDevKit.EntityFrameworkCore.csproj`,
   `DuonDevKit.AspNetCore/DuonDevKit.AspNetCore.csproj`,
   `DuonDevKit.Dapper/DuonDevKit.Dapper.csproj`, or `DuonDevKit.Jwt/DuonDevKit.Jwt.csproj` changed.
3. Commit the version bump.
4. Tag and push. Tags aren't tied 1:1 to a package version — each push of a `v*` tag just triggers
   the workflow, which packs and pushes all five projects (`--skip-duplicate` no-ops whichever
   package(s) didn't change this release):
   ```
   git tag vX.Y.Z
   git push origin vX.Y.Z
   ```
5. The `publish` workflow runs automatically: build → test → pack → push all five `.nupkg` files to
   NuGet.org (`--skip-duplicate`, so re-running a tag after a partial failure, or a tag where only
   some packages changed, won't error on packages already published).
6. Confirm the expected package(s) appear on NuGet.org with the expected version.

## Local sanity check before tagging

```
dotnet build DuonDevKit.slnx --configuration Release
dotnet test DuonDevKit.slnx --configuration Release
dotnet pack DuonDevKit.Core/DuonDevKit.Core.csproj --configuration Release --output ./nupkgs
dotnet pack DuonDevKit.EntityFrameworkCore/DuonDevKit.EntityFrameworkCore.csproj --configuration Release --output ./nupkgs
dotnet pack DuonDevKit.AspNetCore/DuonDevKit.AspNetCore.csproj --configuration Release --output ./nupkgs
dotnet pack DuonDevKit.Dapper/DuonDevKit.Dapper.csproj --configuration Release --output ./nupkgs
dotnet pack DuonDevKit.Jwt/DuonDevKit.Jwt.csproj --configuration Release --output ./nupkgs
```

Inspect the generated `.nupkg` files (e.g. with `nuget.exe` or by renaming to `.zip`) to confirm
metadata and the `DuonDevKit.EntityFrameworkCore`/`DuonDevKit.AspNetCore`/`DuonDevKit.Dapper`/
`DuonDevKit.Jwt` → `DuonDevKit.Core` dependency versions look right before pushing a tag.
