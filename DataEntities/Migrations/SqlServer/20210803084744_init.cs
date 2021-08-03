using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DataEntities.Migrations.SqlServer
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModified = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itemss", x => x.Id);
                });


            migrationBuilder.Sql("ALTER DATABASE CURRENT SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 14 DAYS, AUTO_CLEANUP = ON)", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER DATABASE CURRENT SET CHANGE_TRACKING = OFF (CHANGE_RETENTION = 14 DAYS, AUTO_CLEANUP = ON)", true);

            migrationBuilder.DropTable(
                name: "Items");
        }
    }
}
