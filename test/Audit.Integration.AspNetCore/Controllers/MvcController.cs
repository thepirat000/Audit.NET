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

    public class MvcController : Controller
    {
        // GET: test/abc
        [Audit(IncludeHeaders = true, IncludeModel = true)]
        [Route("test/{title}")]
        public async Task<ActionResult> Index([FromRoute] string title)
        {
            await Task.Delay(1);
            if (title == "666")
            {
                throw new Exception("this is a test exception");
            }
            return View(new TestModelClass(){Title = title});
        }

        // GET: Mvc/Details/5
        public ActionResult Details(int id)
        {
            return View();
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