using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediView.Imaging.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "imaging");

            migrationBuilder.CreateTable(
                name: "instances",
                schema: "imaging",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_id = table.Column<Guid>(type: "uuid", nullable: false),
                    series_instance_uid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sop_instance_uid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    instance_number = table.Column<int>(type: "integer", nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instances", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_instances_sop_instance_uid",
                schema: "imaging",
                table: "instances",
                column: "sop_instance_uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_instances_study_id_series_instance_uid_instance_number",
                schema: "imaging",
                table: "instances",
                columns: new[] { "study_id", "series_instance_uid", "instance_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instances",
                schema: "imaging");
        }
    }
}
