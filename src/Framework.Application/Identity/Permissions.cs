namespace Framework.Application.Identity;

/// <summary>
/// Application permission constants
/// </summary>
public static class Permissions
{
    // User management
    public const string UsersView = "Users.View";
    public const string UsersCreate = "Users.Create";
    public const string UsersEdit = "Users.Edit";
    public const string UsersDelete = "Users.Delete";
    public const string UsersManageRoles = "Users.ManageRoles";
    public const string UsersResetPassword = "Users.ResetPassword";
    public const string UsersImpersonate = "Users.Impersonate";

    // Role management
    public const string RolesView = "Roles.View";
    public const string RolesCreate = "Roles.Create";
    public const string RolesEdit = "Roles.Edit";
    public const string RolesDelete = "Roles.Delete";
    public const string RolesManagePermissions = "Roles.ManagePermissions";

    // Permission management
    public const string PermissionsView = "Permissions.View";
    public const string PermissionsAssign = "Permissions.Assign";

    // Tenant management
    public const string TenantsView = "Tenants.View";
    public const string TenantsCreate = "Tenants.Create";
    public const string TenantsEdit = "Tenants.Edit";
    public const string TenantsDelete = "Tenants.Delete";
    public const string TenantsManageFeatures = "Tenants.ManageFeatures";
    public const string TenantsManageUsers = "Tenants.ManageUsers";

    // System administration
    public const string SystemSettings = "System.Settings";
    public const string SystemAuditLogs = "System.AuditLogs";
    public const string SystemBackup = "System.Backup";
    public const string SystemHealthCheck = "System.HealthCheck";

    // Email Templates
    public const string EmailTemplatesView = "EmailTemplates.View";
    public const string EmailTemplatesCreate = "EmailTemplates.Create";
    public const string EmailTemplatesEdit = "EmailTemplates.Edit";
    public const string EmailTemplatesDelete = "EmailTemplates.Delete";
    public const string EmailLogsView = "EmailLogs.View";

    // Notifications
    public const string NotificationsView = "Notifications.View";
    public const string NotificationsCreate = "Notifications.Create";
    public const string NotificationsManage = "Notifications.Manage";

    // API Keys
    public const string ApiKeysView = "ApiKeys.View";
    public const string ApiKeysCreate = "ApiKeys.Create";
    public const string ApiKeysEdit = "ApiKeys.Edit";
    public const string ApiKeysDelete = "ApiKeys.Delete";

    // Background Jobs
    public const string BackgroundJobsView = "BackgroundJobs.View";
    public const string BackgroundJobsManage = "BackgroundJobs.Manage";
    public const string BackgroundJobsDelete = "BackgroundJobs.Delete";

    /// <summary>
    /// Get all defined permissions
    /// </summary>
    public static IEnumerable<string> GetAll()
    {
        return new[]
        {
            UsersView, UsersCreate, UsersEdit, UsersDelete, UsersManageRoles, UsersResetPassword, UsersImpersonate,
            RolesView, RolesCreate, RolesEdit, RolesDelete, RolesManagePermissions,
            PermissionsView, PermissionsAssign,
            TenantsView, TenantsCreate, TenantsEdit, TenantsDelete, TenantsManageFeatures, TenantsManageUsers,
            SystemSettings, SystemAuditLogs, SystemBackup, SystemHealthCheck,
            EmailTemplatesView, EmailTemplatesCreate, EmailTemplatesEdit, EmailTemplatesDelete, EmailLogsView,
            NotificationsView, NotificationsCreate, NotificationsManage,
            ApiKeysView, ApiKeysCreate, ApiKeysEdit, ApiKeysDelete,
            BackgroundJobsView, BackgroundJobsManage, BackgroundJobsDelete
        };
    }

    /// <summary>
    /// Get permissions by module
    /// </summary>
    public static IEnumerable<PermissionGroup> GetGrouped()
    {
        return new[]
        {
            new PermissionGroup("Users", "User Management", new[]
            {
                UsersView, UsersCreate, UsersEdit, UsersDelete, UsersManageRoles, UsersResetPassword, UsersImpersonate
            }),
            new PermissionGroup("Roles", "Role Management", new[]
            {
                RolesView, RolesCreate, RolesEdit, RolesDelete, RolesManagePermissions
            }),
            new PermissionGroup("Permissions", "Permission Management", new[]
            {
                PermissionsView, PermissionsAssign
            }),
            new PermissionGroup("Tenants", "Tenant Management", new[]
            {
                TenantsView, TenantsCreate, TenantsEdit, TenantsDelete, TenantsManageFeatures, TenantsManageUsers
            }),
            new PermissionGroup("System", "System Administration", new[]
            {
                SystemSettings, SystemAuditLogs, SystemBackup, SystemHealthCheck
            }),
            new PermissionGroup("EmailTemplates", "Email Templates", new[]
            {
                EmailTemplatesView, EmailTemplatesCreate, EmailTemplatesEdit, EmailTemplatesDelete, EmailLogsView
            }),
            new PermissionGroup("Notifications", "Notifications", new[]
            {
                NotificationsView, NotificationsCreate, NotificationsManage
            }),
            new PermissionGroup("ApiKeys", "API Keys", new[]
            {
                ApiKeysView, ApiKeysCreate, ApiKeysEdit, ApiKeysDelete
            }),
            new PermissionGroup("BackgroundJobs", "Background Jobs", new[]
            {
                BackgroundJobsView, BackgroundJobsManage, BackgroundJobsDelete
            })
        };
    }

    /// <summary>
    /// Get permission display name
    /// </summary>
    public static string GetDisplayName(string permission)
    {
        return permission switch
        {
            UsersView => "View Users",
            UsersCreate => "Create Users",
            UsersEdit => "Edit Users",
            UsersDelete => "Delete Users",
            UsersManageRoles => "Manage User Roles",
            UsersResetPassword => "Reset Passwords",
            UsersImpersonate => "Impersonate Users",

            RolesView => "View Roles",
            RolesCreate => "Create Roles",
            RolesEdit => "Edit Roles",
            RolesDelete => "Delete Roles",
            RolesManagePermissions => "Manage Role Permissions",

            PermissionsView => "View Permissions",
            PermissionsAssign => "Assign Permissions",

            TenantsView => "View Tenants",
            TenantsCreate => "Create Tenants",
            TenantsEdit => "Edit Tenants",
            TenantsDelete => "Delete Tenants",
            TenantsManageFeatures => "Manage Tenant Features",
            TenantsManageUsers => "Manage Tenant Users",

            SystemSettings => "System Settings",
            SystemAuditLogs => "View Audit Logs",
            SystemBackup => "System Backup",
            SystemHealthCheck => "Health Checks",

            EmailTemplatesView => "View Email Templates",
            EmailTemplatesCreate => "Create Email Templates",
            EmailTemplatesEdit => "Edit Email Templates",
            EmailTemplatesDelete => "Delete Email Templates",
            EmailLogsView => "View Email Logs",

            NotificationsView => "View Notifications",
            NotificationsCreate => "Create Notifications",
            NotificationsManage => "Manage Notifications",

            ApiKeysView => "View API Keys",
            ApiKeysCreate => "Create API Keys",
            ApiKeysEdit => "Edit API Keys",
            ApiKeysDelete => "Delete API Keys",

            BackgroundJobsView => "View Background Jobs",
            BackgroundJobsManage => "Manage Background Jobs",
            BackgroundJobsDelete => "Delete Background Jobs",

            _ => permission
        };
    }

    /// <summary>
    /// Get permission description
    /// </summary>
    public static string GetDescription(string permission)
    {
        return permission switch
        {
            UsersView => "View user list and details",
            UsersCreate => "Create new user accounts",
            UsersEdit => "Edit existing user information",
            UsersDelete => "Delete user accounts",
            UsersManageRoles => "Assign and remove roles from users",
            UsersResetPassword => "Reset user passwords",
            UsersImpersonate => "Login as another user for troubleshooting",

            RolesView => "View roles and their configurations",
            RolesCreate => "Create new roles",
            RolesEdit => "Edit existing roles",
            RolesDelete => "Delete roles",
            RolesManagePermissions => "Add or remove permissions from roles",

            PermissionsView => "View available permissions",
            PermissionsAssign => "Assign permissions to roles",

            TenantsView => "View tenant list and details",
            TenantsCreate => "Create new tenants",
            TenantsEdit => "Edit tenant settings",
            TenantsDelete => "Delete tenants",
            TenantsManageFeatures => "Enable/disable features per tenant",
            TenantsManageUsers => "Manage users within tenants",

            SystemSettings => "Configure system-wide settings",
            SystemAuditLogs => "View system audit logs",
            SystemBackup => "Create and restore system backups",
            SystemHealthCheck => "View system health status",

            EmailTemplatesView => "View email templates list and details",
            EmailTemplatesCreate => "Create new email templates",
            EmailTemplatesEdit => "Edit existing email templates",
            EmailTemplatesDelete => "Delete email templates",
            EmailLogsView => "View email sending logs and history",

            NotificationsView => "View notifications",
            NotificationsCreate => "Create and send notifications",
            NotificationsManage => "Manage and delete notifications",

            ApiKeysView => "View API keys list and details",
            ApiKeysCreate => "Create new API keys",
            ApiKeysEdit => "Edit existing API keys",
            ApiKeysDelete => "Delete and revoke API keys",

            BackgroundJobsView => "View background job list and status",
            BackgroundJobsManage => "Manage background jobs (requeue, trigger)",
            BackgroundJobsDelete => "Delete background jobs",

            _ => string.Empty
        };
    }
}

/// <summary>
/// Permission group for UI display
/// </summary>
public record PermissionGroup(string Name, string DisplayName, IEnumerable<string> Permissions);
