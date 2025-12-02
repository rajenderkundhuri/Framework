-- MySQL Initial Migration
-- Framework Database Schema
-- Version: 1.0.0
-- Generated: 2024

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- --------------------------------------------------------
-- Table: Users
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Users` (
    `Id` CHAR(36) NOT NULL,
    `UserName` VARCHAR(256) NULL,
    `NormalizedUserName` VARCHAR(256) NULL,
    `Email` VARCHAR(256) NULL,
    `NormalizedEmail` VARCHAR(256) NULL,
    `EmailConfirmed` TINYINT(1) NOT NULL DEFAULT 0,
    `PasswordHash` VARCHAR(500) NULL,
    `FirstName` VARCHAR(100) NULL,
    `LastName` VARCHAR(100) NULL,
    `PhoneNumber` VARCHAR(20) NULL,
    `PhoneNumberConfirmed` TINYINT(1) NOT NULL DEFAULT 0,
    `ProfilePictureUrl` VARCHAR(500) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `TwoFactorEnabled` TINYINT(1) NOT NULL DEFAULT 0,
    `RefreshToken` VARCHAR(500) NULL,
    `RefreshTokenExpiryTime` DATETIME(6) NULL,
    `TenantId` CHAR(36) NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(100) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `ModifiedBy` VARCHAR(100) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Users_NormalizedEmail` (`NormalizedEmail`),
    UNIQUE INDEX `IX_Users_NormalizedUserName` (`NormalizedUserName`),
    INDEX `IX_Users_TenantId` (`TenantId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: Roles
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Roles` (
    `Id` CHAR(36) NOT NULL,
    `Name` VARCHAR(100) NOT NULL,
    `NormalizedName` VARCHAR(100) NULL,
    `Description` VARCHAR(500) NULL,
    `IsDefault` TINYINT(1) NOT NULL DEFAULT 0,
    `IsStatic` TINYINT(1) NOT NULL DEFAULT 0,
    `IsSystem` TINYINT(1) NOT NULL DEFAULT 0,
    `TenantId` CHAR(36) NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(100) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `ModifiedBy` VARCHAR(100) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Roles_NormalizedName` (`NormalizedName`),
    INDEX `IX_Roles_TenantId` (`TenantId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: UserRoles
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `UserRoles` (
    `UserId` CHAR(36) NOT NULL,
    `RoleId` CHAR(36) NOT NULL,
    PRIMARY KEY (`UserId`, `RoleId`),
    CONSTRAINT `FK_UserRoles_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserRoles_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: RolePermissions
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `RolePermissions` (
    `Id` CHAR(36) NOT NULL,
    `RoleId` CHAR(36) NOT NULL,
    `Permission` VARCHAR(100) NOT NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(100) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `ModifiedBy` VARCHAR(100) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_RolePermissions_RoleId_Permission` (`RoleId`, `Permission`),
    CONSTRAINT `FK_RolePermissions_Roles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: UserProfiles
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `UserProfiles` (
    `Id` CHAR(36) NOT NULL,
    `UserId` CHAR(36) NOT NULL,
    `TimeZoneId` VARCHAR(100) NOT NULL DEFAULT 'UTC',
    `DateFormat` VARCHAR(50) NOT NULL DEFAULT 'yyyy-MM-dd',
    `TimeFormat` VARCHAR(50) NOT NULL DEFAULT 'HH:mm:ss',
    `CurrencyCode` VARCHAR(3) NOT NULL DEFAULT 'USD',
    `Locale` VARCHAR(20) NOT NULL DEFAULT 'en-US',
    `NumberFormatLocale` VARCHAR(20) NOT NULL DEFAULT 'en-US',
    `Theme` INT NOT NULL DEFAULT 0,
    `EmailNotificationsEnabled` TINYINT(1) NOT NULL DEFAULT 1,
    `PushNotificationsEnabled` TINYINT(1) NOT NULL DEFAULT 1,
    `PreferredTwoFactorMethod` INT NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(100) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `ModifiedBy` VARCHAR(100) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_UserProfiles_UserId` (`UserId`),
    CONSTRAINT `FK_UserProfiles_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: Tenants
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `Tenants` (
    `Id` CHAR(36) NOT NULL,
    `Name` VARCHAR(100) NOT NULL,
    `NormalizedName` VARCHAR(100) NULL,
    `Identifier` VARCHAR(50) NOT NULL,
    `ConnectionString` VARCHAR(500) NULL,
    `AdminEmail` VARCHAR(256) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `CreatedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(100) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `ModifiedBy` VARCHAR(100) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_Tenants_Identifier` (`Identifier`),
    INDEX `IX_Tenants_NormalizedName` (`NormalizedName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: TenantFeatures
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `TenantFeatures` (
    `Id` CHAR(36) NOT NULL,
    `TenantId` CHAR(36) NOT NULL,
    `FeatureName` VARCHAR(100) NOT NULL,
    `IsEnabled` TINYINT(1) NOT NULL DEFAULT 1,
    `Configuration` VARCHAR(2000) NULL,
    `ExpiresAt` DATETIME(6) NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(100) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `ModifiedBy` VARCHAR(100) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_TenantFeatures_TenantId_FeatureName` (`TenantId`, `FeatureName`),
    CONSTRAINT `FK_TenantFeatures_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: LocalizationResources
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `LocalizationResources` (
    `Id` CHAR(36) NOT NULL,
    `Key` VARCHAR(500) NOT NULL,
    `CultureName` VARCHAR(20) NOT NULL,
    `Value` VARCHAR(4000) NOT NULL,
    `Group` VARCHAR(100) NULL,
    `Description` VARCHAR(500) NULL,
    `IsSystem` TINYINT(1) NOT NULL DEFAULT 0,
    `TenantId` CHAR(36) NULL,
    `CreatedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(100) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `ModifiedBy` VARCHAR(100) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_LocalizationResources_Key_CultureName_TenantId` (`Key`(255), `CultureName`, `TenantId`),
    INDEX `IX_LocalizationResources_CultureName` (`CultureName`),
    INDEX `IX_LocalizationResources_Group` (`Group`),
    INDEX `IX_LocalizationResources_TenantId` (`TenantId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: SupportedLanguages
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `SupportedLanguages` (
    `Id` CHAR(36) NOT NULL,
    `CultureName` VARCHAR(20) NOT NULL,
    `DisplayName` VARCHAR(100) NOT NULL,
    `NativeName` VARCHAR(100) NOT NULL,
    `FlagCode` VARCHAR(10) NULL,
    `IsEnabled` TINYINT(1) NOT NULL DEFAULT 1,
    `IsDefault` TINYINT(1) NOT NULL DEFAULT 0,
    `IsRtl` TINYINT(1) NOT NULL DEFAULT 0,
    `SortOrder` INT NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME(6) NOT NULL,
    `CreatedBy` VARCHAR(100) NULL,
    `ModifiedAt` DATETIME(6) NULL,
    `ModifiedBy` VARCHAR(100) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_SupportedLanguages_CultureName` (`CultureName`),
    INDEX `IX_SupportedLanguages_IsDefault` (`IsDefault`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: AuditLogs
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `AuditLogs` (
    `Id` CHAR(36) NOT NULL,
    `UserId` VARCHAR(100) NULL,
    `UserName` VARCHAR(256) NULL,
    `Action` INT NOT NULL,
    `EntityType` VARCHAR(100) NOT NULL,
    `EntityId` VARCHAR(100) NULL,
    `OldValues` LONGTEXT NULL,
    `NewValues` LONGTEXT NULL,
    `AffectedColumns` LONGTEXT NULL,
    `IpAddress` VARCHAR(50) NULL,
    `UserAgent` VARCHAR(500) NULL,
    `ServiceName` VARCHAR(200) NULL,
    `MethodName` VARCHAR(200) NULL,
    `Success` TINYINT(1) NOT NULL DEFAULT 1,
    `ErrorMessage` VARCHAR(2000) NULL,
    `Timestamp` DATETIME(6) NOT NULL,
    `TenantId` CHAR(36) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_AuditLogs_Timestamp` (`Timestamp`),
    INDEX `IX_AuditLogs_UserId` (`UserId`),
    INDEX `IX_AuditLogs_TenantId` (`TenantId`),
    INDEX `IX_AuditLogs_EntityType_EntityId` (`EntityType`, `EntityId`),
    INDEX `IX_AuditLogs_Action` (`Action`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------
-- Table: __EFMigrationsHistory (EF Core migrations tracking)
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` VARCHAR(150) NOT NULL,
    `ProductVersion` VARCHAR(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET FOREIGN_KEY_CHECKS = 1;

-- --------------------------------------------------------
-- Seed Data: Default Languages
-- --------------------------------------------------------
INSERT INTO `SupportedLanguages` (`Id`, `CultureName`, `DisplayName`, `NativeName`, `FlagCode`, `IsEnabled`, `IsDefault`, `IsRtl`, `SortOrder`, `CreatedAt`)
VALUES
    (UUID(), 'en-US', 'English (United States)', 'English', 'us', 1, 1, 0, 1, NOW()),
    (UUID(), 'en-GB', 'English (United Kingdom)', 'English', 'gb', 1, 0, 0, 2, NOW()),
    (UUID(), 'es-ES', 'Spanish (Spain)', 'Espa\u00f1ol', 'es', 1, 0, 0, 3, NOW()),
    (UUID(), 'fr-FR', 'French (France)', 'Fran\u00e7ais', 'fr', 1, 0, 0, 4, NOW()),
    (UUID(), 'de-DE', 'German (Germany)', 'Deutsch', 'de', 1, 0, 0, 5, NOW()),
    (UUID(), 'pt-BR', 'Portuguese (Brazil)', 'Portugu\u00eas', 'br', 1, 0, 0, 6, NOW()),
    (UUID(), 'zh-CN', 'Chinese (Simplified)', '\u7b80\u4f53\u4e2d\u6587', 'cn', 1, 0, 0, 7, NOW()),
    (UUID(), 'ja-JP', 'Japanese', '\u65e5\u672c\u8a9e', 'jp', 1, 0, 0, 8, NOW()),
    (UUID(), 'ar-SA', 'Arabic (Saudi Arabia)', '\u0627\u0644\u0639\u0631\u0628\u064a\u0629', 'sa', 1, 0, 1, 9, NOW())
ON DUPLICATE KEY UPDATE `Id` = `Id`;

-- Record migration
INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20241201000000_InitialCreate', '9.0.0')
ON DUPLICATE KEY UPDATE `MigrationId` = `MigrationId`;
