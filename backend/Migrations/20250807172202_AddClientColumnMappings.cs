using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddClientColumnMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The database already has the correct column names (paying_amount, full_name, etc.)
            // This migration just acknowledges the column mappings in the ApplicationDbContext
            // No actual database changes are needed since the table was created with SQL scripts
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No changes to revert since no actual database changes were made
        }
    }
}
