using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediView.Imaging.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SopInstanceUidUniquePerStudy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_instances_sop_instance_uid",
                schema: "imaging",
                table: "instances");

            migrationBuilder.CreateIndex(
                name: "ix_instances_study_id_sop_instance_uid",
                schema: "imaging",
                table: "instances",
                columns: new[] { "study_id", "sop_instance_uid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_instances_study_id_sop_instance_uid",
                schema: "imaging",
                table: "instances");

            migrationBuilder.CreateIndex(
                name: "ix_instances_sop_instance_uid",
                schema: "imaging",
                table: "instances",
                column: "sop_instance_uid",
                unique: true);
        }
    }
}
