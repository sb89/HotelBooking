# Hotel Booking

## Development

### Run
Amend the application settings to point to a postgres database.

```
cd src/Web
dotnet run
```

### Migrations

Run the following commands from the `root` directory.

#### Add
`dotnet ef migrations add MigrationName --project src/Application --startup-project src/Web --output-dir Data/Migrations`

#### Rollback to previous migration
`dotnet ef database update PreviousMigrationName --project src/Application --startup-project src/Web`

#### Remove
`dotnet ef migrations remove --project src/Application --startup-project src/Web`

#### Run
`dotnet ef database update --project src/Application --startup-project src/Web`

The app will also automatically run migrations on startup

## Deployment
There is a dockerfile and docker-compose file that can be used to run using docker.

## Exercise

### Assumptions

- Room type implies room capacity: Single (1), Double (2), Deluxe (4)
- Hotel names not unique, an address field could be added to distinguish between hotels with the same name
- The search availability functionality will return multiple rooms (if possible) to meet the number of guests requirement.
- Although the search availabilty function returns multiple of rooms, the create booking endpoint will only allow a single room to be booked at a time. This could be changed to allow multiple rooms to be booked at the same time.

### Hosting

I've hosted the application on my VPS which can be accessed here. This was achieved using docker compose and nginx. The same approach would have been taken on an Azure VM.

### Improvements
- Github action at the moment just builds and ensures tests pass. This would be improved to add automatic deployment.
- Pagination where required
- Improve locking method, but for the purposes of this exercise the current implementation is sufficient.