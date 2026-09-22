using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MediView.Studies.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "studies");

            migrationBuilder.CreateSequence(
                name: "study_number_seq",
                schema: "studies",
                startValue: 1000L);

            migrationBuilder.CreateTable(
                name: "studies",
                schema: "studies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValueSql: "'STU-' || nextval('studies.study_number_seq')"),
                    patient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    patient_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    patient_mrn = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    doctor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    doctor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    scheduled_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    priority = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    images_imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_studies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_status_history",
                schema: "studies",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    from_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    to_status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    study_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_study_status_history_studies_study_id",
                        column: x => x.study_id,
                        principalSchema: "studies",
                        principalTable: "studies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_studies_doctor_id_scheduled_start",
                schema: "studies",
                table: "studies",
                columns: new[] { "doctor_id", "scheduled_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_studies_patient_id",
                schema: "studies",
                table: "studies",
                column: "patient_id");

            migrationBuilder.CreateIndex(
                name: "ix_studies_study_number",
                schema: "studies",
                table: "studies",
                column: "study_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_study_status_history_study_id_changed_at",
                schema: "studies",
                table: "study_status_history",
                columns: new[] { "study_id", "changed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "study_status_history",
                schema: "studies");

            migrationBuilder.DropTable(
                name: "studies",
                schema: "studies");

            migrationBuilder.DropSequence(
                name: "study_number_seq",
                schema: "studies");
        }
    }
}
