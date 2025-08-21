using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveManagerCreatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Conditionally drop FK, index and column if they exist
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_managers_users_created_by'
)
BEGIN
    ALTER TABLE [managers] DROP CONSTRAINT [FK_managers_users_created_by];
END;

IF EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_managers_created_by' AND object_id = OBJECT_ID('managers')
)
BEGIN
    DROP INDEX [IX_managers_created_by] ON [managers];
END;

-- Drop legacy index if present
IF EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'fk_managers_creator' AND object_id = OBJECT_ID('managers')
)
BEGIN
    DROP INDEX [fk_managers_creator] ON [managers];
END;

IF EXISTS (
    SELECT 1 FROM sys.columns WHERE Name = N'created_by' AND Object_ID = Object_ID(N'managers')
)
BEGIN
    ALTER TABLE [managers] DROP COLUMN [created_by];
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "created_by",
                table: "managers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_managers_created_by",
                table: "managers",
                column: "created_by");

            migrationBuilder.AddForeignKey(
                name: "FK_managers_users_created_by",
                table: "managers",
                column: "created_by",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
