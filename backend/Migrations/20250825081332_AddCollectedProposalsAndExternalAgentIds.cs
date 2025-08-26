using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectedProposalsAndExternalAgentIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_distribution_channel_id",
                table: "agents",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_user_id",
                table: "agents",
                type: "varchar(50)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "collected_proposals",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    proposal_number = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    customer_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    customer_name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    proposal_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    premium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    risk_premium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    savings_premium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    total_premium = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    premium_frequency = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    payment_mode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    institutions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    due_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    converted = table.Column<bool>(type: "bit", nullable: true),
                    converted_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    fetched_at_utc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_collected_proposals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_collected_proposals_agent_id_proposal_date",
                table: "collected_proposals",
                columns: new[] { "agent_id", "proposal_date" });

            migrationBuilder.CreateIndex(
                name: "IX_collected_proposals_proposal_number",
                table: "collected_proposals",
                column: "proposal_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "collected_proposals");

            migrationBuilder.DropColumn(
                name: "external_distribution_channel_id",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "external_user_id",
                table: "agents");
        }
    }
}
