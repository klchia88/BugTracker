using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public static class FileUploadValidator
    {
        public static bool IsWebFriendlyImage(HttpPostedFileBase file)
        {
            if (file == null)
                return false;

            if (file.ContentLength > 5 * 1024 * 1024 || file.ContentLength < 1024)
            {
                return false;
            }
            try
            {
                var uploadedFile = file.FileName;
                var extension = Path.GetExtension(uploadedFile);
                return  extension.Equals(".csv") || 
                        extension.Equals(".xls") || 
                        extension.Equals(".xlsx") ||
                        extension.Equals(".doc") ||
                        extension.Equals(".docx") ||
                        extension.Equals(".jpg") ||
                        extension.Equals(".png") ||
                        extension.Equals(".txt") ||
                        extension.Equals(".pdf");
            }
            catch
            {
                return false;
            }
        }
    }
}
