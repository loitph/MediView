using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediView.Reporting.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "reports",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doctor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    findings = table.Column<string>(type: "text", nullable: true),
                    impression = table.Column<string>(type: "text", nullable: true),
                    decision = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    finalized_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_medications",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    drug_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    dosage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    frequency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    duration = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_report_medications", x => x.id);
                    table.ForeignKey(
                        name: "fk_report_medications_reports_report_id",
                        column: x => x.report_id,
                        principalSchema: "reporting",
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_medications_report_id",
                schema: "reporting",
                table: "report_medications",
                column: "report_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_doctor_id_status",
                schema: "reporting",
                table: "reports",
                columns: new[] { "doctor_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_reports_study_id",
                schema: "reporting",
                table: "reports",
                column: "study_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_medications",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "reports",
                schema: "reporting");
        }
    }
}
