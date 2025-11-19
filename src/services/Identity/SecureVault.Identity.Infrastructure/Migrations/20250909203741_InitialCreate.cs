using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVault.Identity.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                public_key = table.Column<byte[]>(type: "bytea", nullable: false),
                salt = table.Column<byte[]>(type: "bytea", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                user_info = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "user_recovery_data",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                recovery_data = table.Column<byte[]>(type: "bytea", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_recovery_data", x => x.user_id);
                table.ForeignKey(
                    name: "fk_user_recovery_data_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_sessions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                refresh_token_jti = table.Column<string>(type: "text", nullable: false),
                access_token_jti = table.Column<string>(type: "text", nullable: false),
                is_persistent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                is_revoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                device_details = table.Column<string>(type: "jsonb", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_sessions", x => x.id);
                table.ForeignKey(
                    name: "fk_user_sessions_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_user_sessions_refresh_token_jti",
            table: "user_sessions",
            column: "refresh_token_jti");

        migrationBuilder.CreateIndex(
            name: "ix_user_sessions_user_id",
            table: "user_sessions",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_users_email",
            table: "users",
            column: "email",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "user_recovery_data");

        migrationBuilder.DropTable(
            name: "user_sessions");

        migrationBuilder.DropTable(
            name: "users");
    }
}
