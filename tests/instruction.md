# Test Project Instructions

- Keep automated tests under `tests/`, separate from application projects in `src/`.
- Group tests by project and mirror the source project structure where practical (for example, `tests/OpenCampfire.Core.Tests/Helpers`).
- Reference production projects from test project files; do not copy production code into tests.
- Run the Core tests with `dotnet test tests/OpenCampfire.Core.Tests/OpenCampfire.Core.Tests.csproj`.
