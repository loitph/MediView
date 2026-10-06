using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediView.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PatientMrnDashPrefix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "mrn",
                schema: "identity",
                table: "patients",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValueSql: "'MRN-' || nextval('identity.patient_mrn_seq')",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValueSql: "'MRN' || nextval('identity.patient_mrn_seq')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "mrn",
                schema: "identity",
                table: "patients",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValueSql: "'MRN' || nextval('identity.patient_mrn_seq')",
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldDefaultValueSql: "'MRN-' || nextval('identity.patient_mrn_seq')");
        }
    }
}
