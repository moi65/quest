﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using Ionic.Zip;
using TextAdventures.Quest;
using System.IO;
using WebEditor.Models;

namespace WebEditor.Controllers
{
    public class EditController : Controller
    {
        //
        // GET: /Edit/Game/1

        public ActionResult Game(int? id)
        {
            if (!id.HasValue)
            {
                return HttpNotFound();
            }
            Models.Editor model = new Models.Editor();
            model.GameId = id.Value;
            ViewBag.Title = "Quest";
            model.SimpleMode = GetSettingBool("simplemode", false);
            model.ErrorRedirect = ConfigurationManager.AppSettings["WebsiteHome"] ?? "http://textadventures.co.uk/";
            model.PlayURL = ConfigurationManager.AppSettings["PlayURL"] + "?id=" + id.ToString();
            model.CacheBuster = Convert.ToInt32((DateTime.Now - (new DateTime(2012, 1, 1))).TotalSeconds);
            return View(model);
        }

        public JsonResult Load(int id, bool simpleMode)
        {
            Logging.Log.DebugFormat("{0}: Load (simpleMode={1})", id, simpleMode);
            Services.EditorService editor = new Services.EditorService();
            EditorDictionary[id] = editor;
            string libFolder = ConfigurationManager.AppSettings["LibraryFolder"];
            string filename = Services.FileManagerLoader.GetFileManager().GetFile(id);
            if (filename == null)
            {
                return Json(new { error = "Invalid ID" }, JsonRequestBehavior.AllowGet);
            }
            var result = editor.Initialise(id, filename, libFolder, simpleMode);
            if (!result.Success)
            {
                return Json(new { error = result.Error.Replace(Environment.NewLine, "<br/>") }, JsonRequestBehavior.AllowGet);
            }
            
            return Json(new {
                tree = editor.GetElementTreeForJson(),
                editorstyle = editor.Style
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult Scripts(int id)
        {
            Logging.Log.DebugFormat("{0}: Get scripts JSON", id);
            return Json(EditorDictionary[id].GetScriptAdderJson(), JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult EditAttribute(int id, string key, string attributeName)
        {
            var element = EditorDictionary[id].GetElementModelForView(id, key, "", null, null, ModelState);
            var data = (IEditorDataExtendedAttributeInfo)element.EditorData;

            var model = new EditAttributeModel
            {
                Element = element,
                Control = new AttributeSubEditorControlData(attributeName),
                Value = data.GetAttribute(attributeName)
            };

            return PartialView("EditAttribute", model);
        }

        public PartialViewResult EditElement(int id, string key, string tab, string error, string refreshTreeSelectElement)
        {
            Logging.Log.DebugFormat("{0}: EditElement {1}", id, key);
            if (Session["EditorDictionary"] == null)
            {
                return Timeout();
            }
            Models.Element model = EditorDictionary[id].GetElementModelForView(id, key, tab, error, refreshTreeSelectElement, ModelState);
            return PartialView("ElementEditor", model);
        }

        [HttpPost]
        public PartialViewResult SaveElement(Models.ElementSaveData element)
        {
            Logging.Log.DebugFormat("{0}: SaveElement {1}", element.GameId, element.Key);
            if (!element.Success)
            {
                Logging.Log.DebugFormat("Element save failed");
                return Timeout();
            }
            var result = EditorDictionary[element.GameId].SaveElement(element.Key, element);
            string tab = !string.IsNullOrEmpty(element.AdditionalAction) ? element.AdditionalActionTab : null;
            return EditElement(element.GameId, element.RedirectToElement, tab, result.Error, result.RefreshTreeSelectElement);
        }

        [HttpPost]
        public PartialViewResult ProcessAction(int id, string key, string tab, string actionCmd)
        {
            var result = EditorDictionary[id].ProcessAdditionalAction(key, actionCmd);
            return EditElement(id, key, tab, null, result.RefreshTreeSelectElement);
        }

        private PartialViewResult Timeout()
        {
            return PartialView("ElementEditor", new Models.Element
            {
                PopupError = "Sorry, your session has timed out.",
                Reload = "1"
            });
        }

        public PartialViewResult EditStringList(int id, string key, IEditorControl control)
        {
            return PartialView("StringListEditor", EditorDictionary[id].GetStringListModel(key, control));
        }

        public PartialViewResult EditScriptStringList(int id, string key, string path, IEditorControl control)
        {
            return PartialView("StringListEditor", EditorDictionary[id].GetScriptStringListModel(key, path, control));
        }

        public PartialViewResult EditScript(int id, string key, IEditorControl control)
        {
            return PartialView("ScriptEditor", EditorDictionary[id].GetScriptModel(id, key, control, ModelState));
        }

        public PartialViewResult EditScriptValue(int id, string key, IEditableScripts script, string attribute)
        {
            return PartialView("ScriptEditor", EditorDictionary[id].GetScriptModel(id, key, script, attribute, ModelState));
        }

        public PartialViewResult ElementsList(int id, string key, IEditorControl control)
        {
            return PartialView("ElementsList", EditorDictionary[id].GetElementsListModel(id, key, control));
        }

        public PartialViewResult EditExits(int id, string key, IEditorControl control)
        {
            return PartialView("ExitsEditor", EditorDictionary[id].GetExitsModel(id, key, control));
        }

        public PartialViewResult EditVerbs(int id, string key, IEditorControl control)
        {
            return PartialView("VerbsEditor", EditorDictionary[id].GetVerbsModel(id, key, control));
        }

        public PartialViewResult EditScriptDictionary(int id, string key, IEditorControl control)
        {
            return PartialView("ScriptDictionaryEditor", EditorDictionary[id].GetScriptDictionaryModel(id, key, control, ModelState));
        }

        public PartialViewResult EditScriptScriptDictionary(int id, string key, string path, IEditorControl control)
        {
            return PartialView("ScriptDictionaryEditor", EditorDictionary[id].GetScriptScriptDictionaryModel(id, key, path, control, ModelState));
        }

        public PartialViewResult EditScriptDictionaryValue(int id, string key, IEditableDictionary<IEditableScripts> value, string keyPrompt, string source, string attribute)
        {
            return PartialView("ScriptDictionaryEditor", EditorDictionary[id].GetScriptDictionaryModel(id, key, value, keyPrompt, source, attribute, ModelState));
        }

        public PartialViewResult EditStringDictionary(int id, string key, IEditorControl control)
        {
            return PartialView("StringDictionaryEditor", EditorDictionary[id].GetStringDictionaryModel(id, key, control, ModelState));
        }

        public PartialViewResult EditGameBookOptions(int id, string key, IEditorControl control)
        {
            return PartialView("StringDictionaryEditor", EditorDictionary[id].GetStringDictionaryModel(id, key, control, ModelState, true));
        }

        private Dictionary<int, Services.EditorService> EditorDictionary
        {
            get
            {
                Dictionary<int, Services.EditorService> result = Session["EditorDictionary"] as Dictionary<int, Services.EditorService>;
                if (result == null)
                {
                    result = new Dictionary<int, Services.EditorService>();
                    Session["EditorDictionary"] = result;
                }
                return result;
            }
        }

        [OutputCache(NoStore = true, Duration = 0)]
        public JsonResult RefreshTree(int id)
        {
            return Json(EditorDictionary[id].GetElementTreeForJson(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public RedirectToRouteResult SaveSettings(Models.Editor settings)
        {
            SaveSettingBool("simplemode", settings.SimpleMode);
            return RedirectToAction("Game", new { id = settings.GameId });
        }

        private void SaveSetting(string name, string value)
        {
            HttpCookie cookie = new HttpCookie("simplemode", value);
            cookie.Expires = DateTime.Now + new TimeSpan(30, 0, 0, 0);
            Response.Cookies.Add(cookie);
        }

        private string GetSetting(string name)
        {
            HttpCookie cookie = Request.Cookies.Get(name);
            if (cookie == null) return string.Empty;
            return cookie.Value;
        }

        private void SaveSettingBool(string name, bool value)
        {
            SaveSetting(name, value ? "1" : "0");
        }

        private bool GetSettingBool(string name, bool defaultValue)
        {
            string result = GetSetting(name);
            if (result == "1") return true;
            if (result == "0") return false;
            return defaultValue;
        }

        public ActionResult FileUpload(int id)
        {
            return View(new WebEditor.Models.FileUpload
            {
                GameId = id,
                AllFiles = GetAllFilesList(id)
            });
        }

        private static List<string> s_serverPermittedExtensions = new List<string>
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".wav",
            ".mp3"
        };

        [HttpPost]
        public ActionResult FileUpload(WebEditor.Models.FileUpload fileModel)
        {
            if (ModelState.IsValid)
            {
                if (!EditorDictionary.ContainsKey(fileModel.GameId))
                {
                    Logging.Log.ErrorFormat("FileUpload - game id {0} not in EditorDictionary", fileModel.GameId);
                    return new HttpStatusCodeResult(500);
                }

                bool continueSave = true;
                string ext = System.IO.Path.GetExtension(fileModel.File.FileName).ToLower();
                List<string> controlPermittedExtensions = EditorDictionary[fileModel.GameId].GetPermittedExtensions(fileModel.Key, fileModel.Attribute);
                if (fileModel.File != null
                    && fileModel.File.ContentLength > 0
                    && s_serverPermittedExtensions.Contains(ext)
                    && controlPermittedExtensions.Contains(ext))
                {
                    string filename = System.IO.Path.GetFileName(fileModel.File.FileName);
                    Logging.Log.DebugFormat("{0}: Upload file {1}", fileModel.GameId, filename);
                    string uploadPath = Services.FileManagerLoader.GetFileManager().UploadPath(fileModel.GameId);
                    
                    // Check to see if file with same name exists
                    if(System.IO.File.Exists(System.IO.Path.Combine(uploadPath, filename)))
                    {
                        FileStream existingFile = new FileStream(System.IO.Path.Combine(uploadPath, filename), FileMode.Open);
                        // if files different, rename the new file by appending a Guid to the name
                        if (!FileCompare(fileModel.File.InputStream, existingFile))
                        {
                            // rename the file by adding a number [count] at the end of filename
                            filename = EditorUtility.GetUniqueFilename(fileModel.File.FileName);                            
                        }
                        else
                            continueSave = false; // skip saving if files are identical
                        existingFile.Close();
                    }

                    if(continueSave)
                        fileModel.File.SaveAs(System.IO.Path.Combine(uploadPath, filename));

                    ModelState.Remove("AllFiles");
                    fileModel.AllFiles = GetAllFilesList(fileModel.GameId);
                    ModelState.Remove("PostedFile");
                    fileModel.PostedFile = filename;
                }
                else
                {
                    ModelState.AddModelError("File", "Invalid file type");
                }
            }
            return View(fileModel);
        }

        private string GetAllFilesList(int id)
        {
            string path = Services.FileManagerLoader.GetFileManager().UploadPath(id);
            if (path == null) return null;  // this will be the case if there was no logged-in user
            IEnumerable<string> files = System.IO.Directory.GetFiles(path).Select(f => System.IO.Path.GetFileName(f)).OrderBy(f => f);
            return string.Join(":", files);
        }

        public ActionResult Publish(int id)
        {
            Services.EditorService editor = new Services.EditorService();
            string libFolder = ConfigurationManager.AppSettings["LibraryFolder"];
            string filename = Services.FileManagerLoader.GetFileManager().GetFile(id);
            if (filename == null)
            {
                return View("Error");
            }
            var result = editor.Initialise(id, filename, libFolder, false);
            if (!result.Success)
            {
                return View("Error");
            }
            
            string outputFolder = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(filename),
                "Output");
            
            System.IO.Directory.CreateDirectory(outputFolder);
            
            string outputFilename = System.IO.Path.Combine(
                outputFolder,
                System.IO.Path.GetFileNameWithoutExtension(filename) + ".quest");

            if (System.IO.File.Exists(outputFilename))
            {
                System.IO.File.Delete(outputFilename);
            }
            
            editor.Publish(outputFilename);

            string url = ConfigurationManager.AppSettings["PublishURL"] + id;

            return Redirect(url);
        }

        // Helper methods
        private bool FileCompare(Stream file1, Stream file2)
        {
            int file1byte;
            int file2byte;

            // check the file-sizes; if they are not same then not identical
            if (file1.Length != file2.Length)
            {
                return false;
            }

            // do Byte by Byte Comparison
            do{
               file1byte = file1.ReadByte();
                file2byte = file2.ReadByte();
            }while((file1byte == file2byte) && (file1byte != -1));
            
            // return result of comparison
            return ((file1byte - file2byte) == 0);
        }

        public ActionResult Download(int id)
        {
            var file = Services.FileManagerLoader.GetFileManager().GetFile(id);
            var folder = Path.GetDirectoryName(file);

            ZipFile zip = new ZipFile();
            foreach (var fileInFolder in Directory.GetFiles(folder))
            {
                zip.AddFile(fileInFolder, "");
            }

            return new FileGeneratingResult("game.zip", "application/zip", zip.Save);
        }
    }

    // From http://stackoverflow.com/questions/943122/writing-to-output-stream-from-action:
    /// <summary>
    /// MVC action result that generates the file content using a delegate that writes the content directly to the output stream.
    /// </summary>
    public class FileGeneratingResult : FileResult
    {
        /// <summary>
        /// The delegate that will generate the file content.
        /// </summary>
        private Action<System.IO.Stream> Content;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileGeneratingResult" /> class.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="content">The content.</param>
        public FileGeneratingResult(string fileName, string contentType, Action<System.IO.Stream> content)
            : base(contentType)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            this.Content = content;
            this.FileDownloadName = fileName;
        }

        /// <summary>
        /// Writes the file to the response.
        /// </summary>
        /// <param name="response">The response object.</param>
        protected override void WriteFile(System.Web.HttpResponseBase response)
        {
            this.Content(response.OutputStream);
        }
    }
}
