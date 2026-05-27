using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Data;
using SIARAWEB.Models;

namespace SIARAWEB.Controllers
{
    public class TrackingDeadlinesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TrackingDeadlinesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TrackingDeadlines
        public async Task<IActionResult> Index()
        {
            return View(await _context.TrackingDeadline.ToListAsync());
        }

        // GET: TrackingDeadlines/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trackingDeadline = await _context.TrackingDeadline
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trackingDeadline == null)
            {
                return NotFound();
            }

            return View(trackingDeadline);
        }

        // GET: TrackingDeadlines/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TrackingDeadlines/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Phase,CutoffDate")] TrackingDeadline trackingDeadline)
        {
            if (ModelState.IsValid)
            {
                _context.Add(trackingDeadline);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(trackingDeadline);
        }

        // GET: TrackingDeadlines/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trackingDeadline = await _context.TrackingDeadline.FindAsync(id);
            if (trackingDeadline == null)
            {
                return NotFound();
            }
            return View(trackingDeadline);
        }

        // POST: TrackingDeadlines/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Phase,CutoffDate")] TrackingDeadline trackingDeadline)
        {
            if (id != trackingDeadline.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(trackingDeadline);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TrackingDeadlineExists(trackingDeadline.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(trackingDeadline);
        }

        // GET: TrackingDeadlines/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var trackingDeadline = await _context.TrackingDeadline
                .FirstOrDefaultAsync(m => m.Id == id);
            if (trackingDeadline == null)
            {
                return NotFound();
            }

            return View(trackingDeadline);
        }

        // POST: TrackingDeadlines/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var trackingDeadline = await _context.TrackingDeadline.FindAsync(id);
            if (trackingDeadline != null)
            {
                _context.TrackingDeadline.Remove(trackingDeadline);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TrackingDeadlineExists(int id)
        {
            return _context.TrackingDeadline.Any(e => e.Id == id);
        }
    }
}
