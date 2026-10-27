# AIComFramework

AIComFramework is a .NET 10 library for reusable AI domain catalogs, typed roles and
rules, explicit request composition, trust values, conversations and result validation.
The application supplies its own `Microsoft.Extensions.AI.IChatClient` implementation.

## Packages

| Package | Use |
| --- | --- |
| AIComFramework.Abstractions | Contracts, roles/rules, trust values and portable snapshots. No external package dependencies. |
| AIComFramework.Core | Composition, conversation history, dependency injection and result validation. Depends on Abstractions, Microsoft.Extensions.AI and DI abstractions. |
| AIComFramework.Serialization.Json | Strict JSON definition serialization. Depends on Abstractions and DI abstractions, independently of Core. |

For most applications, install Core; add Serialization.Json when persisting or editing
declarative definitions. Abstractions is installed transitively by both packages.

```sh
dotnet add package AIComFramework.Core --version 0.1.0-alpha.1
dotnet add package AIComFramework.Serialization.Json --version 0.1.0-alpha.1
```

These commands require the version to exist in a configured feed. For locally built
packages and publication prerequisites, see the
[packaging guide](https://github.com/PatrikZacek/AiComFramework/blob/main/docs/Packaging.md).
The initial version is a prerelease; preparing packages does not publish them.

See the [usage guide](https://github.com/PatrikZacek/AiComFramework#readme) and
[executable sample](https://github.com/PatrikZacek/AiComFramework/tree/main/samples/AIComFramework.Samples.Console)
for domain definitions, request composition, DI, conversations and validation.

Portable PDB symbol packages and Source Link support debugging against the source
commit. Samples and tests are excluded from the packages. Target framework: net10.0.

## License

MIT. The license text is included in each package and in the
[repository](https://github.com/PatrikZacek/AiComFramework/blob/main/LICENSE).
Third-party dependencies retain their own licenses.
