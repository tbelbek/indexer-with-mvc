using Indexer.Models;
using Ionic.Zip;
using System;
using System.Collections.Generic;
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
            return View("Index", new KeyValuePair<string, List<FileItem>>(filename, list));
        }

        private static List<FileItem> LuceneSearcher(string filename)
        {
            var list = LuceneEngine.Search("Name", $"{filename}*");
            list.AddRange(LuceneEngine.Search("Name", $"{filename}"));
            list = list.GroupBy(t => t.Hash).Select(x => x.First()).ToList();
            return list;
        }

        public FileResult GetFile(string hash)
        {
            FileItem file = LuceneEngine.Search("Hash", hash).First();
            byte[] fileBytes = System.IO.File.ReadAllBytes(file.Path);
            string fileName = Path.GetFileName(file.Path);
            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public FileStreamResult GetAllAsZip(string filename)
        {
            List<FileItem> list = LuceneSearcher(filename);
            var stream = new MemoryStream();
            using (ZipFile zip = new ZipFile($"{DateTime.Now.ToString("ddMMyyyhhmmss.zip")}"))
            {
                foreach (FileItem item in list)
                {
                    zip.AddFile(item.Path, string.Empty);
                }

                zip.Save(stream);
            }

            stream.Seek(0, SeekOrigin.Begin);
            return File(stream, System.Net.Mime.MediaTypeNames.Application.Octet, $"{DateTime.Now.ToString("ddMMyyyhhmmss")}-{filename}.zip");

        }
    }
}