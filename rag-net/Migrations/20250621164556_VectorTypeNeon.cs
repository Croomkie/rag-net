﻿using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace rag_net.Migrations
{
    /// <inheritdoc />
    public partial class VectorTypeNeon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AlterColumn<Vector>(
                name: "Embedding",
                table: "EmbeddingChunks",
                type: "vector(1536)",
                nullable: false,
                oldClrType: typeof(float[]),
                oldType: "real[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AlterColumn<float[]>(
                name: "Embedding",
                table: "EmbeddingChunks",
                type: "real[]",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(1536)");
        }
    }
}
