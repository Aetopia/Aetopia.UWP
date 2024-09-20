# Aetopia.UWP
Provides various utilities for interacting with UWP apps from a desktop application.

## API

### `Injector.Inject`

The following methods do the following:

- Correctly setup ACLs for the target dynamic link library.

- Launch the specified UWP app using the specified application user model ID.

- Inject the dynamic link library.

```cs
Injector.Inject(string appUserModelId, string path)
```

```cs
Injector.InjectAsync(string appUserModelId, string path)
```