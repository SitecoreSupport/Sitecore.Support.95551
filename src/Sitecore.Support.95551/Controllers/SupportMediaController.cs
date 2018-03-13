namespace Sitecore.Support.Controllers
{
    using Sitecore;
    using Sitecore.Configuration;
    using Sitecore.Controllers;
    using Sitecore.Controllers.Results;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Data.Managers;
    using Sitecore.Diagnostics;
    using Sitecore.Resources.Media;
    using Sitecore.Support.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Web;
    using System.Web.Mvc;

    [ShellSite, Authorize]
    public class SupportMediaController : Controller
    {
        private JsonResult DoUpload(string database, string destinationUrl)
        {
            if (string.IsNullOrEmpty(destinationUrl))
            {
                destinationUrl = "/sitecore/media library";
            }
            List<UploadedFileItem> list = new List<UploadedFileItem>();
            SitecoreViewModelResult result = new SitecoreViewModelResult();

            #region Addded code
            if (!ValidateDestination(database, destinationUrl, result))
            {
                this.Response.StatusCode = new HttpStatusCodeResult(HttpStatusCode.Forbidden).StatusCode;                
                this.Response.StatusDescription = result.Result.errorItems[0].Message;
                this.Response.TrySkipIisCustomErrors = true;
                return result;
            }
            #endregion

            foreach (string str in base.Request.Files)
            {
                HttpPostedFileBase file = base.Request.Files[str];
                if (file != null)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.FileName);
                    if (!string.IsNullOrEmpty(base.Request.Form["name"]))
                    {
                        fileNameWithoutExtension = base.Request.Form["name"];
                    }
                    fileNameWithoutExtension = ItemUtil.ProposeValidItemName(fileNameWithoutExtension, "default");
                    string str3 = string.Empty;
                    if (!string.IsNullOrEmpty(base.Request.Form["alternate"]))
                    {
                        str3 = base.Request.Form["alternate"];
                    }
                    Database contentDatabase = Context.ContentDatabase;
                    if (!string.IsNullOrEmpty(database))
                    {
                        contentDatabase = Factory.GetDatabase(database);
                    }
                    if (contentDatabase == null)
                    {
                        contentDatabase = Context.ContentDatabase;
                    }
                    MediaCreatorOptions options = new MediaCreatorOptions
                    {
                        AlternateText = str3,
                        Database = contentDatabase,
                        FileBased = Settings.Media.UploadAsFiles,
                        IncludeExtensionInItemName = Settings.Media.IncludeExtensionsInItemNames,
                        KeepExisting = true,
                        Language = LanguageManager.DefaultLanguage,
                        Versioned = Settings.Media.UploadAsVersionableByDefault,
                        Destination = this.ParseDestinationUrl(destinationUrl) + fileNameWithoutExtension
                    };
                    if (!ValidateFile(file, result))
                    {
                        return result;
                    }
                    Item innerItem = MediaManager.Creator.CreateFromStream(file.InputStream, "/upload/" + file.FileName, options);
                    if (!string.IsNullOrEmpty(base.Request.Form["description"]))
                    {
                        innerItem.Editing.BeginEdit();
                        innerItem["Description"] = base.Request.Form["description"];
                        innerItem.Editing.EndEdit();
                    }
                    MediaItem item = new MediaItem(innerItem);
                    MediaUrlOptions options2 = new MediaUrlOptions(130, 130, false)
                    {
                        Thumbnail = true,
                        BackgroundColor = Color.Transparent,
                        Database = item.Database
                    };
                    string mediaUrl = MediaManager.GetMediaUrl(item, options2);
                    list.Add(new UploadedFileItem(innerItem.Name, innerItem.ID.ToString(), innerItem.ID.ToShortID().ToString(), mediaUrl));
                }
            }
            
            ((dynamic)result.Result).uploadedFileItems = list;

            return result;
        }

        private string ParseDestinationUrl(string destinationUrl)
        {
            if (!destinationUrl.EndsWith("/"))
            {
                destinationUrl = destinationUrl + "/";
            }
            return destinationUrl;
        }

        [ValidateAntiForgeryToken, HttpPost]
        public JsonResult Upload(string database, string destinationUrl)
        {
            try
            {
                return this.DoUpload(database, destinationUrl);
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message, exception, this);
                SitecoreViewModelResult result = new SitecoreViewModelResult();
                List<ErrorItem> list = new List<ErrorItem> {
                    new ErrorItem("exception", exception.GetType().ToString(), exception.Message)
                };
                ((dynamic)result.Result).errorItems = list;
                return result;
            }
        }

        private static bool ValidateDestination(string database, string destinationUrl, SitecoreViewModelResult result)
        {
            List<ErrorItem> list = new List<ErrorItem>();
            bool flag = true;
            Database contentDatabase = ClientHost.Databases.ContentDatabase;
            if (!string.IsNullOrEmpty(database))
            {
                contentDatabase = Factory.GetDatabase(database);
            }
            Item item = contentDatabase.GetItem(destinationUrl);
            if ((item == null) || !item.Access.CanCreate())
            {
                list.Add(new ErrorItem("destination", destinationUrl, ClientHost.Globalization.Translate("You do not have permission to upload files to the currently selected folder.")));
                flag = false;
            }
            if (!flag)
            {
                ((dynamic)result.Result).errorItems = list;
            }
            return flag;
        }

        private static bool ValidateFile(HttpPostedFileBase file, SitecoreViewModelResult result)
        {
            List<ErrorItem> list = new List<ErrorItem>();
            int contentLength = file.ContentLength;
            bool flag = true;
            if (contentLength > Settings.Media.MaxSizeInDatabase)
            {
                list.Add(new ErrorItem("size", contentLength.ToString(), string.Format(ClientHost.Globalization.Translate("The file exceeds the maximum size ({0})."), Settings.Media.MaxSizeInDatabase)));
                flag = false;
            }
            if (!flag)
            {
                ((dynamic)result.Result).errorItems = list;
            }
            return flag;
        }
    }
}