using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace PowerToChange.Modules.StaffRmb.Helpers
{
    public class Filesystem
    {
        public const string IMAGE_FOLDER = "/_RmbReceipts";

        public static void ensureFolderExists(int portalId)
        {
            FolderMappingInfo fm = FolderMappingController.Instance.GetFolderMapping(portalId, "Secure");
            if (!FolderManager.Instance.FolderExists(portalId, IMAGE_FOLDER + "/"))
            {
                IFolderInfo f1 = FolderManager.Instance.AddFolder(fm, IMAGE_FOLDER);
                DotNetNuke.Security.Roles.RoleController rc = new DotNetNuke.Security.Roles.RoleController();
                DotNetNuke.Security.Permissions.PermissionController pc = new DotNetNuke.Security.Permissions.PermissionController();
                DotNetNuke.Security.Permissions.PermissionInfo w = (DotNetNuke.Security.Permissions.PermissionInfo)pc.GetPermissionByCodeAndKey("SYSTEM_FOLDER", "WRITE")[0];
                FolderManager.Instance.SetFolderPermission(f1, w.PermissionID, rc.GetRoleByName(portalId, "Accounts Team").RoleID);
            }
        }

        public static IFolderInfo getImageFolder(int userId, int portalId)
        {
            FolderMappingInfo fm = FolderMappingController.Instance.GetFolderMapping(portalId, "Secure");
            string path = Filesystem.IMAGE_FOLDER + "/" + userId;
            // Clear the folder cache, to ensure we're getting the most up-to-date folder info
            DataCache.ClearFolderCache(portalId);
            if (FolderManager.Instance.FolderExists(portalId, path))
            {
                //if (!Directory.Exists(_view.Server.MapPath("~/portals/" + PS.PortalId + path)))
                //{
                //    Directory.CreateDirectory(_view.Server.MapPath("~/portals/" + PS.PortalId + path));
                //}
                return FolderManager.Instance.GetFolder(portalId, path);
            }
            else
            {
                return FolderManager.Instance.AddFolder(fm, path);
            }
        }

        public static void checkFolderPermissions(int PortalId, IFolderInfo theFolder, int theUserId, List<UserInfo> approvers)
        {
            // Get the write permission
            PermissionController pc = new PermissionController();
            PermissionInfo w = (PermissionInfo)pc.GetPermissionByCodeAndKey("SYSTEM_FOLDER", "WRITE")[0];
            // Get a list of all the folderPermissions we currently have
            FolderPermissionCollection folderPermissions = theFolder.FolderPermissions;
            // Set up the first permission
            FolderPermissionInfo permission = new FolderPermissionInfo();
            // Set up some default values for the permission
            initFolderPermission(permission, theFolder.FolderID, PortalId, w.PermissionID);
            // Set the user id to be this user
            permission.UserID = theUserId;
            // Add folder permissions, with a check for duplicates. 
            // This duplicate check (the 'True' parameter) will classify this as a "duplicate" if this permission
            // has the same PermissionID, UserID, and RoleID as a pre-existing one, and not add it if it is a duplicate
            folderPermissions.Add(permission, true);
            // Get all the possible approvers for this reimbursement
            try
            {
                foreach (var approver in approvers)
                {
                    // Create a new permission for this approver
                    permission = new FolderPermissionInfo();
                    // Initialize all the variables
                    initFolderPermission(permission, theFolder.FolderID, PortalId, w.PermissionID);
                    // Set the userid to the approver's id
                    permission.UserID = approver.UserID;
                    // Add permission for approver
                    folderPermissions.Add(permission, true);
                }
            }
            catch { }
            // Finally, add permissions for the accounts team:
            try
            {
                permission = new FolderPermissionInfo();
                // Initialize new folder permission
                initFolderPermission(permission, theFolder.FolderID, PortalId, w.PermissionID);
                // Set the role ID
                DotNetNuke.Security.Roles.RoleController rc = new DotNetNuke.Security.Roles.RoleController();
                permission.RoleID = rc.GetRoleByName(PortalId, "Accounts Team").RoleID;
                folderPermissions.Add(permission, true);
            }
            catch { }
            // Once we're finished adding these folder permissions, save it all
            FolderPermissionController.SaveFolderPermissions(theFolder);
        }

        private static void initFolderPermission(FolderPermissionInfo folderPermission, int FolderID, int PortalID, int PermissionID)
        {
            folderPermission.FolderID = FolderID;
            folderPermission.PortalID = PortalID;
            folderPermission.PermissionID = PermissionID;
            folderPermission.AllowAccess = true;
        }
    }
}