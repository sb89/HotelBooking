# Hotel Booking

## Migrations

Run the following commands from the `root` directory.

### Add
`dotnet ef migrations add MigrationName --project src/Application --startup-project src/Web --output-dir Data/Migrations`

### Rollback to previous migration
`dotnet ef database update PreviousMigrationName --project src/Application --startup-project src/Web`

### Remove
`dotnet ef migrations remove --project src/Application --startup-project src/Web`

### Run
`dotnet ef database update --project src/Application --startup-project src/Web`