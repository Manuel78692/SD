using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SERVIDOR.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateSeparateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SensorReading",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WavyId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SensorType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorReading", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GpsReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Latitude = table.Column<double>(type: "REAL", precision: 10, scale: 7, nullable: false),
                    Longitude = table.Column<double>(type: "REAL", precision: 10, scale: 7, nullable: false),
                    Altitude = table.Column<double>(type: "REAL", precision: 8, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GpsReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GpsReadings_SensorReading_Id",
                        column: x => x.Id,
                        principalTable: "SensorReading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GyroReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    X = table.Column<double>(type: "REAL", precision: 8, scale: 4, nullable: false),
                    Y = table.Column<double>(type: "REAL", precision: 8, scale: 4, nullable: false),
                    Z = table.Column<double>(type: "REAL", precision: 8, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GyroReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GyroReadings_SensorReading_Id",
                        column: x => x.Id,
                        principalTable: "SensorReading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HumidityReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumidityReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HumidityReadings_SensorReading_Id",
                        column: x => x.Id,
                        principalTable: "SensorReading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<double>(type: "REAL", precision: 4, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhReadings_SensorReading_Id",
                        column: x => x.Id,
                        principalTable: "SensorReading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemperatureReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<double>(type: "REAL", precision: 5, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemperatureReadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemperatureReadings_SensorReading_Id",
                        column: x => x.Id,
                        principalTable: "SensorReading",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GpsReading_Location",
                table: "GpsReadings",
                columns: new[] { "Latitude", "Longitude" });

            migrationBuilder.CreateIndex(
                name: "IX_HumidityReading_Value",
                table: "HumidityReadings",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_PhReading_Value",
                table: "PhReadings",
                column: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReading_Timestamp",
                table: "SensorReading",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReading_WavyId",
                table: "SensorReading",
                column: "WavyId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReading_WavyId_Timestamp",
                table: "SensorReading",
                columns: new[] { "WavyId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TemperatureReading_Value",
                table: "TemperatureReadings",
                column: "Value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GpsReadings");

            migrationBuilder.DropTable(
                name: "GyroReadings");

            migrationBuilder.DropTable(
                name: "HumidityReadings");

            migrationBuilder.DropTable(
                name: "PhReadings");

            migrationBuilder.DropTable(
                name: "TemperatureReadings");

            migrationBuilder.DropTable(
                name: "SensorReading");
        }
    }
}
