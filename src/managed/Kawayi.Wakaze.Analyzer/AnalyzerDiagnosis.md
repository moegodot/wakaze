## Release 1.0

### New Rules

| Rule ID | Category | Severity | Notes                                                                  |
|---------|----------|----------|------------------------------------------------------------------------|
| KWA0001 | Usage    | Warning  | Invalid `SchemaFamily(string)` value.                                  |
| KWA0002 | Usage    | Warning  | Invalid `SchemaId(string)` value.                                      |
| KWA0003 | Usage    | Warning  | Registered schema `Schema` family mismatches declared family metadata. |
| KWA0004 | Usage    | Warning  | Registered schema family mismatches declared URI scheme metadata.      |
| KWA0005 | Documentation | Warning | Public API symbol is missing XML documentation entry metadata.     |
| KWA0006 | Documentation | Warning | Public API symbol is missing one or more required `<param>` tags.  |
| KWA0007 | Documentation | Warning | Public API symbol is missing one or more required `<typeparam>` tags. |
| KWA0008 | Documentation | Warning | Public API symbol is missing a required `<returns>` tag.           |
| KWA0009 | Documentation | Warning | Public property or indexer is missing a required `<value>` tag.    |
| KWA0010 | Documentation | Warning | Public API symbol explicitly throws an exception without documenting it. |

## MSBuild Rule Switches

Each Wakaze analyzer rule can be disabled with an MSBuild property named `EnableKWA****`.
Rules stay enabled by default. A rule is disabled only when its matching property is explicitly set to `false`.

```xml
<PropertyGroup>
    <EnableKWA0005>false</EnableKWA0005>
</PropertyGroup>
```

This property can be set in a project file or in `Directory.Build.props`.
