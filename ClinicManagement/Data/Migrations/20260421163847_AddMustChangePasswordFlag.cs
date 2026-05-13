using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMustChangePasswordFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @hasColumn := (
                    SELECT COUNT(*)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'Users'
                      AND column_name = 'MustChangePassword'
                );
                SET @sql := IF(
                    @hasColumn = 0,
                    'ALTER TABLE `Users` ADD COLUMN `MustChangePassword` tinyint(1) NOT NULL DEFAULT FALSE;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET @hasColumn := (
                    SELECT COUNT(*)
                    FROM information_schema.columns
                    WHERE table_schema = DATABASE()
                      AND table_name = 'Users'
                      AND column_name = 'MustChangePassword'
                );
                SET @sql := IF(
                    @hasColumn = 1,
                    'ALTER TABLE `Users` DROP COLUMN `MustChangePassword`;',
                    'SELECT 1;'
                );
                PREPARE stmt FROM @sql;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
                """);
        }
    }
}
