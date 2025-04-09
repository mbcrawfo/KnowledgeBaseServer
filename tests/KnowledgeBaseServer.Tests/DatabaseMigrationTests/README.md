After releasing a new version, add a new database and tests to ensure that future migrations upgrade it correctly.

1.  Create the db using the TestDbGenerator app.  From the project root run:
    ```bash
    dotnet run --project tests/TestDbGenerator/TestDbGenerator.csproj tests/KnowledgeBaseServer.Tests/DatabaseMigrationTests/databases/v{version}.sqlite
    ```
2. Add a new test class in this folder that subclasses `MigrationTest`.
3. Define nested records/classes that represent the tables in the database, in its form for the release that was just created.
4. Add a test method that does the following
   - Loads all data from the test database using those records.
   - Migrates the db to the latest version.
   - Uses the helper methods and data objects from the `Data` namespace to load the post-migration data.
   - Asserts that the pre-migration and post-migration data are equal.

As new migrations are added to the project, update the migration tests as necessary to validate that the data is correct
after the latest migrations have been applied.

