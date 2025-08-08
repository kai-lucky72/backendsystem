using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class FixPasswordHashColumnSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alter the password_hash column to increase its size
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN password_hash VARCHAR(255) NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert the password_hash column back to its original size
            migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN password_hash VARCHAR(60) NOT NULL");
        }
    }
}
