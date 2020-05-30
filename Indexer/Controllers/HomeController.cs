using Indexer.Helper;
using Indexer.Models;
using Ionic.Zip;
using SendIndexerToKindle.Helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Indexer.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View(new KeyValuePair<string, List<FileItem>>(string.Empty, new List<FileItem>()));
        }

        public ActionResult ListFiles()
        {
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult ListFiles(string filename)
        {
            List<FileItem> list = LuceneSearcher(filename);
            ViewBag.Value = filename;
            return View("Index", new KeyValuePair<string, List<FileItem>>(filename, list));
        }

        [HttpPost]
        public JsonResult SendToKindle(string hash)
        {
            return Json(SendToKindleHelper(hash), JsonRequestBehavior.AllowGet);
        }

        private static List<FileItem> LuceneSearcher(string filename)
        {
            var list = LuceneEngine.Search("Name", $"{filename.Trim()}*");
            list.AddRange(LuceneEngine.Search("Name", $"{filename.Trim()}"));
            list = list.GroupBy(t => t.Hash).Select(x => x.First()).ToList();
            return list;
        }

        public void GetFile(string hash)
        {
            FileItem file = LuceneEngine.Search("Hash", hash).First();
            GetFileByPath(file);
        }

        private void GetFileByPath(FileItem file)
        {
            string fileName = Path.GetFileName(file.Path);
            long length = new System.IO.FileInfo(file.Path).Length;
            Response.Clear();
            Response.ContentType = "application/octet-stream";
            Response.AppendHeader("Content-Disposition", "filename=" + fileName);
            Response.AppendHeader("content-length", length.ToString());
            Response.TransmitFile(file.Path);
            Response.End();
        }

        public bool SendToKindleHelper(string hash)
        {
            try
            {
                var smtphelper = new SmtpService();
                FileItem file = LuceneEngine.Search("Hash", hash).First();
                var mobiPath = ConvertToKindleHelper.ConvertToMobi(file.Path);
                smtphelper.SendMail(file.Name, file.Name, "destructer9@kindle.com", mobiPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public void GetAllAsZip(string filename)
        {
            HttpContext.Server.ScriptTimeout = 600;
            List<FileItem> list = LuceneSearcher(filename);
            var fileItem = new FileItem();

            if (Directory.Exists($"{ConfigurationManager.AppSettings["IndexPath"].Split(',')[0]}temp/"))
            {
                Directory.CreateDirectory($"{ConfigurationManager.AppSettings["IndexPath"].Split(',')[0]}temp/");
            }

            fileItem.Name = $"{DateTime.Now:ddMMyyyhhmmss}-{filename}.zip";
            fileItem.Path = $"{ConfigurationManager.AppSettings["IndexPath"].Split(',')[0]}temp/{DateTime.Now:ddMMyyyhhmmss}-{filename}.zip";

            using (ZipFile zip = new ZipFile())
            {
                foreach (FileItem item in list)
                {
                    zip.AddFile(item.Path, string.Empty);
                }

                zip.Save(fileItem.Path);
            }

            GetFileByPath(fileItem);

            if (System.IO.File.Exists(fileItem.Path))
            {
                System.IO.File.Delete(fileItem.Path);
            }

        }
    }
}