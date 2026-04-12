### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
WG0001 | Usage | Error | `[RegisterSchema]` type must implement `ISchemaDefinition<TFamily, TScheme>`.
WG0002 | Usage | Error | `[ProjectTo]` method must be declared on a `[RegisterSchema]` schema type.
WG0003 | Usage | Error | `[ProjectTo]` target must implement `ISchemaDefinition<TFamily, TScheme>`.
WG0004 | Usage | Error | `[ProjectTo]` method must be static.
WG0005 | Usage | Error | `[ProjectTo]` method must declare exactly one parameter.
WG0006 | Usage | Error | `[ProjectTo]` method must not return `void`.
WG0007 | Usage | Error | `[ProjectTo]` method parameter type must implement `ITypedObject`.
WG0008 | Usage | Error | `[ProjectTo]` method return type must implement `ITypedObject`.
WG0009 | Usage | Error | `[ProjectTo]` method must be accessible from generated registration code.
