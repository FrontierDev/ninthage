# Benchmark build and startup foundation

The benchmark path is opt-in. Ordinary launches do not pass `--benchmark` and retain the existing `GameBootstrapper.IsHeadlessServer()` decision: batch mode or a null graphics device selects the server, otherwise the client.

## Build variants

Use `Tools/Ninth Age/Benchmark/Validate Build Configurations`, then build either `Build Development` or `Build Measurement`. The two project-owned configuration assets record the intended platform, x86_64 architecture, scripting backend, managed stripping, graphics API, development/profiler/debug flags, fixture manifest and build revision. The build tool applies those settings for the build and restores the editor settings afterward. `Windows Debug.asset` is not changed and is not treated as profiler-enabled.

The development variant uses Mono, Minimal stripping, development/debugging and profiler/deep-profiling flags. The measurement variant uses IL2CPP, Medium stripping and no development, profiler or debugging flags. The build emits `benchmark-build.json` beside the executable.

## Explicit launch

Every benchmark launch requires the role, profile, fixture, non-negative seed, absolute results directory, run identity and (for a client) real account credentials:

```text
NinthAge.exe -batchmode -nographics -benchmark --benchmark-role=server --benchmark-profile=P1-S0-04 --benchmark-fixture=world-forest --benchmark-seed=424242 --benchmark-results=C:\BenchmarkRuns\server\run-001 --benchmark-run-id=run-001 --benchmark-build-revision=benchmark-measurement-1.0.0
NinthAge.exe -batchmode -nographics -benchmark --benchmark-role=client --benchmark-profile=P1-S0-04 --benchmark-fixture=world-forest --benchmark-seed=424242 --benchmark-results=C:\BenchmarkRuns\client\run-001 --benchmark-run-id=run-001 --benchmark-build-revision=benchmark-measurement-1.0.0 --benchmark-client-username=benchmark-user --benchmark-client-password=provided-by-runner
```

The client role is an explicit exception to the normal batch/no-graphics server selection. It sets a process-local benchmark role before runtime systems initialize, then uses the normal `LoginAuthenticator`, account character list and `Server_RequestEnterWorld` RPC. It does not create accounts, bypass authentication or bypass character validation. A missing or rejected character selection fails the process explicitly.

Invalid benchmark arguments fail before either runtime initialization path starts. The process writes one immutable `run.json` in the requested result directory; an existing run identity is rejected and credentials are never written to metadata.

## Fixture and Addressables supply

Generated test-world scenes and their stable `.meta` files remain under the tracked `Assets/Tests/Performance/Fixtures/` tree. The build tool validates the configured `world-fixture-manifest.json` and every referenced scene before building. It does not copy fixture scenes into ignored production `Assets/Scenes/` or `Assets/Prefabs/`, and it does not add them to `ProjectSettings/EditorBuildSettings.asset`; the production scene list remains unchanged.

The existing runtime still obtains networked world content through the project Addressables catalog and PurrNet scene module. A fixture generator or later benchmark harness must add its generated scene/resource keys to the project-owned Addressables content input and rebuild the catalog before launching a run. A missing fixture file, meta file or content key is a preflight failure; the tooling does not silently substitute a production scene. The current fixture manifest is therefore a validated source manifest, not a claim that all future generated fixture scenes are already Addressable.

## Manual validation left to the user

Build and launch one ordinary client and one headless server, then launch the explicit benchmark server and automated client against an isolated test account/database. Confirm normal PurrNet initialization, authentication, character selection, world entry and deterministic `run.json`/`benchmark-build.json` metadata. Unity Editor, Player and PurrNet runtime validation is intentionally not automated by this repository task.
