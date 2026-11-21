# Releasing

GitHub Actions builds and tests every push and pull request to `main` (`.github/workflows/ci.yml`). To publish to
NuGet, push a version tag:

```sh
git tag v0.2.0
git push origin v0.2.0
```

`.github/workflows/release.yml` takes the package version from the tag, builds, tests, packs and pushes the `.nupkg`
and `.snupkg` to nuget.org using NuGet trusted publishing.

The root `README.md` is packed as the package's readme on nuget.org, which doesn't resolve relative links, so links from
it into `docs/` must be absolute GitHub URLs.