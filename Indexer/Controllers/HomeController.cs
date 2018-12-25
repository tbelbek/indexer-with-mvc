﻿using Indexer.Models;
using Ionic.Zip;
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

        [HttpPost]
        public ActionResult ListFiles(string filename)
        {
            List<FileItem> list = LuceneSearcher(filename);
            ViewBag.Value = filename;
            return View("Index", new KeyValuePair<string, List<FileItem>>(filename, list));
        }

        private static List<FileItem> LuceneSearcher(string filename)
        {
            var list = LuceneEngine.Search("Name", $"{filename}*");
            list.AddRange(LuceneEngine.Search("Name", $"{filename}"));
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

        public void GetAllAsZip(string filename)
        {
            HttpContext.Server.ScriptTimeout = 600;
            List<FileItem> list = LuceneSearcher(filename);
            var fileItem = new FileItem();

            if (Directory.Exists($"{ConfigurationManager.AppSettings["IndexPath"].Split(',')[0]}temp/"))
            {
                Directory.CreateDirectory($"{ConfigurationManager.AppSettings["IndexPath"].Split(',')[0]}temp/");
            }

            fileItem.Name = $"{DateTime.Now.ToString("ddMMyyyhhmmss")}-{filename}.zip";
            fileItem.Path = $"{ConfigurationManager.AppSettings["IndexPath"].Split(',')[0]}temp/{DateTime.Now.ToString("ddMMyyyhhmmss")}-{filename}.zip";

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