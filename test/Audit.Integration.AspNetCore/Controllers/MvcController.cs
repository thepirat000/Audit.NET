using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Audit.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Audit.Integration.AspNetCore.Controllers
{
    public class TestModelClass
    {
        public string Title { get; set; }
    }

    public class UploadModel
    {
        public string ImageCaption { set; get; }
        public string ImageDescription { set; get; }
        public IFormFile MyImage { set; get; }
    }

    public class MvcController : Controller
    {
        [HttpGet]
        [Route("mvc/ignoreme")]
        [Audit(IncludeHeaders = true, IncludeModel = true)]
        [AuditIgnore]
        public async Task<ActionResult> IgnoreMe()
        {
            await Task.Delay(1);
            return View("Index", new TestModelClass());
        }

        [HttpGet]
        [Route("mvc/ignoreparam")]
        [Audit(IncludeHeaders = true, IncludeModel = true, IncludeResponseBody = true)]
        public async Task<ActionResult> IgnoreParam(int id, [AuditIgnore]string secret)
        {
            await Task.Delay(1);
            return View("Index", new TestModelClass());
        }

        [HttpGet]
        [Route("mvc/ignoreresponse")]
        [Audit(IncludeHeaders = true, IncludeModel = true, IncludeResponseBody = true)]
        [return:AuditIgnore]
        public async Task<ActionResult> IgnoreResponse(int id, string secret)
        {
            await Task.Delay(1);
            return View("Index", new TestModelClass());
        }

        // GET: test/abc
        [Audit(IncludeHeaders = true, IncludeModel = true)]
        [Route("test/{title}")]
        public async Task<ActionResult> Index([FromRoute] string title)
        {
            await Task.Delay(1);
            if (title == "666")
            {
                throw new Exception("*************** THIS IS A TEST EXCEPTION **************");
            }
            return View(new TestModelClass(){Title = title});
        }

        // GET: Mvc/Details/5
        [Audit.WebApi.AuditIgnore]
        [HttpGet]
        [Route("mvc/details/{id}")]
        public ActionResult Details(int id)
        {
            return View("Index", new TestModelClass() { Title = id.ToString() });
        }

        // GET: Mvc/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Mvc/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Mvc/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Mvc/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Mvc/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Mvc/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}