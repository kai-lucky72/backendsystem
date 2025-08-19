using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWorkIdAndNullableManagerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FK on agents.manager_id if it exists (name-agnostic)
            migrationBuilder.Sql(@"
DECLARE @fk NVARCHAR(128);
SELECT @fk = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'agents') AND c.name = 'manager_id';
IF @fk IS NOT NULL EXEC('ALTER TABLE [agents] DROP CONSTRAINT [' + @fk + ']');
");

            // Drop users.work_id index if exists (by known name)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_work_id' AND object_id = OBJECT_ID('[users]'))
    DROP INDEX [IX_users_work_id] ON [users];
");

            // Drop ANY index that references users.work_id
            migrationBuilder.Sql(@"
DECLARE @dropIdxSql nvarchar(max) = N'';
SELECT @dropIdxSql = STRING_AGG('DROP INDEX [' + i.name + '] ON [users];', ' ')
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID(N'users') AND c.name = 'work_id';
IF @dropIdxSql IS NOT NULL AND LEN(@dropIdxSql) > 0 EXEC sp_executesql @dropIdxSql;
");

            // Drop users.work_id column if exists
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'work_id' AND Object_ID = Object_ID(N'users'))
    ALTER TABLE [users] DROP COLUMN [work_id];
");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "varchar(255)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)");

            migrationBuilder.AlterColumn<long>(
                name: "manager_id",
                table: "agents",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            // Null out orphaned manager references before adding FK
            migrationBuilder.Sql(@"
UPDATE a
SET a.manager_id = NULL
FROM agents a
LEFT JOIN managers m ON m.user_id = a.manager_id
WHERE m.user_id IS NULL;
");

            // Ensure unique index on users.phone_number (filtered) exists
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_phone_number' AND object_id = OBJECT_ID('[users]'))
    CREATE UNIQUE INDEX [IX_users_phone_number] ON [users] ([phone_number]) WHERE [phone_number] IS NOT NULL;
");

            migrationBuilder.AddForeignKey(
                name: "FK_agents_managers_manager_id",
                table: "agents",
                column: "manager_id",
                principalTable: "managers",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop FK if exists
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_agents_managers_manager_id')
    ALTER TABLE [agents] DROP CONSTRAINT [FK_agents_managers_manager_id];
");

            // Drop phone index if exists
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_phone_number' AND object_id = OBJECT_ID('[users]'))
    DROP INDEX [IX_users_phone_number] ON [users];
");

            migrationBuilder.AlterColumn<string>(
                name: "password_hash",
                table: "users",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true);

            // Re-add work_id column if needed (legacy)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'work_id' AND Object_ID = Object_ID(N'users'))
    ALTER TABLE [users] ADD [work_id] varchar(50) NOT NULL DEFAULT('');
");

            migrationBuilder.AlterColumn<long>(
                name: "manager_id",
                table: "agents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_users_work_id' AND object_id = OBJECT_ID('[users]'))
    CREATE UNIQUE INDEX [IX_users_work_id] ON [users] ([work_id]);
");

            migrationBuilder.AddForeignKey(
                name: "FK_agents_managers_manager_id",
                table: "agents",
                column: "manager_id",
                principalTable: "managers",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
