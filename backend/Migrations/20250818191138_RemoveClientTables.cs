using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClientTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "clients_collected");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    agent_id = table.Column<long>(type: "bigint", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    collected_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    collected_by_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    contract_years = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    full_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    insurance_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    national_id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    paying_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    paying_method = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone_number = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clients_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "clients_collected",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    client_data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    collected_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clients_collected", x => x.Id);
                    table.ForeignKey(
                        name: "FK_clients_collected_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_clients_agent_id",
                table: "clients",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_clients_national_id",
                table: "clients",
                column: "national_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clients_phone_number",
                table: "clients",
                column: "phone_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_clients_collected_agent_id",
                table: "clients_collected",
                column: "agent_id");
        }
    }
}
